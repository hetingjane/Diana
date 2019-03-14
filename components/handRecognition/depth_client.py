import struct
import time
import argparse
import os

from skimage.transform import resize
import sys
import numpy as np
from .realtime_hand_recognition import RealTimeHandRecognition
from ..fusion.conf.endpoints import connect
from ..fusion.conf import streams
from ..fusion.conf import decode

"""
# timestamp (long) | depth_hands_count(int) | left_hand_height (int) | left_hand_width (int) |
# right_hand_height (int) | right_hand_width (int)| left_hand_pos_x (float) | left_hand_pos_y (float) | ... |
# left_hand_depth_data ([left_hand_width * left_hand_height]) |
# right_hand_depth_data ([right_hand_width * right_hand_height])
"""

def decode_content(raw_frame, offset):
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


def decode_and_send_frame(frame, gestures, stream_id, fusion_socket, flip):
    print('decoding frame')
    timestamp, frame_type, width, height, posx, posy, depth_data = decode_frame(frame)

    if posx == -1 and posy == -1:
        probs = [0] * num_gestures + [1]
        max_index = len(probs) - 1
        print("can't find hand")

    else:
        hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
        print(hand_arr.shape, posx, posy)
        posz = hand_arr[int(posx), int(posy)]
        hand_arr -= posz
        hand_arr /= 150
        hand_arr = np.clip(hand_arr, -1, 1)
        hand_arr = resize(hand_arr, (168, 168))
        hand_arr = hand_arr[20:-20, 20:-20]
        hand_arr = hand_arr.reshape((1, 128, 128, 1))
        print('classifying frame')
        max_index, probs = hand_classfier.classify(hand_arr, flip)

        probs = list(probs) + [0]

    print(i, timestamp, gestures[max_index], probs[max_index])

    pack_list = [stream_id, timestamp, max_index] + list(probs)

    bytes = struct.pack("<iqi" + "f" * (num_gestures + 1), *pack_list)

    if fusion_socket is not None:
        fusion_socket.send(bytes)


# By default read 100 frames
if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument('--kinect_host', '-k', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', '-f', help='Host name of the machine running Kinect Server', default=None)

    args = parser.parse_args()

    RH_stream_id = streams.get_stream_id("RH")
    LH_stream_id = streams.get_stream_id("LH")
    RH_gestures = list(np.load("components/log/gesture_list_RH.npy"))
    RH_gestures = [str(g).replace(".npy", "") for g in RH_gestures]
    LH_gestures = list(np.load("components/log/gesture_list_LH.npy"))
    LH_gestures = [str(g).replace(".npy", "") for g in LH_gestures]
    num_gestures = len(RH_gestures)

    RH_gestures += ['blind']
    LH_gestures += ['blind']

    hand_classfier = RealTimeHandRecognition(num_gestures)
    RH_kinect_socket = connect('kinect', args.kinect_host, "RH")
    LH_kinect_socket = connect('kinect', args.kinect_host, "LH")

    RH_fusion_socket = connect('fusion', args.fusion_host, "RH") if args.fusion_host is not None else None
    LH_fusion_socket = connect('fusion', args.fusion_host, "LH") if args.fusion_host is not None else None
    print('connected to fusion and kinect')
    i = 0
    hands_list = []
    while True:
        try:
            print('receiving frames')
            RH_frame = recv_depth_frame(RH_kinect_socket)
            LH_frame = recv_depth_frame(LH_kinect_socket)
        except KeyboardInterrupt:
           break
        except:
            continue

        decode_and_send_frame(RH_frame, RH_gestures, RH_stream_id, RH_fusion_socket, flip=False)
        decode_and_send_frame(LH_frame, LH_gestures, LH_stream_id, LH_fusion_socket, flip=True)
        timer.wait(FPS=30)

    RH_kinect_socket.close()
    LH_kinect_socket.close()
    if RH_fusion_socket is not None:
        RH_fusion_socket.close()
    if LH_fusion_socket is not None:
        LH_fusion_socket.close()

    sys.exit(0)



