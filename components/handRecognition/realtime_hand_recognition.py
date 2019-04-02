import os

import tensorflow as tf
import numpy as np

from . import hands_resnet_model


class RealTimeHandRecognition:
    def __init__(self, hands, gestures, batch_size):

        hps = hands_resnet_model.HParams(batch_size=batch_size,
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

        gpu_options = tf.GPUOptions(per_process_gpu_memory_fraction=0.4)
        self.config = tf.ConfigProto(gpu_options=gpu_options)
        self.config.gpu_options.allow_growth = True
        self.config.allow_soft_placement = True

        sess = tf.Session(config=self.config)
        tf.train.start_queue_runners(sess)
        path = os.path.abspath('./models/{}'.format(hands))
        print(path)
        ckpt_state = tf.train.get_checkpoint_state(path)
        print('Loading checkpoint {}'.format(ckpt_state.model_checkpoint_path))
        saver.restore(sess, ckpt_state.model_checkpoint_path)

        self.sess = sess
        self.model = model

        self.past_probs = None
        self.past_probs_L = None
        self.past_probs_R = None

    def classify(self, data, flip):
        if flip:
            data = np.flipud(data)
        (predictions) = self.sess.run(self.model.predictions, feed_dict={self.model._images: data})
        probs = predictions[0]
        print('SHAPE', predictions.shape)

        if self.past_probs is None:
            self.past_probs = probs
        else:
            self.past_probs = (self.past_probs+probs)/2

        max_prediction = np.argmax(self.past_probs)
        return max_prediction, self.past_probs

    def classifyLR(self, data_L, data_R):
        input_shape = list(data_L.shape)
        input_shape[0] = 2
        input = np.empty(input_shape)
        input[0] = np.flipud(data_L)
        input[1] = data_R
        (predictions) = self.sess.run(self.model.predictions, feed_dict={self.model._images: input})
        LH_probs = predictions[0]
        RH_probs = predictions[1]

        if self.past_probs_L is None:
            self.past_probs_L = LH_probs
        else:
            self.past_probs_L = (self.past_probs_L + LH_probs) / 2

        max_prediction_LH = np.argmax(self.past_probs_L)

        if self.past_probs_R is None:
            self.past_probs_R = RH_probs
        else:
            self.past_probs_R = (self.past_probs_R+RH_probs)/2

        max_prediction_RH = np.argmax(self.past_probs_R)

        return (max_prediction_LH, self.past_probs_L), (max_prediction_RH, self.past_probs_R)


class RealTimeHandRecognitionOneShot(RealTimeHandRecognition):
    """
    Overloaded class specific for one-shot learning. Only generate feature vectors.
    """
    def __init__(self, hands, gestures, batch_size):
        RealTimeHandRecognition.__init__(self, hands, gestures, batch_size)

    def classify(self, data, flip=False):
        if flip:
            data = np.flipud(data)
        (feature, predictions) = self.sess.run([self.model.fc_x, self.model.predictions], feed_dict={self.model._images: data})

        return feature, predictions

    def classifyLR(self, data_L, data_R):
        input_shape = list(data_L.shape)
        input_shape[0] = 2
        input = np.empty(input_shape)
        input[0] = np.flipud(data_L)
        input[1] = data_R
        (features, predictions) = self.sess.run([self.model.fc_x, self.model.predictions], feed_dict={self.model._images: input})
        LH_feature = features[0]
        LH_probs = predictions[0]
        RH_feature = features[1]
        RH_probs = predictions[1]

        return (LH_probs, LH_feature), (RH_probs, RH_feature)


