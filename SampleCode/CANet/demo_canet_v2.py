# this code was adapted from resnet_v2.py obtained from the tf-slim github repo
from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import tensorflow as tf

import demo_canet_utils as canet_utils

slim = tf.contrib.slim
canet_arg_scope = canet_utils.canet_arg_scope


@slim.add_arg_scope
def bottleneck(inputs, depth, depth_bottleneck, stride, rate=1,
               outputs_collections=None, scope=None):
    """Bottleneck residual unit variant with BN before convolutions.

    This is the full preactivation residual unit variant proposed in [2]. See
    Fig. 1(b) of [2] for its definition. Note that we use here the bottleneck
    variant which has an extra bottleneck layer.

    When putting together two consecutive ResNet blocks that use this unit, one
    should use stride = 2 in the last unit of the first block.

    Args:
      inputs: A tensor of size [batch, height, width, channels].
      depth: The depth of the ResNet unit output.
      depth_bottleneck: The depth of the bottleneck layers.
      stride: The ResNet unit's stride. Determines the amount of downsampling of
        the units output compared to its input.
      rate: An integer, rate for atrous convolution.
      outputs_collections: Collection to add the ResNet unit output.
      scope: Optional variable_scope.

    Returns:
      The ResNet unit's output.
    """
    with tf.variable_scope(scope, 'bottleneck_v2', [inputs]) as sc:
        depth_in = slim.utils.last_dimension(inputs.get_shape(), min_rank=4)
        preact = slim.batch_norm(inputs, activation_fn=tf.nn.relu, scope='preact')
        if depth == depth_in:
            shortcut = canet_utils.subsample(inputs, stride, 'shortcut')
        else:
            shortcut = slim.conv2d(preact, depth, [1, 1], stride=stride,normalizer_fn=None, activation_fn=None,scope='shortcut')

        residual = slim.conv2d(preact, depth_bottleneck, [1, 1], stride=1,scope='conv1')
        residual = canet_utils.conv2d_same(residual, depth_bottleneck, 3, stride,rate=rate, scope='conv2')
        residual = slim.conv2d(residual, depth, [1, 1], stride=1,normalizer_fn=None, activation_fn=None,scope='conv3')

        output = shortcut + residual

        return slim.utils.collect_named_outputs(outputs_collections,
                                                sc.original_name_scope,
                                                output)


