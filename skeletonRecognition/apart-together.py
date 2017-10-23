import struct
import sys
from collections import deque

import numpy as np

from SlidingWindow import sliding_window_dataset
from WindowProcess import (extract_data, process_window_data, collect_all_results, send_default_values)
from support.endpoints import connect
from support import streams

def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect little endian byte order
    endianness = "<"

    # [ commonTimestamp | frame type | Tracked body count | Engaged
    header_format = "qiBB"

    timestamp, frame_type, tracked_body_count, engaged = struct.unpack(endianness + header_format, raw_frame[:struct.calcsize(header_format)])

    # For each body, a header is transmitted
    # TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    body_format = "Q4B"

    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BB7f"

    frame_format = body_format + (joint_format * 25)
    
    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack(endianness + (frame_format * engaged), raw_frame[struct.calcsize(header_format):])
    
    decoded = (timestamp, frame_type, tracked_body_count, engaged) + frame_pieces

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
    # print load_size
    return recv_all(sock, load_size)


# By default read 100 frames
if __name__ == '__main__':

    # Time the network performance
    s = connect('kinect', 'Body')
    r = connect('fusion', 'Body')

    if s is None:
        sys.exit(0)

    window_threshold = 15
    body_parts = ['LA', 'RA']
    data_stream = deque([], maxlen=window_threshold)
    count = 0

    avg_frame_time = 0.0

    arms_x_bits = ["still", "arms apart", "arms together"]
    arms_y_bits = ["still", "stack up", "stack down"]
    
    
    while True:
        try:
            f = recv_skeleton_frame(s)
        except EOFError:
            print "Disconnected from Kinect Server"
            break
        fd = decode_frame(f)
        timestamp, frame_type, body_count, engaged = fd[:4]
        print 'timestamp received: ', timestamp

        input_data = (timestamp, body_count) + fd[4:]
        

        if engaged: 
            data_stream.extend([input_data])
  
            if len(data_stream) >= window_threshold:
                t = np.vstack([extract_data(frame) for frame in data_stream])
                test_window = sliding_window_dataset([t], window_threshold)

                proba_array, map_array = [], []
                for b in body_parts:
                    res = process_window_data(test_window, body_part=b)
                    proba_array.append(res[0].tolist()), map_array.append(res[1])

                # Processing arms apart and arms together -X direction
                if (map_array[0] == 0 and map_array[1] == 1) or (map_array[0] == 1 and map_array[1] == 0):
                    _, bit_val = process_window_data(test_window, body_part='arms_x')

                    if bit_val == 0:
                        bit_val = 26
                    elif bit_val == 1:
                        bit_val = 27
                    elif bit_val == 2:
                        bit_val = 28
                    map_array[0] = map_array[1] = bit_val

                # Processing arms apart and arms together -Y direction
                if (map_array[0] == 2 and map_array[1] == 3) or (map_array[0] == 3 and map_array[1] == 2):
                    _, bit_val = process_window_data(test_window, body_part='arms_y')
                    if bit_val == 0:
                        bit_val = 26
                    elif bit_val == 1:
                        bit_val = 29
                    elif bit_val == 2:
                        bit_val = 30
                    map_array[0] = map_array[1] = bit_val

            else:
                map_array, proba_array = send_default_values(body_parts)

            result = collect_all_results(map_array, proba_array, int(engaged))
            timestamp = list(data_stream)[-1][0]

        else:
            map_array, proba_array = send_default_values(body_parts)
            result = collect_all_results(map_array, proba_array, int(engaged))
            data_stream.clear()

        pack_list = [streams.get_stream_id("Body"), timestamp] + result
        print timestamp, body_count, engaged, result[:2]
        raw_data = struct.pack("<iqii" + "ff" * 6 + 'i', *pack_list)

        if r is not None:
            r.sendall(raw_data)

    print "Total frame time: {}".format(avg_frame_time)
   
    s.close()
    if r is not None:
        r.close()

    sys.exit(0)
