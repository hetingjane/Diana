#!/usr/bin/env python

import socket, sys, struct
import time
import numpy as np
import matplotlib.pyplot as plt
src_addr = '129.82.45.102'
src_port = 8125

def connect():
    """
    Connect to a specific port
    """

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    try:
        sock.connect((src_addr, src_port))
        # Socket will only be used to read, so make it unidirectional
        sock.shutdown(socket.SHUT_WR)
    except:
        print "Error connecting to {}:{}".format(src_addr, src_port)
        return None
    print "Successfully connected to {}:{}".format(src_addr, src_port)
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
    left_hand_pos = struct.unpack(endianness + left_hand_pos_format, raw_frame[header_size: header_size + left_hand_pos_size])

    right_hand_pos_format = "ffH"
    right_hand_pos_size = struct.calcsize(right_hand_pos_format)
    right_hand_pos = struct.unpack(endianness + left_hand_pos_format, raw_frame[header_size + left_hand_pos_size: header_size + left_hand_pos_size + right_hand_pos_size])

    depth_hands_size = left_hand_pos_size + right_hand_pos_size
    depth_hands = left_hand_pos + right_hand_pos

    left_hand_depth_data_format = str(left_hand_width * left_hand_height) + "H"
    right_hand_depth_data_format = str(right_hand_width * right_hand_height) + "H"

    left_hand_depth_data = ()
    right_hand_depth_data = ()

    if left_hand_width * left_hand_height > 0 and right_hand_height * right_hand_width > 0:
        depth_data = struct.unpack_from(endianness + left_hand_depth_data_format + right_hand_depth_data_format, raw_frame, header_size + depth_hands_size)
        left_hand_depth_data = depth_data[:left_hand_width * left_hand_height]
        right_hand_depth_data = depth_data[left_hand_width * left_hand_height:]

    return (timestamp, depth_hands_count) + depth_hands + (left_hand_width, left_hand_height, right_hand_width, right_hand_height , left_hand_depth_data, right_hand_depth_data)

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
    (frame_size,) = struct.unpack("!i", recv_all(sock, 4))

    return recv_all(sock, frame_size) 
    

# By default read 100 frames
if __name__ == '__main__':

    # Time the network performance 
    s = connect()
    
    i = 0

    avg_frame_time = 0.0

    while i<1000:
        t_begin = time.time()
        f = recv_depth_frame(s)
        t_end = time.time()
        print "Time take for this frame: {}".format(t_end - t_begin)
        avg_frame_time += (t_end - t_begin)
        df = decode_frame(f)
    
        do_plot = False
        
        if do_plot and i % 20 == 0:
            offset = 2 + (df[1] * 2) + 2
            lwidth = df[offset]
            lheight = df[offset + 1]
            rwidth = df[offset + 2]
            rheight = df[offset + 3]

            if df[offset + 4]:
                left_image = np.array(df[offset + 4]).reshape((lheight, lwidth))
                im = plt.imshow(left_image, cmap='gray')
                plt.show()

            if df[offset + 5]:
                right_image = np.array(df[offset + 5]).reshape((rheight, rwidth))
                im = plt.imshow(right_image, cmap='gray')
                plt.show()

        print "\n\n"
            
        i += 1

    print "Total frame time: {}".format(avg_frame_time)

    avg_frame_time /= i
    
    print "Average frame time over {} frames: {}".format(i, avg_frame_time)

    s.close()
    sys.exit(0)
