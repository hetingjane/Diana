from . import hands_resnet_model
import tensorflow as tf
import numpy as np


class RealTimeHandRecognition():
    def __init__(self, gestures):

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

        gpu_options = tf.GPUOptions()
        self.config = tf.ConfigProto(gpu_options=gpu_options)
        self.config.gpu_options.allow_growth = True
        self.config.allow_soft_placement = True

        sess = tf.Session(config=self.config)
        tf.train.start_queue_runners(sess)
        saver.restore(sess, r"components\log\RH_model.ckpt")

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