def CANet(inputs,
              mask1,
              mask2,
              blocks,
              num_classes_intent=None,
              num_classes_hand = None,
              is_training=True,
              global_pool=True,
              output_stride=None,
              include_root_block=True,
              spatial_squeeze=True,
              reuse=None,
              scope=None):

    print ('Input is: ', inputs.shape)

    with tf.variable_scope(scope, 'resnet_v2', [inputs], reuse=reuse) as sc:
        end_points_collection = sc.name + '_end_points'
        with slim.arg_scope([slim.conv2d, bottleneck,
                             canet_utils.stack_blocks_dense],
                            outputs_collections=end_points_collection):
            with slim.arg_scope([slim.batch_norm], is_training=is_training):
                net = inputs
                if include_root_block:
                    if output_stride is not None:
                        if output_stride % 4 != 0:
                            raise ValueError('The output_stride needs to be a multiple of 4.')
                        output_stride /= 4
                    # We do not include batch normalization or activation functions in
                    # conv1 because the first ResNet unit will perform these. Cf.
                    # Appendix of [2].
                    with slim.arg_scope([slim.conv2d],activation_fn=None, normalizer_fn=None):
                        net = canet_utils.conv2d_same(net, 64, 7, stride=2, scope='conv1')
                    net = slim.max_pool2d(net, [3, 3], stride=2, scope='pool1')
                net = canet_utils.stack_blocks_dense(net, blocks, output_stride)

                print ('Global net is: ', net)
                # This is needed because the pre-activation variant does not have batch
                # normalization or activation functions in the residual unit output. See
                # Appendix of [2].
                net = slim.batch_norm(net, activation_fn=tf.nn.relu, scope='postnorm')
                #begin prady add
                print('mask1 size is: ', mask1)
                pooled_mask1 = tf.nn.avg_pool(mask1, [1, 32, 32, 1], [1, 32, 32, 1], 'SAME',name='hand_mask1')
                print('Pooled mask1 is: ', pooled_mask1)
                pooled_mask1 = tf.div(pooled_mask1, tf.expand_dims(tf.expand_dims(tf.expand_dims(tf.add(tf.reduce_sum(pooled_mask1, [1, 2, 3]),1e-7),1),1),1))

                m1 = pooled_mask1

                pooled_mask1 = tf.concat([pooled_mask1] * 2048, axis=3)
                net1 = tf.multiply(net, pooled_mask1,name='feature_mask1')
                a = net
                b = net1
                pooled_mask2 = tf.nn.avg_pool(mask2, [1, 32, 32, 1], [1, 32, 32, 1], 'SAME', name='hand_mask2')
                pooled_mask2 = tf.div(pooled_mask2, tf.expand_dims(tf.expand_dims(tf.expand_dims(tf.add(tf.reduce_sum(pooled_mask2, [1, 2, 3]), 1e-7), 1), 1), 1))
                m2 = pooled_mask2

                pooled_mask2 = tf.concat([pooled_mask2] * 2048, axis=3)
                net2 = tf.multiply(net, pooled_mask2, name='feature_mask2')
                c=net2
                d=pooled_mask1
                e=pooled_mask2
                #end add
                if global_pool:
                    # Global average pooling.
                    net = tf.reduce_mean(net, [1, 2], name='pool5', keep_dims=True)
                    #begin prady add
                    net1 = tf.reduce_mean(net1, [1, 2], name='pool6', keep_dims=True)
                    net2 = tf.reduce_mean(net2, [1, 2], name='pool7', keep_dims=True)
                    f,g,h = net, net1, net2
                    #end add
                if num_classes_intent is not None and num_classes_hand is not None:
                    net = slim.conv2d(net, num_classes_intent, [1, 1], activation_fn=None, normalizer_fn=None, scope='logits')

                    #begin prady add
                    net1 = slim.conv2d(net1, num_classes_hand, [1, 1], activation_fn=None,normalizer_fn=None, scope='logits1')
                    net2 = slim.conv2d(net2, num_classes_hand, [1, 1], activation_fn=None, normalizer_fn=None, scope='logits2')
                    #end add

                    if spatial_squeeze:
                        net = tf.squeeze(net, [1, 2], name='SpatialSqueeze')
                        #begin prady add
                        net1 = tf.squeeze(net1, [1, 2], name='SpatialSqueeze1')
                        net2 = tf.squeeze(net2, [1, 2], name='SpatialSqueeze2')
                        #end add

                # Convert end_points_collection into a dictionary of end_points.
                end_points = slim.utils.convert_collection_to_dict(end_points_collection)
                if num_classes_intent is not None and num_classes_hand is not None:
                    end_points['predictions'] = slim.softmax(net, scope='predictions')
                    #begin prady add
                    end_points['hand_predictions1'] = slim.softmax(net1, scope='predictions1')
                    end_points['hand_predictions2'] = slim.softmax(net2, scope='predictions2')
                end_points['mask1'] = m1
                end_points['mask2'] = m2
                end_points['a'] = [a,b,c,d,e]
                #end add
                return net, end_points, net1, net2


CANet.default_image_size = 224



def resnet_v2_block(scope, base_depth, num_units, stride):
    """Helper function for creating a resnet_v2 bottleneck block.

    Args:
      scope: The scope of the block.
      base_depth: The depth of the bottleneck layer for each unit.
      num_units: The number of units in the block.
      stride: The stride of the block, implemented as a stride in the last unit.
        All other units have stride=1.

    Returns:
      A resnet_v2 bottleneck block.
    """
    return canet_utils.Block(scope, bottleneck, [{
        'depth': base_depth * 4,
        'depth_bottleneck': base_depth,
        'stride': 1
    }] * (num_units - 1) + [{
        'depth': base_depth * 4,
        'depth_bottleneck': base_depth,
        'stride': stride
    }])


CANet.default_image_size = 224


def CANet_50(inputs,
                 mask1,
                 mask2,
                 num_classes_intent=None,
                 num_classes_hand = None,
                 is_training=True,
                 global_pool=True,
                 output_stride=None,
                 spatial_squeeze=True,
                 reuse=None,
                 scope='resnet_v2_50'):

    inputs = tf.to_float(inputs)
    mask1 = tf.to_float(mask1)
    mask2 = tf.to_float(mask2)

    """ResNet-50 model of [1]. See resnet_v2() for arg and return description."""
    blocks = [
        resnet_v2_block('block1', base_depth=64, num_units=3, stride=2),
        resnet_v2_block('block2', base_depth=128, num_units=4, stride=2),
        resnet_v2_block('block3', base_depth=256, num_units=6, stride=2),
        resnet_v2_block('block4', base_depth=512, num_units=3, stride=1),
    ]
    return CANet(inputs, mask1,mask2,blocks, num_classes_intent, num_classes_hand, is_training=is_training,
                     global_pool=global_pool, output_stride=output_stride,
                     include_root_block=True, spatial_squeeze=spatial_squeeze,
                     reuse=reuse, scope=scope)


CANet_50.default_image_size = CANet.default_image_size
