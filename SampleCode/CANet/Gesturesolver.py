import numpy as np
import tensorflow as tf
from collections import deque
from os.path import join
from itertools import chain


class GestureRecognition(object):
    def __init__(self):
        # Unpack keyword arguments
        self.batch_size = 1
        self.num_intent = 21
        self.num_hand = 32
        self.window_length = 10
        self.IMAGE_HEIGHT, self.IMAGE_WIDTH = 424, 512
        self._depthimage_stream = deque([], maxlen=self.window_length)
        self._leftmask_stream = deque([], maxlen=self.window_length)
        self._rightmask_stream = deque([], maxlen=self.window_length)

        from Models import CANet_try
        g1 = tf.Graph()
        self.log_root = "D:/UnityProjects/BlocksWorld/SampleCode/CANet/"
        with g1.as_default():
            print ('Loading CANet model')
            print ('Log root is: ', self.log_root)
            self.model = CANet_try(log_root=self.log_root)
            checkpoint_file = tf.train.latest_checkpoint(self.log_root)
            print ('Latest checkpoint is: ', checkpoint_file)
            train_step = int(tf.train.latest_checkpoint(self.log_root).split("-")[-1])
            print ('Global step is ;', train_step)
            ckpt_state = tf.train.get_checkpoint_state(self.log_root)
            tf.logging.info('Loading checkpoint %s', ckpt_state.model_checkpoint_path)
            self.model.saver.restore(self.model.sess, ckpt_state.model_checkpoint_path)
            tf.logging.info("Checkpoint restoration done")


        self.intents = list(np.load(join(self.log_root, 'intents_list.npy'))) + ['blind']
        self.hands = list(np.load(join(self.log_root, 'gesture_list.npy'))) + ['blind']


    def feed_input(self, fd):
        self.engaged, self.depth_image, self.left_mask, self.right_mask = fd[0], fd[1], fd[2], fd[3]
        self.depth_image= self.preprocess_image(self.depth_image)

        encoding_array, proba_array = self.call_recognition()
        # Result is 22 probabilities of intent, 33 of each hand and 3 labels of intent, LH, RH
        # Total Length = 91
        self.result = encoding_array + list(chain(*proba_array))
        
    
    def preprocess_image(self, image):
        image = np.clip(image,400,1800)
        image = np.multiply(image, 1.0)
        image -= np.min(image)
        image /= np.max(image)
        image = np.multiply(image, 255)
        image = image.astype("uint8")
        '''
        #Threshold the depth distance to 2000mm. Normalize to 0-1, multiply by 255 and cast to int
        image = np.clip(image, 0, threshold)
        image = np.divide(image, threshold)
        image *= 255
        image = np.array(image, np.int32)
        '''
        return image


    def call_recognition(self):
        if self.engaged:
            self._depthimage_stream.extend([self.depth_image])
            self._leftmask_stream.extend([self.left_mask])
            self._rightmask_stream.extend([self.right_mask])
            if len(self._depthimage_stream) >= self.window_length:
                encoding_array, proba_array = self.get_gesture_result()
            else:
                encoding_array, proba_array = self.default_values()
        else:
            # print 'Disengaged....clearing buffer'
            encoding_array, proba_array = self.default_values()
            self._depthimage_stream.clear()
            self._leftmask_stream.clear()
            self._rightmask_stream.clear()
        return encoding_array, proba_array


    def get_gesture_result(self):
        #Input: depth data stream, depth image stream, skeleton stream
        #Process: Generate masks for LH, RH...>convert to numpy array
        #Output: Labels and Probability arrays
        proba_array, encoding_array = [], []

        
        ind = int(self.window_length/2 -1)
        #Changing code for the depth image to reshape, and taking the 4th index of masks, reshaping and passing to network
        x_frame, lh_mask, rh_mask = np.asarray(self._depthimage_stream), np.asarray(self._leftmask_stream)[ind], np.asarray(self._rightmask_stream)[ind]
        x_frame = np.transpose(x_frame, (1, 2, 0))
        x_frame = x_frame[np.newaxis, :,:, :]

        lh_mask = lh_mask[np.newaxis, :, :, np.newaxis]
        rh_mask = rh_mask[np.newaxis, :, :, np.newaxis]
        #print ('Shapes of x_Frame, lh_mask, rh_mask are: ', x_frame.shape, lh_mask.shape, rh_mask.shape)

        intent_label, intent_probs, lh_label, lh_probs, rh_label, rh_probs = self._predict(x_frame, lh_mask, rh_mask)
        encoding_array.append(intent_label)
        encoding_array.append(lh_label)
        encoding_array.append(rh_label)
        proba_array.append(intent_probs)
        proba_array.append(lh_probs)
        proba_array.append(rh_probs)

        return encoding_array, proba_array


    def printable_result(self, result):
        #print ('Length of result is: ', len(result))
        intent_label, lh_label, rh_label = result[0], result[1], result[2]
        return [self.intents[intent_label], 'LH: '+str(self.hands[lh_label]), 'RH: '+str(self.hands[rh_label])]


    def default_values(self):
        proba_array, encoding_array = [], []

        #default intent probability and encoding
        intent_probs = [0] * 21 + [1]
        proba_array.append(intent_probs)
        encoding_array.append(21)

        #default lh probability and encoding
        lh_probs = [0] * 32 + [1]
        proba_array.append(lh_probs)
        encoding_array.append(32)

        #default rh probability and encoding
        rh_probs = [0] * 32 + [1]
        proba_array.append(rh_probs)
        encoding_array.append(32)

        return encoding_array, proba_array



    def _predict(self, x_frame, x_lh, x_rh):
        #print ('shape of x_frame, x_lh, x_rh are: ', x_frame.shape, x_lh.shape, x_rh.shape)
        intent_probs, lh_probs, rh_probs = self.model.sess.run([self.model.intent_logits, self.model.lh_logits, self.model.rh_logits],feed_dict={self.model.images: x_frame, self.model.mask1: x_lh, self.model.mask2: x_rh})

        intent_probs = list(intent_probs[0]) + [0]
        lh_probs = list(lh_probs[0]) + [0]
        rh_probs = list(rh_probs[0]) + [0]

        intent_label = np.argmax(intent_probs)
        lh_label = np.argmax(lh_probs)
        rh_label = np.argmax(rh_probs)

        print ('-'*40)
        print ('Intent: %d, LH_label: %d, RH_label: %d'%(intent_label, lh_label, rh_label))
        return intent_label, intent_probs, lh_label, lh_probs, rh_label, rh_probs







