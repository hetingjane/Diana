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
from .RandomForest.threaded_one_shot import OneShot

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
    # forest.is_learning = False  # whether the forest is taking images for learning
    forest.is_ready = True  # whether the forest is ready to be used for classification

    print('%s forest loaded!' % hand_type)

    return forest


# def conditional_load_forest(hand_type, engaged):
#     global_lock.acquire()
#     if


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
    one_shot_queue = queue.Queue()
    forest = load_forest(hand)
    learn_status = False  # whether to learn
    one_shot_thread = OneShot(hand, hand_classfier, forest, one_shot_queue, global_lock)

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
            forest.is_ready = False
            global_lock.release()
            learn_status = True
        else:
            learn_status = False

        probs = [0] * num_gestures  # probabilities to send to fusion

        if posx == -1 and posy == -1:
            probs += [1]
            max_index = len(probs)-1
        else:
            hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
            hand_arr = preprocess_hand_arr(hand_arr, posx, posy)
            # print(hand_arr.shape, posx, posy)

            one_shot_queue.put((hand_arr, frame_pieces, learn_status))

            feature = hand_classfier.classify(hand_arr)
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

