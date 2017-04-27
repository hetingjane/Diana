import socket
import struct
import time
import gzip
import cv2
import sys
import numpy as np
from collections import deque

from realtime_head_recognition import RealTimeHeadRecognition
from support.constants import *
import matplotlib.pyplot as plt

def connect(server="kinect"):
    """
    Connect to a specific port
    """
    print "Attempting to connect to ", server
    if server == "kinect":
        src_addr = KINECT_SRC_ADDR
        src_port = KINECT_HEAD_DEPTH_PORT
    else:
        src_addr = FUSION_SRC_ADDR
        src_port = FUSION_INPUT_PORT

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    #sock.settimeout(10)

    try:
        sock.connect((src_addr, src_port))
        # Socket will only be used to read, so make it unidirectional
        #sock.shutdown(socket.SHUT_WR)
    except:
        print "Error connecting to {}:{}".format(src_addr, src_port)
        return None
    print "Successfully connected to host ", server
    return sock


# timestamp (long) | depth_head_count(int) | head_height (int) | head_width (int) |
# head_pos_x (float) | head_pos_y (float) | ... |
# left_hand_depth_data ([left_hand_width * left_hand_height]) |
# right_hand_depth_data ([right_hand_width * right_hand_height])
def decode_frame(raw_frame):
    # Expect network byte order
    endianness = "!"

    # In each frame, a header is transmitted
    header_format = "qiii"
    header_size = struct.calcsize(endianness + header_format)
    header = struct.unpack(endianness + header_format, raw_frame[:header_size])

    timestamp, depth_head_count, head_height, head_width = header

    head_pos_format = "fffH"
    head_pos_size = struct.calcsize(head_pos_format)
    head_pos = struct.unpack(endianness + head_pos_format,
                                  raw_frame[header_size: header_size + head_pos_size])

    head_depth_data_format = str(head_width * head_height) + "H"

    head_depth_data = ()

    if head_width * head_height > 0:
        head_depth_data = struct.unpack_from(endianness + head_depth_data_format, raw_frame, header_size + head_pos_size)
    #print timestamp, depth_hands_count, depth_hands, left_hand_width, left_hand_height, right_hand_width, right_hand_height

    return timestamp, depth_head_count, head_pos, head_width, head_height, head_depth_data


def recv_all(sock, size):
    result = b''
    while len(result) < size:
        data = sock.recv(size - len(result))
        if not data:
            raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
        result += data
    return result


def recv_depth_frame(sock):
    (frame_size,) = struct.unpack("!i", recv_all(sock, 4))
    return recv_all(sock, frame_size)


if __name__ == '__main__':
    kinect_socket = connect()
    fusion_socket = connect("Fusion")

    gesture_list = ["nod", "shake","other"]
    num_gestures = len(gesture_list)

    head_classifier = RealTimeHeadRecognition(num_gestures)

    gesture_list += ['blind']

    index = 0
    start_time = time.time()
    window = deque(maxlen=30)
    euclidean_skeleton = deque(maxlen=29)
    prev_skeleton = None

    while True:
        try:
            frame = recv_depth_frame(kinect_socket)
            decoded_frame = decode_frame(frame)
        except KeyboardInterrupt:
           break
        except:
            continue

        timestamp, depth_head_count, head_pos, head_width, head_height, head_depth_data = decoded_frame
        curr_skeleton = np.array(head_pos[:-1])

        if depth_head_count>0 and head_depth_data:
            head = np.array(head_depth_data, dtype=np.float32).reshape((head_height, head_width))
            head = cv2.resize(head, (168, 168))
            head = head[20:-20, 20:-20]
            head -= head_pos[-1]
            head /= 150

            head += 1
            head *= 127.5

            window.append(head)
            if prev_skeleton is not None:
                euclidean_skeleton.append(np.linalg.norm(curr_skeleton-prev_skeleton))
            prev_skeleton = curr_skeleton

            if len(window)==30:
                new_window = [window[0]]
                for i in range(1,30):
                    new_window.append(window[i]-window[i-1])

                new_window = [n/255.0 for n in new_window]

                new_window = np.rollaxis(np.stack(new_window), 0, 3)[np.newaxis,:,:,:]

                gesture_index, probs = head_classifier.classify(new_window)
                head_movement = np.sum(euclidean_skeleton)
                probs = list(probs)+[0]
                print timestamp, gesture_list[gesture_index], probs, head_movement,

                if head_movement>0.03:
                    gesture_index = 2
                    probs = [0,0,1,0]
                    print gesture_list[gesture_index], probs,
                print

                pack_list = [FUSION_HEAD_ID, timestamp, gesture_index] + list(probs)

                bytes = struct.pack("!iqi" + "f" * (num_gestures+1), *pack_list)

                if fusion_socket is not None:
                    fusion_socket.send(bytes)

            index += 1

        else:
            pack_list = [FUSION_HEAD_ID, timestamp, num_gestures] + [0]*num_gestures+[1]
            print pack_list
            bytes = struct.pack("!iqi" + "f" * (num_gestures+1), *pack_list)

            if fusion_socket is not None:
                fusion_socket.send(bytes)


    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()