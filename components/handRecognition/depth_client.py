import struct
import threading
import time
import argparse
import numpy as np
from skimage.transform import resize
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


def get_frame(kinect_socket):
    return decode.read_frame(kinect_socket, decode_content_hand)


def read_process_send(fusion_socket, classifier, gestures, stream_id, engaged, frame_pieces, timestamp, writer_data_hand, classified, blind):
    try:
        bytes = classifier.get_bytes(timestamp, writer_data_hand, engaged, frame_pieces, gestures,
                                     stream_id, classified, blind)

        if fusion_socket is not None:
            fusion_socket.send(bytes)
    except KeyboardInterrupt:
        return False

    return True


def _preprocess_hand_arr(depth_data, posx, posy, height, width):
    hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
    posz = hand_arr[int(posx), int(posy)]
    hand_arr -= posz
    hand_arr /= 150
    hand_arr = np.clip(hand_arr, -1, 1)
    hand_arr = resize(hand_arr, (168, 168))
    hand_arr = hand_arr[20:-20, 20:-20]
    hand_arr = hand_arr.reshape((1, 128, 128, 1))

    return hand_arr


def can_process(hand, frame_pieces, posx, posy):
    # if hands are below midpoint between spine mid and spine base, send blind
    if len(frame_pieces) != 0:
        spine_base_ind = 0
        spine_mid_ind = 1
        if hand == "RH":
            hand_ind = 11
        else:
            hand_ind = 7
        spine_base_y = frame_pieces[spine_base_ind * 9 + 8]
        spine_mid_y = frame_pieces[spine_mid_ind * 9 + 8]
        hand_y = frame_pieces[hand_ind * 9 + 8]
    else:
        return False

    if (posx == -1 and posy == -1) or (hand_y <= spine_base_y + abs(spine_mid_y - spine_base_y) / 2):
        return False
    return True


def main(args):
    start_time = time.time()
    frame_count = 0  # To calculate the fps
    lock = threading.Lock()

    if args.disable_one_shot:
        print('running base classifier')
        HandModel = RealTimeHandRecognition
        Classifier = BaseClassifier
    else:
        print('running one-shot classifier')
        HandModel = RealTimeHandRecognitionOneShot
        Classifier = OneShotClassifier

    if args.hand == "BOTH":
        print('tracking both hands')
        RH_model = HandModel("RH", 32, 2)
        RH_classifier = Classifier("RH", lock)
        LH_classifier = Classifier("LH", lock, is_flipped=True)

        kinect_socket = connect('kinect', args.kinect_host, ("RH", "LH", "Body"))
        RH_fusion_socket = connect('fusion', args.fusion_host, "RH") if args.fusion_host is not None else None
        LH_fusion_socket = connect('fusion', args.fusion_host, "LH") if args.fusion_host is not None else None

        RH_gestures = postures.right_hand_postures
        LH_gestures = postures.left_hand_postures

        RH_stream_id = streams.get_stream_id("RH")
        LH_stream_id = streams.get_stream_id("LH")

        while True:
            RH_blind = False
            LH_blind = False

            _, (_, engaged, frame_pieces), _ = \
                decode.read_frame(kinect_socket, decode_content_body)

            (LH_timestamp, LH_frame_type), (LH_width, LH_height, LH_posx, LH_posy, LH_depth_data), (LH_writer_data_hand,) = get_frame(kinect_socket)
            (RH_timestamp, RH_frame_type), (RH_width, RH_height, RH_posx, RH_posy, RH_depth_data), (RH_writer_data_hand,) = get_frame(kinect_socket)

            if can_process("LH", frame_pieces, LH_posx, LH_posy):
                if LH_blind:
                    RH_model.past_probs_L = None  # reset smoothing
                LH_frame = _preprocess_hand_arr(LH_depth_data, LH_posx, LH_posy, LH_height, LH_width)
            else:
                LH_frame = np.empty((1,128,128,1))  # to facilitate forward pass
                LH_blind = True

            if can_process("RH", frame_pieces, RH_posx, RH_posy):
                if RH_blind:
                    RH_model.past_probs_R = None  # reset smoothing
                RH_frame = _preprocess_hand_arr(RH_depth_data, RH_posx, RH_posy, RH_height, RH_width)
            else:
                RH_frame = np.empty((1,128,128,1))  # to facilitate forward pass
                RH_blind = True

            LH_out, RH_out = RH_model.classifyLR(LH_frame, RH_frame)

            if not read_process_send(LH_fusion_socket, LH_classifier, LH_gestures, LH_stream_id, engaged,
                                     frame_pieces, LH_timestamp, LH_writer_data_hand, LH_out, LH_blind) \
                    or not read_process_send(RH_fusion_socket, RH_classifier, RH_gestures, RH_stream_id, engaged,
                                             frame_pieces, RH_timestamp, RH_writer_data_hand, RH_out, RH_blind):
                break

            print()

            frame_count += 1
            if frame_count == 100:
                print("\n", "=" * 100, "FPS", 100 / (time.time() - start_time))
                start_time = time.time()
                frame_count = 0

        kinect_socket.close()
        if RH_fusion_socket is not None:
            RH_fusion_socket.close()
        if LH_fusion_socket is not None:
            LH_fusion_socket.close()
    else:
        print('tracking single hand--', args.hand)
        model = HandModel(args.hand, 32, 1)
        classifier = Classifier(args.hand, lock)

        kinect_socket = connect('kinect', args.kinect_host, ("Body", args.hand)) if args.kinect_host is not None else None
        fusion_socket = connect('fusion', args.fusion_host, args.hand) if args.fusion_host is not None else None

        if args.hand == "LH":
            gestures = postures.left_hand_postures
        else:
            gestures = postures.right_hand_postures

        stream_id = streams.get_stream_id(args.hand)

        while True:
            out = None
            blind = False

            _, (_, engaged, frame_pieces), _ = \
                decode.read_frame(kinect_socket, decode_content_body)

            (timestamp, frame_type), (width, height, posx, posy, depth_data), (
            writer_data_hand,) = get_frame(kinect_socket)

            if can_process(args.hand, frame_pieces, posx, posy):
                frame = _preprocess_hand_arr(depth_data, posx, posy, height, width)
                out = model.classify(frame)
                blind = True
                model.past_probs = None

            if not read_process_send(fusion_socket, classifier, gestures, stream_id, engaged,
                                     frame_pieces, timestamp, writer_data_hand, out, blind):
                break

            print()

            frame_count += 1
            if frame_count == 100:
                print("\n", "=" * 40, "FPS", 100 / (time.time() - start_time))
                start_time = time.time()
                frame_count = 0

        kinect_socket.close()
        if fusion_socket is not None:
            fusion_socket.close()


if __name__ == "__main__":
    args = parse_argument()
    main(args)
