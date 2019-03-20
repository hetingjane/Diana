import struct
import time
import argparse
import numpy as np
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


def parse_argument():
    parser = argparse.ArgumentParser()
    parser.add_argument('--kinect_host', help='Host name of the machine running Kinect Server', default='localhost')
    parser.add_argument('--fusion-host', help='Host name of the machine running Fusion Server', default='localhost')
    parser.add_argument('--enable-one-shot', help='Start client in one-shot learning mode', action='store_true', default=True)

    return parser.parse_args()


def recv_decode_send(kinect_socket, fusion_socket, classifier, stream_id, gestures, flip):
    _, (_, engaged, frame_pieces), _ = \
        decode.read_frame(kinect_socket, decode_content_body)
    (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data_hand,) = \
        decode.read_frame(kinect_socket, decode_content_hand)
    msg = classifier.get_bytes(timestamp, width, height, posx, posy, depth_data,
                                  writer_data_hand, engaged, frame_pieces, stream_id, gestures, flip)
    if fusion_socket is not None:
        fusion_socket.send(msg)

# By default read 100 frames
if __name__ == '__main__':

    args = parse_argument()

    RH_stream_id = streams.get_stream_id("RH")
    LH_stream_id = streams.get_stream_id("LH")

    from ..fusion.conf.postures import left_hand_postures, right_hand_postures

    RH_gestures = right_hand_postures
    LH_gestures = left_hand_postures

    RH_kinect_socket = connect('kinect', args.kinect_host, ("RH", "Body"))
    LH_kinect_socket = connect('kinect', args.kinect_host, ("LH", "Body"))

    RH_fusion_socket = connect('fusion', args.fusion_host, "RH") if args.fusion_host is not None else None
    LH_fusion_socket = connect('fusion', args.fusion_host, "LH") if args.fusion_host is not None else None

    if RH_kinect_socket is None or LH_kinect_socket is None or RH_fusion_socket is None or LH_fusion_socket is None:
        print("connection could not be established")
        exit(-1)

    if args.enable_one_shot:
        classifier = OneShotClassifier("RH")
    else:
        classifier = BaseClassifier("RH")

    i = 0

    start_time = time.time()
    while True:
        try:
            recv_decode_send(LH_kinect_socket, LH_fusion_socket, classifier, LH_stream_id, LH_gestures, flip=True)
            recv_decode_send(RH_kinect_socket, RH_fusion_socket, classifier, RH_stream_id, RH_gestures, flip=False)
            print()
        except KeyboardInterrupt:
            break

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

