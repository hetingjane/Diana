import struct
import sys
from collections import deque
import numpy as np
from SlidingWindow import sliding_window_dataset
from WindowProcess import (extract_data, process_window_data, collect_all_results, send_default_values, code_to_label_encoding)
from Preprocessing import (prune_joints_dataset, check_active_arm)
from ..fusion.conf.endpoints import connect
from ..fusion.conf import streams
from receiveAndShow import calculate_point


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

    if s is None:
        sys.exit(0)

    window_threshold = 15
    body_parts = ['LA', 'RA']
    data_stream = deque([], maxlen=window_threshold)

    avg_frame_time = 0.0


    from GRU_classifier import (GRU_RNN, EGGNOGClassifierSlidingWindow)
    logpath = '/s/red/a/nobackup/vision/dkpatil/demo/GRU_5_class/'  # '/s/chopin/k/grad/dkpatil/temp/SkeletonRealTime/GRU_classifier/'
    model = GRU_RNN(logpath)
    solver = EGGNOGClassifierSlidingWindow(model=model, restore_model=True)
    class_list = ['emblems', 'motions', 'neutral', 'oscillate', 'still']

    r = connect('fusion', 'Body')


    import time
    start_time = time.time()
    count = 0

    while True:
        try:
            f = recv_skeleton_frame(s)

        except EOFError:
            print "Disconnected from Kinect Server"
            break
        fd = decode_frame(f)
        timestamp, frame_type, body_count, engaged = fd[:4]

        if engaged:engaged_bit = 'Engaged'
        else: engaged_bit = 'Disengaged'
        print engaged_bit


        input_data = (timestamp, body_count) + fd[4:]
        lpoint, rpoint = calculate_point(fd)
        # print lpoint, rpoint

        if engaged: 
            data_stream.extend([input_data])
  
            if len(data_stream) >= window_threshold:
                t = np.vstack([extract_data(frame) for frame in data_stream])
                test_window = sliding_window_dataset([t], window_threshold)

                proba_array, map_array = [], []
                #Format of proba array is:  <<Emblem>, <Motion>, <Neutral>, <Oscillate>, <Still>> <<6 Probability values of LA> <6 of RA>>
                #Format of map_array is: <<LA index>, <RA index>, <Body index>>

                # Processing the GRU Classification for the 15 frame window
                res = prune_joints_dataset([t], body_part='arms')
                predicted = solver.predict(res)

                #Adding the probability values of 5 class first
                proba_array.append(predicted[1].tolist())


                for b in body_parts:
                    active = check_active_arm(t, body_part = b)
                    if active:
                        res = process_window_data(test_window, body_part=b)
                    else:
                        print 'Dangling arm, sending default values'
                        res = (np.zeros(6), 31)
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


                #Adding label index of 5 class result
                map_array.append(predicted[0])

            else:
                map_array, proba_array = send_default_values(body_parts)

            result = collect_all_results(map_array, [lpoint, rpoint], proba_array, int(engaged))
            timestamp = list(data_stream)[-1][0]

        else:
            map_array, proba_array = send_default_values(body_parts, value_to_add=31)
            result = collect_all_results(map_array, [lpoint, rpoint], proba_array, int(engaged))
            data_stream.clear()

        pack_list = [streams.get_stream_id("Body"), timestamp] + result
        # print lpoint, rpoint
        print timestamp, engaged_bit, code_to_label_encoding(result[0]), ',', code_to_label_encoding(result[1]), class_list[result[2]], lpoint, rpoint
        raw_data = struct.pack("<iqiii" + "ff" * 2 + "f" * 5 + "ff" * 6 + 'i', *pack_list)

        if r is not None:
            r.sendall(raw_data)


        count += 1

        if count%100 == 0:
            end_time = time.time()
            print '=' * 30
            print 'FPS: ', 100.0/(end_time - start_time)
            print '=' * 30
            start_time = end_time
            count = 0


    print "Total frame time: {}".format(avg_frame_time)
    s.close()
    if r is not None:
        r.close()

    sys.exit(0)
