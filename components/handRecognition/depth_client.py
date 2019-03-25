import struct
import time
import argparse

import sys

from components.fusion.conf import postures
from components.handRecognition.realtime_hand_recognition import RealTimeHandRecognition, RealTimeHandRecognitionOneShot
from components.skeletonRecognition.skeleton_client import decode_content as decode_content_body
from components.handRecognition.base_classifier import BaseClassifier
from components.handRecognition.one_shot_classifier import OneShotClassifier
from components.fusion.conf.endpoints import connect
from components.fusion.conf import streams
from components.fusion.conf import decode


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
    # print(width, height, posx, posy)

    depth_data_format = str(width * height) + "H"
    depth_data = struct.unpack_from(endianness + depth_data_format, raw_frame, offset + content_header_size)

    offset = offset + content_header_size + struct.calcsize(
        endianness + depth_data_format)  # new offset from where tail starts
    return (width, height, posx, posy, list(depth_data)), offset


def parse_argument():
    parser = argparse.ArgumentParser()
    parser.add_argument('--hand', help='Which hand to track (LH, RH, BOTH), default is BOTH', default="BOTH")
    parser.add_argument('--kinect-host', help='Host name of the machine running Kinect Server', default="localhost")
    parser.add_argument('--fusion-host', help='Host name of the machine running Fusion Server', default="localhost")
    parser.add_argument('--disable-one-shot', help='Disable one-shot learning mode', action='store_true', default=False)

    return parser.parse_args()


def read_process_send(hand, kinect_socket, fusion_socket, classifier, gestures, stream_id, engaged, frame_pieces, flip=False):
    try:
        (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data_hand,) = \
            decode.read_frame(kinect_socket, decode_content_hand)


        bytes = classifier.get_bytes(timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged,
                                frame_pieces, hand, gestures, stream_id, flip)

        if fusion_socket is not None:
            fusion_socket.send(bytes)
    except KeyboardInterrupt:
        return False

    return True


def print_fps(frame_count, start_time):
    frame_count += 1
    if frame_count == 100:
        print("\n", "=" * 100, "FPS", 100 / (time.time() - start_time))
        start_time = time.time()
        frame_count = 0


def main(args):
    start_time = time.time()
    frame_count = 0  # To calculate the fps

    if args.disable_one_shot:
        print('running base classifier')
        HandModel = RealTimeHandRecognition
        Classifier = BaseClassifier
    else:
        print('running one-shot classifier')
        HandModel = RealTimeHandRecognitionOneShot
        Classifier = OneShotClassifier

    body_kinect_socket = connect('kinect', args.kinect_host, "Body")

    if args.hand == "BOTH":
        print('tracking both hands')
        RH_model = HandModel("RH", 32)
        LH_model = RH_model
        RH_classifier = Classifier(RH_model, "RH")
        LH_classifier = Classifier(LH_model, "LH")

        RH_kinect_socket = connect('kinect', args.kinect_host, "RH")
        LH_kinect_socket = connect('kinect', args.kinect_host, "LH")
        RH_fusion_socket = connect('fusion', args.fusion_host, "RH") if args.fusion_host is not None else None
        LH_fusion_socket = connect('fusion', args.fusion_host, "LH") if args.fusion_host is not None else None

        RH_gestures = postures.right_hand_postures
        LH_gestures = postures.left_hand_postures

        RH_stream_id = streams.get_stream_id("RH")
        LH_stream_id = streams.get_stream_id("LH")

        while True:
            _, (_, engaged, frame_pieces), _ = \
                decode.read_frame(body_kinect_socket, decode_content_body)

            if not read_process_send("LH", LH_kinect_socket, LH_fusion_socket, LH_classifier, LH_gestures, LH_stream_id,
                                     engaged, frame_pieces, flip=True) \
                    or not read_process_send("RH", RH_kinect_socket, RH_fusion_socket, RH_classifier, RH_gestures,
                                             RH_stream_id, engaged, frame_pieces):
                break

            print()

            print_fps(frame_count, start_time)

        RH_kinect_socket.close()
        LH_kinect_socket.close()
        if RH_fusion_socket is not None:
            RH_fusion_socket.close()
        if LH_fusion_socket is not None:
            LH_fusion_socket.close()
    else:
        print('tracking single hand--', args.hand)
        model = HandModel(args.hand, 32)
        classifier = Classifier(model, args.hand)

        kinect_socket = connect('kinect', args.kinect_host, args.hand)
        fusion_socket = connect('fusion', args.fusion_host, args.hand) if args.fusion_host is not None else None

        if args.hand == "LH":
            gestures = postures.left_hand_postures
        else:
            gestures = postures.right_hand_postures

        stream_id = streams.get_stream_id(args.hand)

        while True:
            _, (_, engaged, frame_pieces), _ = \
                decode.read_frame(body_kinect_socket, decode_content_body)

            if not read_process_send(args.hand, kinect_socket, fusion_socket, classifier, gestures, stream_id,
                                     engaged, frame_pieces):
                break

            print()

            print_fps(frame_count, start_time)

        kinect_socket.close()
        if fusion_socket is not None:
            fusion_socket.close()


if __name__ == "__main__":
    args = parse_argument()
    main(args)
