import gzip
import socket
import struct
import time
from collections import deque

import numpy as np

from skeletonRecognition.Compute import (check_engage_disengage)
from skeletonRecognition.SlidingWindow import sliding_window_dataset
from skeletonRecognition.WindowProcess import (extract_data, process_window_data, collect_all_results, send_default_values)

src_addr = '129.82.45.102'
src_port = 8123

des_addr = '10.1.118.19'  # 'cwc1'
des_port = 9125


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
    print "Successfully connected to host"
    return sock


def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect network byte order
    endianness = "!"

    # For each body, a header is transmitted
    # [ commonTimestamp | TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    header_format = "qQBBBB"

    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BBfffffff"

    frame_format = header_format + (joint_format * 25)

    body_frame_length = struct.calcsize(endianness + frame_format)

    # Confirm that the length of the frame is a valid one, i.e.
    # length of raw_frame is a multiple of length of single body frame
    # indicating multiple bodies in a single frame of skeleton data
    assert len(
        raw_frame) % body_frame_length == 0, "Frame length {} is not a multiple of single body frame length {}".format(
        len(raw_frame), body_frame_length)

    body_count = len(raw_frame) // body_frame_length

    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack(endianness + (frame_format * body_count), raw_frame)

    # Need to give the user a way to iterate over body frames if needed
    return frame_pieces


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


def recv_skeleton_frame(sock):
    """
    Experimental function to read each stream frame from the server
    """
    (frame_size,) = struct.unpack("!i", recv_all(sock, 4))

    return recv_all(sock, frame_size)


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

    window_threshold = 15
    body_parts = ['LA', 'RA']
    data_stream = deque([], maxlen=window_threshold)
    count = 0

    avg_frame_time = 0.0


    while True:
        t_begin = time.time()
        f = recv_skeleton_frame(s)
        t_end = time.time()

        avg_frame_time += (t_end - t_begin)

        input_data = decode_frame(f)

        if (len(input_data) == 0):
            continue
        else:
            dta = extract_data(input_data)
            data_stream.extend([input_data])  # append(input_data)
            if check_engage_disengage(dta):
                if (len(data_stream) >= window_threshold):
                    t = np.vstack([extract_data(frame) for frame in data_stream])#data_stream[i]) for i in range(count, (count + window_threshold))])
                    test_window = sliding_window_dataset([t], window_threshold)#sliding_window_dataset(normalize_spine_base_dataset([t]), window_threshold)

                    proba_array, map_array = [], []
                    for b in body_parts:
                        res = process_window_data(test_window, body_part=b)
                        proba_array.append(res[0].tolist()), map_array.append(res[1])
                    result = collect_all_results(map_array, proba_array, 1)
                else:
                    map_array, proba_array = send_default_values(body_parts)
                    result = collect_all_results(map_array, proba_array, 1)

            else:
                map_array, proba_array = send_default_values(body_parts)
                result = collect_all_results(map_array, proba_array, 0)

        time_stamp = list(data_stream)[-1][0]
        print 'timestamp:', time_stamp
        pack_list = [1, time_stamp] + result
        bytes = struct.pack("!iqii" + "ff" * 6 + 'i', *pack_list)
        r.sendall(bytes)



    print "Total frame time: {}".format(avg_frame_time)

    s.close()
    sys.exit(0)


'''
time_stamp = data_stream[(count+window_threshold-1)][0]
print 'timestamp:', time_stamp
pack_list = [11, time_stamp, result]
bytes = struct.pack("!iqii" + "ff" * 6 + 'i', *pack_list)
r.sendall(bytes)
'''