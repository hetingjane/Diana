import socket
import struct
import time
import gzip
import cv2
import sys
import numpy as np
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

    head_pos_format = "ffH"
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
    print "FS", frame_size

    return recv_all(sock, frame_size)



if __name__ == '__main__':
    kinect_socket = connect()
    #fusion_socket = connect("Fusion")

    i = 0

    start_time = time.time()
    while True:
        try:
            frame = recv_depth_frame(kinect_socket)
            decoded_frame = decode_frame(frame)
        except KeyboardInterrupt:
           break
        except:
            continue

        timestamp, depth_head_count, head_pos, head_width, head_height, head_depth_data = decoded_frame

        print timestamp

        if depth_head_count>0:
            head = np.array(head_depth_data, dtype=np.float32).reshape((head_height, head_width))
            head = cv2.resize(head, (168, 168))
            head = head[20:-20, 20:-20]
            head -= head_pos[2]
            head /= 150

            plt.imshow(head)
            plt.show()
