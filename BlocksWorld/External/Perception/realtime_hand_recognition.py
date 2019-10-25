import os

import tensorflow as tf
import numpy as np

import hands_resnet_model


class RealTimeHandRecognition:
    def __init__(self, hands, gestures, batch_size, blind_idx):

        hps = hands_resnet_model.HParams(batch_size=batch_size,
                                         num_classes=gestures,
                                         min_lrn_rate=0.0001,
                                         lrn_rate=0.1,
                                         num_residual_units=5,
                                         use_bottleneck=True,
                                         weight_decay_rate=0.0002,
                                         relu_leakiness=0,
                                         optimizer='mom')

        self.BLIND_IDX = blind_idx
        self.active_arm_threshold = 200
        self.pixel_intensity_threshold = 0.4
        model = hands_resnet_model.ResNet(hps, "eval")
        model.build_graph()
        saver = tf.train.Saver()

        gpu_options = tf.GPUOptions(per_process_gpu_memory_fraction=0.5)
        self.config = tf.ConfigProto(gpu_options=gpu_options)
        self.config.gpu_options.allow_growth = True
        self.config.allow_soft_placement = True

        sess = tf.Session(config=self.config)
        tf.train.start_queue_runners(sess)
        path = os.path.abspath('External/Perception/models/{}'.format(hands))
        print(path)
        ckpt_state = tf.train.get_checkpoint_state(path)
        print('Loading checkpoint {}'.format(ckpt_state.model_checkpoint_path))
        saver.restore(sess, ckpt_state.model_checkpoint_path)

        self.sess = sess
        self.model = model

        self.past_probs_L = None
        self.past_probs_R = None

    def _is_bright(self, depth_frame):
        #hand_arr = np.squeeze(depth_frame)

        bright_corners = np.sum([depth_frame[i, j] > self.pixel_intensity_threshold for i in [0, -1] for j in [0, -1]])
        return bright_corners >= 3
        
    def get_frame(self, data):
        frame, hand_y, hand_z, spine_base_y, spine_base_z = data
        
        #we don't want to process frames when user's hand is resting (at/near spine base y or z)
        hand_low = hand_y > spine_base_y
        hand_close_to_body = (spine_base_z - hand_z) < self.active_arm_threshold
        hand_behind = spine_base_z < hand_z
        
        print('low', hand_low, 'close', hand_close_to_body, 'behind', hand_behind, end='\t')
        
        if hand_behind or (hand_low and hand_close_to_body):
            self.past_probs_L = None
            self.past_probs_R = None
            return None
        else:
            return frame
            
    def smoothL(self, LH_probs):
        if self.past_probs_L is None:
            self.past_probs_L = LH_probs
        else:
            self.past_probs_L = (self.past_probs_L + LH_probs) / 2
            
        return self.past_probs_L
            
    def smoothR(self, RH_probs):
        if self.past_probs_R is None:
            self.past_probs_R = RH_probs
        else:
            self.past_probs_R = (self.past_probs_R+RH_probs) / 2
            
        return self.past_probs_R
        
    def classifyLR(self, frame_L, frame_R):
        input_shape = list(frame_L.shape)
        input_shape[0] = 2
        input = np.empty(input_shape)
        input[0] = np.flipud(frame_L)
        input[1] = frame_R
        (predictions) = self.sess.run(self.model.predictions, feed_dict={self.model._images: input})

        smoothed_L = self.smoothL(predictions[0])
        top1_L = np.argmax(smoothed_L)
        smoothed_R = self.smoothR(predictions[1])
        top1_R = np.argmax(smoothed_R)
        return top1_L, top1_R

    def get_predsmax(self, data_L, data_R):
        frame_L = self.get_frame(data_L)
        frame_R = self.get_frame(data_R)
        
        if frame_L is not None and frame_R is not None:
            pred_L, pred_R = self.classifyLR(frame_L, frame_R)
            return pred_L, pred_R
        elif frame_L is not None:
            frame_R = np.zeros((1, 128, 128, 1))
            pred_L, pred_R = self.classifyLR(frame_L, frame_R)
            return pred_L, self.BLIND_IDX
        elif frame_R is not None:
            frame_L = np.zeros((1, 128, 128, 1))
            pred_L, pred_R = self.classifyLR(frame_L, frame_R)
            return self.BLIND_IDX, pred_R
        else:
            return self.BLIND_IDX, self.BLIND_IDX


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

        return predictions, feature

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


