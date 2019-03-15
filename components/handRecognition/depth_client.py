import struct
import time
import argparse

import sys
from ..skeletonRecognition.skeleton_client import decode_content as decode_content_body
from .base_classifier import BaseClassifier
from .one_shot_classifier import OneShotClassifier
from ..fusion.conf.endpoints import connect
from ..fusion.conf import streams
from ..fusion.conf import decode


def decode_content_hand(raw_frame, offset):
    """
    :param raw_frame: frame starting from 4 to end (4 for length field)
    :param offset: index where header ends; header is header_l, timestamp, frame_type
    # timestamp (long) | depth_hands_count(int) | left_hand_height (int) | left_hand_width (int) |
    # right_hand_height (int) | right_hand_width (int)| left_hand_pos_x (float) | left_hand_pos_y (float) | ... |
    # left_hand_depth_data ([left_hand_width * left_hand_height]) |
    # right_hand_depth_data ([right_hand_width * right_hand_height])
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


def recv_process_reply(classifier, engaged, fusion_socket, kinect_socket, gestures, stream_id, name, flip):
    try:
        (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data,) = decode.read_frame(
            kinect_socket, decode_content)
        #print("timestamp, frame_type", timestamp, frame_type)
        #print("width, height, posx, posy", width, height, posx, posy)
        #print("writer_data", writer_data)

    except KeyboardInterrupt:
        return False

    if posx == -1 and posy == -1:
        probs = [0.0] * num_gestures + [1.0]
        max_index = len(probs) - 1

    else:
        hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
        #print(hand_arr.shape, posx, posy)
        posz = hand_arr[int(posx), int(posy)]
        hand_arr -= posz
        hand_arr /= 150
        hand_arr = np.clip(hand_arr, -1, 1)
        hand_arr = resize(hand_arr, (168, 168))
        hand_arr = hand_arr[20:-20, 20:-20]
        hand_arr = hand_arr.reshape((1, 128, 128, 1))
        max_index, probs = hand_classfier.classify(hand_arr, flip)

        probs = list(probs) + [0.0]

    print(name, gestures[max_index], '{:.2}'.format(probs[max_index]), end='\t')

    msg = classifier.get_bytes(timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged,
                                 frame_pieces)

    if fusion_socket is not None:
        fusion_socket.send(msg)

    return True


def parse_argument():
    parser = argparse.ArgumentParser()
    parser.add_argument('hand', help='Hand to follow', choices=['LH', 'RH'])
    parser.add_argument('kinect_host', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', help='Host name of the machine running Fusion Server', default=None)
    parser.add_argument('--enable-one-shot', help='Start client in one-shot learning mode', action='store_true')

    return parser.parse_args()


# By default read 100 frames
if __name__ == '__main__':

    args = parse_argument()

    RH_stream_id = streams.get_stream_id("RH")
    LH_stream_id = streams.get_stream_id("LH")
    RH_gestures = list(np.load("components/log/gesture_list_RH.npy"))
    RH_gestures = [str(g).replace(".npy", "") for g in RH_gestures]
    LH_gestures = list(np.load("components/log/gesture_list_LH.npy"))
    LH_gestures = [str(g).replace(".npy", "") for g in LH_gestures]
    num_gestures = len(RH_gestures)

    RH_gestures += ['blind']
    LH_gestures += ['blind']

    RH_kinect_socket = connect('kinect', args.kinect_host, "RH")
    LH_kinect_socket = connect('kinect', args.kinect_host, "LH")

    RH_fusion_socket = connect('fusion', args.fusion_host, "RH") if args.fusion_host is not None else None
    LH_fusion_socket = connect('fusion', args.fusion_host, "LH") if args.fusion_host is not None else None

    if args.enable_one_shot:
        LH_classifier = OneShotClassifier(hand, LH_stream_id)
        RH_classifier = OneShotClassifier(hand, RH_stream_id)
    else:
        LH_classifier = BaseClassifier(hand, LH_stream_id)
        RH_classifier = BaseClassifier(hand, RH_stream_id)

    i = 0

    start_time = time.time()
    while True:
        try:
            (timestamp, frame_type), (tracked_body_count, engaged, frame_pieces), (writer_data_body,) = \
                decode.read_frame(kinect_socket, decode_content_body)
        except KeyboardInterrupt:
            break

        if not recv_process_reply(classifier, engaged, LH_fusion_socket, LH_kinect_socket, LH_gestures, LH_stream_id, 'LH', flip=True):
            break
        if not recv_process_reply(classifier, engaged, RH_fusion_socket, RH_kinect_socket, RH_gestures, RH_stream_id, 'RH', flip=False):
            break

        print()
        i += 1

        if i % 100 == 0:
            print("=" * 80, "FPS", 100 / (time.time() - start_time))
            start_time = time.time()

    RH_kinect_socket.close()
    LH_kinect_socket.close()
    if RH_fusion_socket is not None:
        RH_fusion_socket.close()
    if LH_fusion_socket is not None:
        LH_fusion_socket.close()

    sys.exit(0)

