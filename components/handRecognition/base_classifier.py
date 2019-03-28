import struct

import numpy as np
from skimage.transform import resize

class BaseClassifier:
    def __init__(self, hand, lock, is_flipped=False):
        # load gesture labels
        self.num_gestures = 32  # this is the number of gestures trained with ResNet
        self.probs = None  # probs sent to fusion, recalculate for each frame
        self.hand = hand

    def get_bytes(self, timestamp, writer_data_hand, engaged, frame_pieces, gestures, stream_id, feature, blind):
        self.probs = [0 for i in range(len(gestures))]


        max_index = self._process(feature, writer_data_hand, engaged, frame_pieces, gestures, blind)

        print('{:<20}'.format(gestures[max_index]), '{:.1}'.format(float(self.probs[max_index])), end='\t')

        pack_list = [stream_id, timestamp, max_index] + list(self.probs)

        return struct.pack("<iqi" + "f" * len(self.probs), *pack_list)

    def _process(self, feature, writer_data_hand, engaged, frame_pieces, gestures, blind):
        if blind:
            max_index = len(self.probs) - 1  # max_index refers to 'blind'
            self.probs[max_index] = 1
        else:
            max_index, new_probs = feature
            self.probs[:len(new_probs)] = new_probs

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
