import struct

import numpy as np
from skimage.transform import resize

from .realtime_hand_recognition import RealTimeHandRecognition
from ..fusion.conf import postures


class BaseClassifier:
    def __init__(self, hand, stream_id):
        self.hand = hand
        self.stream_id = stream_id

        # load gesture labels
        self.gestures = postures.right_hand_postures if self.hand == 'RH' else postures.left_hand_postures
        self.num_gestures = 32  # this is the number of gestures trained with ResNet
        print(self.hand, self.num_gestures)
        self.probs = None  # probs sent to fusion, recalculate for each frame

        self.hand_recognition = self._get_hand_recognition()

    def get_bytes(self, timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces):
        self.probs = [0 for i in range(len(self.gestures))]
        max_index = \
            self._process(timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces)

        pack_list = [self.stream_id, timestamp, max_index] + list(self.probs)

        return struct.pack("<iqi" + "f" * len(self.probs), *pack_list)

    def _process(self, timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces):
        if posx == -1 and posy == -1:
            max_index = len(self.probs) - 1  # max_index refers to 'blind'
            self.probs[max_index] = 1
        else:
            hand_arr = self._preprocess_hand_arr(depth_data, posx, posy, height, width)
            max_index, new_probs = self.hand_recognition.classify(hand_arr)
            self.probs[:len(new_probs)] = new_probs

        print(timestamp, self.gestures[max_index], self.probs[max_index])
        return max_index

    def _get_hand_recognition(self):
        return RealTimeHandRecognition(self.hand, self.num_gestures)

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
