import struct

import numpy as np
from skimage.transform import resize

class BaseClassifier:
    def __init__(self, recognizer, hand):
        # load gesture labels
        self.num_gestures = 32  # this is the number of gestures trained with ResNet
        self.probs = None  # probs sent to fusion, recalculate for each frame
        self.hand = hand
        self.hand_recognition = recognizer

    def get_bytes(self, timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces, hand, gestures, stream_id):
        self.probs = [0 for i in range(len(gestures))]
        max_index = \
            self._process(timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces, gestures)

        pack_list = [stream_id, timestamp, max_index] + list(self.probs)

        return struct.pack("<iqi" + "f" * len(self.probs), *pack_list)

    def _process(self, timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces, gestures):
        if posx == -1 and posy == -1:
            max_index = len(self.probs) - 1  # max_index refers to 'blind'
            self.probs[max_index] = 1
        else:
            hand_arr = self._preprocess_hand_arr(depth_data, posx, posy, height, width)
            if self.hand == "RH":
                max_index, new_probs = self.hand_recognition.classify(hand_arr, flip=False)
            else:
                max_index, new_probs = self.hand_recognition.classify(hand_arr, flip=True)
            self.probs[:len(new_probs)] = new_probs

        print('{:<20}'.format(gestures[max_index]), '{:.1}'.format(float(self.probs[max_index])), end='\t')
        return max_index

    def _preprocess_hand_arr(self, depth_data, posx, posy, height, width):
        hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
        posz = hand_arr[int(posx), int(posy)]
        hand_arr -= posz
        hand_arr /= 150
        hand_arr = np.clip(hand_arr, -1, 1)
        hand_arr = resize(hand_arr, (168, 168))
        hand_arr = hand_arr[20:-20, 20:-20]
        hand_arr = hand_arr.reshape((1, 128, 128, 1))

        return hand_arr
