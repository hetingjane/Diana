import struct

class BaseClassifier:
    def __init__(self, hand, lock, blacklist, is_flipped=False):
        # load gesture labels
        self.num_gestures = 32  # this is the number of gestures trained with ResNet
        self.probs = None  # probs sent to fusion, recalculate for each frame
        self.hand = hand

    def get_bytes(self, timestamp, writer_data_hand, engaged, frame_pieces, gestures, stream_id, probs, feature, blind, frame):
        self.probs = [0 for i in range(len(gestures))]


        max_index = self._process(feature, writer_data_hand, engaged, frame_pieces, probs, gestures, blind, frame)

        print('{:<20}'.format(gestures[max_index]), '{:.1}'.format(float(self.probs[max_index])), end='\t')

        pack_list = [stream_id, timestamp, max_index] + list(self.probs)

        return struct.pack("<iqi" + "f" * len(self.probs), *pack_list)

    def _process(self, feature, writer_data_hand, engaged, frame_pieces, probs, gestures, blind, frame):
        if blind:
            max_index = len(self.probs) - 1  # max_index refers to 'blind'
            self.probs[max_index] = 1
        else:
            max_index = feature
            new_probs = probs
            self.probs[:len(new_probs)] = new_probs

        return max_index
