#!/usr/bin/env python

import socket
import struct
import time
import gzip
import cv2
import sys
import numpy as np
from realtime_hand_recognition import RealTimeHandRecognition
from support.constants import *

stream_id = 64 | 128;

def connect(server="kinect"):
    """
    Connect to a specific port
    """
    if server == "kinect":
        src_addr = KINECT_SRC_ADDR
        src_port = KINECT_DEPTH_PORT
    else:
        src_addr = FUSION_SRC_ADDR
        src_port = FUSION_INPUT_PORT

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(10)

    try:
        sock.connect((src_addr, src_port))
    except:
        print "Error connecting to {}:{}".format(src_addr, src_port)
        return None

    if server == "kinect":
        try:
            print "Sending stream info"
            sock.sendall(struct.pack('<i', stream_id));
        except:
            print "Error: Stream rejected"
            return None

    print "Successfully connected to host ", server
    return sock


# timestamp (long) | depth_hands_count(int) | left_hand_height (int) | left_hand_width (int) |
# right_hand_height (int) | right_hand_width (int)| left_hand_pos_x (float) | left_hand_pos_y (float) | ... |
# left_hand_depth_data ([left_hand_width * left_hand_height]) |
# right_hand_depth_data ([right_hand_width * right_hand_height])
def decode_frame(raw_frame):
    # Expect network byte order
    endianness = "<"

    # In each frame, a header is transmitted
    header_format = "qiiiff"
    header_size = struct.calcsize(endianness + header_format)
    header = struct.unpack(endianness + header_format, raw_frame[:header_size])

    timestamp, frame_type, width, height, posx, posy = header

    depth_data_format = str(width * height) + "H"

    depth_data = struct.unpack_from(endianness + depth_data_format, raw_frame, header_size)

    return (timestamp, frame_type, width, height, posx, posy, list(depth_data))


def recv_all(sock, size):
    result = b''
    while len(result) < size:
        data = sock.recv(size - len(result))
        if not data:
            raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
        result += data
    return result


def recv_depth_frame(sock):
    """
    Experimental function to read each stream frame from the server
    """
    (frame_size,) = struct.unpack("<i", recv_all(sock, 4))
    return recv_all(sock, frame_size)


# By default read 100 frames
if __name__ == '__main__':

    hand = sys.argv[1]
    gestures = list(np.load("/s/red/a/nobackup/cwc/hands/real_time_training_data/%s/gesture_list.npy" % hand))
    gestures = [g.replace(".npy", "") for g in gestures]
    num_gestures = len(gestures)

    gestures += ['blind']
    print hand, num_gestures

    if hand == "RH":
        id = FUSION_RIGHT_HAND_ID
    else:
        id = FUSION_LEFT_HAND_ID

    if hand == "LH":
        stream_id = 64
    elif hand == "RH":
        stream_id = 128


    hand_classfier = RealTimeHandRecognition(hand, num_gestures)
    kinect_socket = connect()
    fusion_socket = connect("Fusion")

    i = 0


    hands_list = []

    start_time = time.time()
    while True:
        try:
            frame = recv_depth_frame(kinect_socket)
        except KeyboardInterrupt:
           break
        except:
            continue

        timestamp, frame_type, width, height, posx, posy, depth_data = decode_frame(frame)

        hand = np.array(depth_data, dtype=np.float32).reshape((height, width))
        print hand.shape, posx, posy
        posz = hand[int(posx), int(posy)]
        hand = cv2.resize(hand, (168, 168))
        hand = hand[20:-20, 20:-20]
        hand -= posz
        hand /= 150

        hand = hand.reshape((1,128,128,1))
        max_index, probs = hand_classfier.classify(hand)
        probs = list(probs)+[0]

        print i, timestamp, gestures[max_index], probs[max_index]
        i += 1

        if i%100==0:
            print "="*100,"FPS",100/(time.time()-start_time)
            start_time = time.time()


        pack_list = [id,timestamp,max_index]+list(probs)

        bytes = struct.pack("!iqi"+"f"*(num_gestures+1), *pack_list)

        if fusion_socket is not None:
            fusion_socket.send(bytes)

    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)

