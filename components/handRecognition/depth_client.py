import struct
import time
import argparse
import os
import pickle
import threading
import queue

from skimage.transform import resize
import sys
import numpy as np
from .realtime_hand_recognition import RealTimeHandRecognition
from ..fusion.conf.endpoints import connect
from ..fusion.conf import streams
from ..fusion.conf import decode

from . import RandomForest

global_lock = threading.Lock()

"""
# timestamp (long) | depth_hands_count(int) | left_hand_height (int) | left_hand_width (int) |
# right_hand_height (int) | right_hand_width (int)| left_hand_pos_x (float) | left_hand_pos_y (float) | ... |
# left_hand_depth_data ([left_hand_width * left_hand_height]) |
# right_hand_depth_data ([right_hand_width * right_hand_height])
"""

def decode_content_hand(raw_frame, offset):
    """
    raw_frame: frame starting from 4 to end (4 for length field)
    offset: index where header ends; header is header_l, timestamp, frame_type
    """
    endianness = "<"

    content_header_format = "iiff"  # width, height, posx, posy
    content_header_size = struct.calcsize(endianness + content_header_format)
    content_header = struct.unpack_from(endianness + content_header_format, raw_frame, offset)

    width, height, posx, posy = content_header
    #print(width, height, posx, posy)

    depth_data_format = str(width * height) + "H"
    depth_data = struct.unpack_from(endianness + depth_data_format, raw_frame, offset + content_header_size)
    
    offset = offset + content_header_size + struct.calcsize(endianness + depth_data_format)  # new offset from where tail starts
    return (width, height, posx, posy, list(depth_data)), offset


def decode_content_body(raw_frame, offset):
    """
    raw_frame: frame starting from 4 to end (4 for length field)
    offset: index where header ends; header is header_l, timestamp, frame_type
    """
    endianness = "<"

    content_header_format = "BB"  # Tracked body count | Engaged
    content_header_size = struct.calcsize(endianness + content_header_format)
    content_header = struct.unpack_from(endianness + content_header_format, raw_frame, offset)

    tracked_body_count, engaged = content_header

    # For each body, a header is transmitted
    # TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    body_format = "Q4B"

    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BB7f"

    frame_format = body_format + (joint_format * 25)

    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack_from(endianness + (frame_format * engaged), raw_frame, offset + content_header_size)

    # decoded = (tracked_body_count, engaged) + frame_pieces
    decoded = (tracked_body_count, engaged, frame_pieces)
    offset = offset + content_header_size + struct.calcsize(
        endianness + frame_format * engaged)  # new offset from where tail starts
    return decoded, offset


