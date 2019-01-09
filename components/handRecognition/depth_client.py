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


def parse_argument():
    parser = argparse.ArgumentParser()
    parser.add_argument('hand', help='Hand to follow', choices=['LH', 'RH'])
    parser.add_argument('kinect_host', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', help='Host name of the machine running Fusion Server', default=None)
    parser.add_argument('--enable-one-shot', help='Start client in one-shot learning mode', action='store_true')

    return parser.parse_args()


if __name__ == '__main__':

    args = parse_argument()
    hand = args.hand

    stream_id = streams.get_stream_id(hand)

    if args.enable_one_shot:
        classifier = OneShotClassifier(hand, stream_id)
    else:
        classifier = BaseClassifier(hand, stream_id)

    kinect_socket = connect('kinect', args.kinect_host, (hand, 'Body'))
    fusion_socket = connect('fusion', args.fusion_host, hand) if args.fusion_host is not None else None

    frame_count = 0  # To calculate the fps
    hands_list = []

    start_time = time.time()
    while True:
        try:
            (timestamp, frame_type), (tracked_body_count, engaged, frame_pieces), (writer_data_body,) = \
                decode.read_frame(kinect_socket, decode_content_body)
            (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data_hand,) = \
                decode.read_frame(kinect_socket, decode_content_hand)
        except KeyboardInterrupt:
            break

        frame_count += 1
        if frame_count == 100:
            print("="*100, "FPS", 100/(time.time()-start_time))
            start_time = time.time()
            frame_count = 0

        bytes = classifier.get_bytes(timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged,
                                     frame_pieces)

        if fusion_socket is not None:
            fusion_socket.send(bytes)

    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)

