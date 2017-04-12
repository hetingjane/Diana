#!/usr/bin/env python

import socket, sys, struct
import time, gzip
import numpy as np
import cv2
from realtime_hand_recognition import RealTimeHandRecognition
import matplotlib.pyplot as plt
import sys

def connect(server="kinect"):
    """
    Connect to a specific port
    """
    if server == "kinect":
        src_addr = '129.82.45.102'
        src_port = 8125
    else:
        src_addr = '129.82.45.104'#'10.1.118.19'#'cwc1'
        src_port = 9125

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(10)

    try:
        sock.connect((src_addr, src_port))
        # Socket will only be used to read, so make it unidirectional
        #sock.shutdown(socket.SHUT_WR)
    except:
        print "Error connecting to {}:{}".format(src_addr, src_port)
        return None
    print "Successfully connected to host ", server
    return sock


# timestamp (long) | depth_hands_count(int) | left_hand_height (int) | left_hand_width (int) |
# right_hand_height (int) | right_hand_width (int)| left_hand_pos_x (float) | left_hand_pos_y (float) | ... |
# left_hand_depth_data ([left_hand_width * left_hand_height]) |
# right_hand_depth_data ([right_hand_width * right_hand_height])
def decode_frame(raw_frame):
    # Expect network byte order
    endianness = "!"

    # In each frame, a header is transmitted
    header_format = "qiiiii"
    header_size = struct.calcsize(endianness + header_format)
    header = struct.unpack(endianness + header_format, raw_frame[:header_size])

    timestamp, depth_hands_count, left_hand_height, left_hand_width, right_hand_height, right_hand_width = header

    left_hand_pos_format = "ffH"
    left_hand_pos_size = struct.calcsize(left_hand_pos_format)
    left_hand_pos = struct.unpack(endianness + left_hand_pos_format,
                                  raw_frame[header_size: header_size + left_hand_pos_size])

    right_hand_pos_format = "ffH"
    right_hand_pos_size = struct.calcsize(right_hand_pos_format)
    right_hand_pos = struct.unpack(endianness + left_hand_pos_format, raw_frame[
                                                                      header_size + left_hand_pos_size: header_size + left_hand_pos_size + right_hand_pos_size])
    depth_hands_size = left_hand_pos_size + right_hand_pos_size
    depth_hands = left_hand_pos + right_hand_pos

    left_hand_depth_data_format = str(left_hand_width * left_hand_height) + "H"
    right_hand_depth_data_format = str(right_hand_width * right_hand_height) + "H"

    left_hand_depth_data = ()
    right_hand_depth_data = ()

    if left_hand_width * left_hand_height > 0 and right_hand_height * right_hand_width > 0:
        depth_data = struct.unpack_from(endianness + left_hand_depth_data_format + right_hand_depth_data_format,
                                        raw_frame, header_size + depth_hands_size)
        left_hand_depth_data = depth_data[:left_hand_width * left_hand_height]
        right_hand_depth_data = depth_data[left_hand_width * left_hand_height:]
    #print timestamp, depth_hands_count, depth_hands, left_hand_width, left_hand_height, right_hand_width, right_hand_height

    return (timestamp, depth_hands_count) + depth_hands + (
    left_hand_width, left_hand_height, right_hand_width, right_hand_height, left_hand_depth_data, right_hand_depth_data)


def recv_all(sock, size):
    result = b''
    while len(result) < size:
        data = sock.recv(size - len(result))
        if not data:
            raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
        result += data
    return result


def check_savings(raw_frame):
    return ((len(raw_frame) - len(gzip.compress(raw_frame))) / len(raw_frame)) * 100

def recv_depth_frame(sock):
    """
    Experimental function to read each stream frame from the server
    """
    (frame_size,) = struct.unpack("!i", recv_all(sock, 4))

    return recv_all(sock, frame_size)


# By default read 100 frames
if __name__ == '__main__':

    hand = sys.argv[1]
    gestures = list(np.load("/s/red/a/nobackup/cwc/hands/real_time_training_data/%s/gesture_list.npy" % hand))
    gestures = [g.replace(".npy", "") for g in gestures]
    num_gestures = len(gestures)

    print hand, num_gestures


    hand_classfier = RealTimeHandRecognition(hand, num_gestures)
    kinect_socket = connect()
    fusion_socket = connect("Fusion")

    i = 0

    if hand == "RH":
        id = 4
    else:
        id = 2
    hands_list = []

    start_time = time.time()
    while True:
        try:
            frame = recv_depth_frame(kinect_socket)
            decoded_frame = decode_frame(frame)
        except KeyboardInterrupt:
           break
        except:
            continue

        timestamp = decoded_frame[0]

        offset = 2 + (decoded_frame[1] * 2) + 2
        lwidth = decoded_frame[offset]
        lheight = decoded_frame[offset + 1]
        rwidth = decoded_frame[offset + 2]
        rheight = decoded_frame[offset + 3]

        left_palm_z = decoded_frame[offset-4]
        right_palm_z = decoded_frame[offset-1]


        if hand == "LH":
            if decoded_frame[offset + 4]:
                left_hand = np.array(decoded_frame[offset + 4],dtype=np.float32).reshape((lheight, lwidth))
                left_hand = cv2.resize(left_hand, (168,168))
                left_hand = left_hand[20:-20, 20:-20]
                left_hand -= left_palm_z
                left_hand /= 150
                left_hand = left_hand.reshape((1,128,128,1))
                max_index, probs = hand_classfier.classify(left_hand)
            else:
                continue

        if hand == "RH":
            if decoded_frame[offset + 5]:
                right_hand = np.array(decoded_frame[offset + 5],dtype=np.float32).reshape((rheight, rwidth))
                right_hand = cv2.resize(right_hand, (168, 168))
                right_hand = right_hand[20:-20, 20:-20]
                right_hand -= right_palm_z
                right_hand /= 150
                right_hand = right_hand.reshape((1, 128, 128, 1))
                max_index, probs = hand_classfier.classify(right_hand)
            else:
                continue

        print i, timestamp, gestures[max_index], probs[max_index]
        i += 1

        if i%100==0:
            print "="*100,"FPS",100/(time.time()-start_time)
            start_time = time.time()

        #timestamp = i-1
        pack_list = [id,timestamp,max_index]+list(probs)
        #print pack_list

        bytes = struct.pack("!iqi"+"f"*num_gestures, *pack_list)

        if fusion_socket is not None:
            fusion_socket.send(bytes)

    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)