def parse_argument():
    parser = argparse.ArgumentParser()
    parser.add_argument('hand', help='Hand to follow', choices=['LH', 'RH'])
    parser.add_argument('kinect_host', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', help='Host name of the machine running Fusion Server', default=None)

    return parser.parse_args()


def load_forest(hand_type):
    """
    Load random forest classifier from disk
    :return: loaded forest
    """
    load_path = '/s/red/a/nobackup/vision/jason/forest/%s_forest.pickle' % hand_type
    print('Loading random forest checkpoint: %s' % load_path)
    f = open(load_path, 'rb')
    forest = pickle.load(f)
    f.close()

    # status variables are used for one-shot learning
    forest.is_fresh = True  # whether the forest is a fresh copy
    forest.is_learning = False  # whether the forest is taking images for learning
    forest.is_ready = True  # whether the forest is ready to be used for classification

    print('%s forest loaded!' % hand_type)

    return forest


def conditional_load_forest(hand_type, engaged):
    global_lock.acquire()
    if


def preprocess_hand_arr(hand_arr, posx, posy):
    posz = hand_arr[int(posx), int(posy)]
    hand_arr -= posz
    hand_arr /= 150
    hand_arr = np.clip(hand_arr, -1, 1)
    hand_arr = resize(hand_arr, (168, 168))
    hand_arr = hand_arr[20:-20, 20:-20]
    hand_arr = hand_arr.reshape((1, 128, 128, 1))

    return hand_arr


def find_label_sync(forest, feature):
    label_index, dist = None, None
    if global_lock.acquire(blocking=False) and forest.is_ready:
        label_index, dist = forest.find_nn(feature)
        global_lock.release()
    return label_index, dist


class OneShot:
    def __init__(self, hand_type, forest, forest_status):
        # First find out the corresponding kinect v2 joint index of the hand
        self.hand_type = hand_type
        if hand_type == 'RH':
            self.palm_ind = 11
        elif hand_type == 'LH':
            self.palm_ind = 7
        # the start and end position of palm center coordinate in decoded body frame
        self.palm_coordinates_ind_start = self.palm_ind*9 + 7
        self.palm_coordinates_ind_end = self.palm_ind*9 + 10

        self.pixel_intensity_threshold = 0.4  # used in self._is_gesture()
        self.buffer_length = 10  # determines the number of frames in the buffer
        self.moving_variance_threshold = 0.001  # used in self._can_start(), starts learning when the arm stops moving

        self.receiving_frames = False  # Only start to receive frames when a signal is sent from kinect server
        self.learning = False  # Keep learning until all reference features were added in the forest
        self.skip_frame = 0  # skip some frames to maximize variance in learning input
        self.ref_frames = []  # a buffer list of frames to learn
        self.palm_centers = []  # a buffer list of palm center coordinates

        self.forest = None  # random forest instance

    def add_frame(self, hand_arr, skeleton_arr, start_learn):
        """
        When learning starts, hand depth array and body skeleton array needs to used to extract necessary information
        and stored for further processing.
        :param hand_arr: hand depth array from self.decode_frame_hand()
        :param skeleton_arr: body skeleton array from self.decode_frame_skeleton()
        :param start_learn: A signal from kinect to indicate whether the learning initiates
        :return: None
        """
        if start_learn:
            self.receiving_frames = True
            self.learning = True
        if not self.receiving_frames:
            return
        if self._is_gesture(hand_arr) and not self.skip_frame:
            self.ref_frames.append(hand_arr)
            self.palm_centers.append(skeleton_arr[self.palm_coordinates_ind_start:self.palm_coordinates_ind_end])
            if len(self.palm_centers) > self.buffer_length:
                self.ref_frames.pop(0)
                self.palm_centers.pop(0)

                # Assumes the start time of learning is when the hand stops moving. This is determined by the palm
                # center buffer variance, which needs to be smaller than certain threshold.
                if self._palm_center_buffer_variance() < self.moving_variance_threshold:
                    # Start to learn, stop receiving frames
                    self.receiving_frames = False
                    ######### start a thread for learning

        else:
            """
            In current frame, the hand is still next to body. Proceed without processing and reset buffer
            """
            self.ref_frames = []
            self.palm_centers = []

        self.skip_frame = (self.skip_frame + 1) % 3  # skip every 2 frames to maximize variance in learning input

    def _is_gesture(self, hand_arr):
        """
        The hand is performing a gesture only if it's not next to body. This is by no means a perfect solution but
        should work as a precondition. Based on previous gesture images, the pixels are almost white around the edges
        of the image, and their values after pre-processing are around 0.5. Therefore the idea here is to check 4 corner
        pixels and see if at least 3 of them meet the condition (i.e. <0.4, the arm could occupy a corner, in which
        case only 3 corners meet condition).
        :param hand_arr: hand image aray
        :return: A boolean about whether the hand is performing a gesture
        """
        return np.sum([hand_arr[i, j] > self.pixel_intensity_threshold for i in [0, -1] for j in [0, -1]]) >= 3

    def _palm_center_buffer_variance(self):
        """
        This method calculates the variance of distances of palm centers between two consecutive frames.
        :return: The variance
        """
        if len(self.palm_centers) < self.buffer_length:
            return None
        distances = np.linalg.norm(np.array(self.palm_centers[:-1]) - np.array(self.palm_centers[1:]), axis=0)
        return np.std(distances)

    def _learn(self):
        pass




if __name__ == '__main__':

    args = parse_argument()

    sys.modules['RandomForest'] = RandomForest  # to load the pickle file

    hand = args.hand

    stream_id = streams.get_stream_id(hand)

    # load gesture labels
    gestures = np.load(os.path.abspath('./data/labels_{}.npy'.format(hand)))
    gestures = [g.decode('ascii').replace(".npy", "") for g in gestures]
    num_gestures = len(gestures)
    gestures += ['blind']
    print(hand, num_gestures)

    hand_classfier = RealTimeHandRecognition(hand, num_gestures)

    kinect_socket_hand = connect('kinect', args.kinect_host, hand)
    kinect_socket_body = connect('kinect', args.kinect_host, 'Body')
    fusion_socket = connect('fusion', args.fusion_host, hand) if args.fusion_host is not None else None

    # One-shot learning code. The one-shot learning code reads from the queue to process.
    feature_queue = queue.Queue()
    forest = load_forest(hand)
    ####  start one-shot learning thread

    i = 0
    hands_list = []

    start_time = time.time()
    while True:
        try:
            (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data_hand,) = \
                decode.read_frame(kinect_socket_hand, decode_content_hand)
            (timestamp, frame_type), (tracked_body_count, engaged, frame_pieces), (writer_data_body,) = \
                decode.read_frame(kinect_socket_body, decode_content_body)
            # print("timestamp, frame_type", timestamp, frame_type)
            # print("width, height, posx, posy", width, height, posx, posy)
            # print("writer_data", writer_data)
            
        except KeyboardInterrupt:
           break

        if not engaged:
            continue

        if writer_data_hand == 'learn':
            global_lock.acquire()
            forest.is_fresh = False
            forest.is_learning = True
            forest.is_ready = False
            global_lock.release()

        probs = [0] * num_gestures  # probabilities to send to fusion

        if posx == -1 and posy == -1:
            probs += [1]
            max_index = len(probs)-1
        else:
            hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
            hand_arr = preprocess_hand_arr(hand_arr, posx, posy)
            # print(hand_arr.shape, posx, posy)

            feature = hand_classfier.classify(hand_arr)
            feature_queue.put(feature)

            found_index, dist = find_label_sync(forest, feature)
            if found_index is not None:
                probs[max_index] = (0.5 - dist[0] / 2046.0)  # feature vector has a dimension of 1024, so dist[0]/1023/2

            probs = list(probs)+[0]

        print(i, timestamp, gestures[max_index], probs[max_index])
        i += 1

        if i % 100==0:
            print("="*100, "FPS", 100/(time.time()-start_time))
            start_time = time.time()

        pack_list = [stream_id, timestamp,max_index]+list(probs)

        bytes = struct.pack("<iqi"+"f"*(num_gestures+1), *pack_list)

        if fusion_socket is not None:
            fusion_socket.send(bytes)

    kinect_socket_hand.close()
    kinect_socket_body.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)

