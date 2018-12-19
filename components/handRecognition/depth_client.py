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
    

# By default read 100 frames
if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument('hand', help='Hand to follow', choices=['LH', 'RH'])
    parser.add_argument('kinect_host', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', help='Host name of the machine running Kinect Server', default=None)

    args = parser.parse_args()

    hand = args.hand

    stream_id = streams.get_stream_id(hand)
    gestures = np.load(os.path.abspath('./data/labels_{}.npy'.format(hand)))
    gestures = [g.decode('ascii').replace(".npy", "") for g in gestures]
    num_gestures = len(gestures)

    gestures += ['blind']
    print(hand, num_gestures)

    hand_classfier = RealTimeHandRecognition(hand, num_gestures)
    kinect_socket = connect('kinect', args.kinect_host, hand)

    fusion_socket = connect('fusion', args.fusion_host, hand) if args.fusion_host is not None else None

    i = 0
    hands_list = []

    start_time = time.time()
    while True:
        try:
            (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data,) = decode.read_frame(kinect_socket, decode_content)
            print("timestamp, frame_type", timestamp, frame_type)
            print("width, height, posx, posy", width, height, posx, posy)
            print("writer_data", writer_data)
            
        except KeyboardInterrupt:
           break
        
        if posx == -1 and posy == -1:
            probs = [0]*num_gestures+[1]
            max_index = len(probs)-1

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
            max_index, probs = hand_classfier.classify(hand_arr)

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

    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)

