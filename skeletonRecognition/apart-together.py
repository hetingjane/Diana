import gzip
import socket
import struct
import time
from collections import deque

import numpy as np

from Compute import (check_engage_disengage)
from SlidingWindow import sliding_window_dataset
from WindowProcess import (extract_data, process_window_data, collect_all_results, send_default_values)
from support.constants import *

src_addr = KINECT_SRC_ADDR
src_port = KINECT_SKELETON_PORT

des_addr = FUSION_SRC_ADDR
des_port = FUSION_INPUT_PORT

stream_id = 32

def connect():
    """
    Connect to a specific port
    """

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    try:
        sock.connect((src_addr, src_port))
    except:
        print "Error connecting to {}:{}".format(src_addr, src_port)
        return None
    try:
		print "Sending stream info"
		sock.sendall(struct.pack('<i', stream_id))
    except:
        print "Error: Stream rejected"
        return None
    print "Successfully connected to host"
    return sock


def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect little endian byte order
    endianness = "<"

    # [ commonTimestamp | frame type | Tracked body count
    header_format = "qiB"

    timestamp, frame_type, tracked_body_count, enagaged = struct.unpack(endianness + header_format, raw_frame[:struct.calcsize(header_format)])

    # For each body, a header is transmitted
    # TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    body_format = "Q4B"

    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BB7f"

    frame_format = body_format + (joint_format * 25)

    tracked = tracked_body_count > 0
    
    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack(endianness + (frame_format * (1 if tracked else 0)), raw_frame[struct.calcsize(header_format):])
    
    decoded = (timestamp, frame_type, tracked_body_count, enagaged) + frame_pieces

    return decoded


def recv_all(sock, size):
    result = b''
    while len(result) < size:
        data = sock.recv(size - len(result))
        if not data:
            raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
        result += data
    return result


def recv_skeleton_frame(sock):
    """
    To read each stream frame from the server
    """
    (load_size,) = struct.unpack("<i", recv_all(sock, struct.calcsize("<i")))
    #print load_size
    return recv_all(sock, load_size)


def connect_fusion():
    """
    Connect to a specific port
    """
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    try:
        sock.connect((des_addr, des_port))
        # Socket will only be used to read, so make it unidirectional
    except:
        print "Error connecting to {}:{}".format(des_addr, des_port)
        return None
    print "Successfully connected to host"
    return sock


# By default read 100 frames
if __name__ == '__main__':

    # Time the network performance
    s = connect()
    r = connect_fusion()

    if s==None:
	sys.exit(0)

    window_threshold = 15
    body_parts = ['LA', 'RA']
    data_stream = deque([], maxlen=window_threshold)
    count = 0

    avg_frame_time = 0.0

    arms_x_bits = ["still", "arms apart", "arms together"]
    arms_y_bits = ["still", "stack up", "stack down"]

    while True:
        f = recv_skeleton_frame(s)
        fd = decode_frame(f)
        timestamp, frame_type, body_count, enagaged = fd[:4]
        input_data = (timestamp, body_count) + fd[4:]
  
        data_stream.extend([input_data])
        if engaged:
            if (len(data_stream) >= window_threshold):
                t = np.vstack([extract_data(frame) for frame in data_stream])
                test_window = sliding_window_dataset([t], window_threshold)

                proba_array, map_array = [], []
                for b in body_parts:
                    res = process_window_data(test_window, body_part=b)
                    proba_array.append(res[0].tolist()), map_array.append(res[1])

                #Processing arms apart and arms together -X direction
                if (map_array[0] == 0 and map_array[1] == 1) or (map_array[0] == 1 and map_array[1] == 0):
                    _, bit_val = process_window_data(test_window, body_part='arms_x')

                    if (bit_val == 0):
                        bit_val = 26
                    elif (bit_val == 1):
                        bit_val = 27
                    elif (bit_val == 2):
                        bit_val = 28
                    map_array[0] = map_array[1] = bit_val

                #Processing arms apart and arms together -Y direction
                if (map_array[0] == 2 and map_array[1] == 3) or (map_array[0] == 3 and map_array[1] == 2):
                    _, bit_val = process_window_data(test_window, body_part='arms_y')
                    if (bit_val == 0):
                        bit_val = 26
                    elif (bit_val == 1):
                        bit_val = 29
                    elif (bit_val == 2):
                        bit_val = 30
                    map_array[0] = map_array[1] = bit_val

            else:
                map_array, proba_array = send_default_values(body_parts)
            result = collect_all_results(map_array, proba_array, int(engaged))


        else:
            map_array, proba_array = send_default_values(body_parts)
            result = collect_all_results(map_array, proba_array, int(engaged))

        time_stamp = list(data_stream)[-1][0]
        print time_stamp, (result[:2]), body_count
        pack_list = [1, time_stamp] + result
        bytes = struct.pack("!iqii" + "ff" * 6 + 'i', *pack_list)

        if r != None:
            r.sendall(bytes)    

    print "Total frame time: {}".format(avg_frame_time)

   
    s.close()
    if r != None:
        r.close()
    sys.exit(0)

