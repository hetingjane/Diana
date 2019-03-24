import os

import tensorflow as tf
import numpy as np

import hands_resnet_model


class RealTimeHandRecognition:
    def __init__(self, hands, gestures):

        hps = hands_resnet_model.HParams(batch_size=1,
                                         num_classes=gestures,
                                         min_lrn_rate=0.0001,
                                         lrn_rate=0.1,
                                         num_residual_units=5,
                                         use_bottleneck=True,
                                         weight_decay_rate=0.0002,
                                         relu_leakiness=0,
                                         optimizer='mom')

        model = hands_resnet_model.ResNet(hps, "eval")
        model.build_graph()
        saver = tf.train.Saver()

        gpu_options = tf.GPUOptions()#per_process_gpu_memory_fraction=0.4)
        self.config = tf.ConfigProto(gpu_options=gpu_options)
        # self.config.gpu_options.allow_growth = True
        # self.config.allow_soft_placement = True

        sess = tf.Session(config=self.config)
        tf.train.start_queue_runners(sess)
        print(os.path.abspath('./models/{}'.format(hands)))
        ckpt_state = tf.train.get_checkpoint_state(os.path.abspath('../../models/{}'.format(hands)))
        print('Loading checkpoint {}'.format(ckpt_state.model_checkpoint_path))
        saver.restore(sess, ckpt_state.model_checkpoint_path)

        self.sess = sess
        self.model = model

        self.past_probs = None

    def classify(self, data, flip):
        if flip:
            data = np.flipud(data)
        (predictions) = self.sess.run([self.model.predictions], feed_dict={self.model._images: data})
        probs = predictions[0][0]

        if self.past_probs is None:
            self.past_probs = probs
        else:
            self.past_probs = (self.past_probs+probs)/2

        max_prediction = np.argmax(self.past_probs)
        return max_prediction, self.past_probs


class RealTimeHandRecognitionOneShot(RealTimeHandRecognition):
    """
    Overloaded class specific for one-shot learning. Only generate feature vectors.
    """
    def __init__(self, hands, gestures):
        RealTimeHandRecognition.__init__(self, hands, gestures)

    def classify(self, data, flip):
        if flip:
            data = np.flipud(data)

        (feature) = self.sess.run([self.model.fc_x], feed_dict={self.model._images: data})

        return feature

