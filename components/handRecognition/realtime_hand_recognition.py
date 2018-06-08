from . import hands_resnet_model
import tensorflow as tf
import numpy as np
import os

class RealTimeHandRecognition():
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

        sess = tf.Session(config=tf.ConfigProto(allow_soft_placement=True))
        tf.train.start_queue_runners(sess)
        # this is unnecessary, since we just want the latest ckpt file
        # ckpt_state = tf.train.get_checkpoint_state(r"C:\Users\cwc\Desktop\portable\RealTime\components\log\%s_model"%hands)
        # print('Loading checkpoint %s', ckpt_state.model_checkpoint_path)
        # saver.restore(sess, ckpt_state.model_checkpoint_path)
        saver.restore(sess, r"components\log\%s_model.ckpt"%hands)

        self.sess = sess
        self.model = model

        self.past_probs = None

    def classify(self, data):
        (predictions) = self.sess.run([self.model.predictions], feed_dict={self.model._images: data})
        probs = predictions[0][0]

        if self.past_probs is None:
            self.past_probs = probs
        else:
            self.past_probs = (self.past_probs+probs)/2


        max_prediction = np.argmax(self.past_probs)
        return max_prediction, self.past_probs

