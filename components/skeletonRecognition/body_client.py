import struct
import sys
import argparse
from collections import deque
from .BackEnd import *
from ..fusion.conf.endpoints import connect
from ..fusion.conf import streams
from .receiveAndShow import Pointing


def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect little endian byte order
    endianness = "<"

    # [ commonTimestamp | frame type | Tracked body count | Engaged
    header_format = "qiBB"

    timestamp, frame_type, tracked_body_count, engaged = struct.unpack(endianness + header_format,
                                                                       raw_frame[:struct.calcsize(header_format)])

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



if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('kinect', help='Kinect host name', type=str)
    parser.add_argument('--fusion-host', default=None, help='Fusion host name, default set to None', type=str)
    parser.add_argument('--pointing-mode', default='screen', help='Pointing mode, default set to screen', type=str)

    args = parser.parse_args()
    # validate_arguments(args)
    kinect_host, fusion_host, pointing_mode = args.kinect, args.fusion_host, args.pointing_mode


    rgb = False
    lstm = False
    dims = 2 if rgb else 3
    feature_size = 10 if rgb else 21
    logpath = '/s/red/a/nobackup/vision/dkpatil/demo/GRU_5_class/'
    class_list = np.load(logpath+'labels_list.npy')
    print(class_list)



    # Time the network performance
    if rgb:
        s = connect_rgb()
    else:
        s = connect('kinect', kinect_host, 'Body')

    if fusion_host is not None:
        r = connect('fusion', fusion_host, 'Body')
    else:
        r = None


    if s is None:
        sys.exit(0)

    window_threshold = 15
    body_parts = ['LA', 'RA']
    data_stream = deque([], maxlen=window_threshold)

    avg_frame_time = 0.0


    if lstm:
        from .GRU_classifier import (GRU_RNN, EGGNOGClassifierSlidingWindow)
        num_classes = 5
        model = GRU_RNN(logs_path=logpath, features=feature_size, n_classes=num_classes)
        solver = EGGNOGClassifierSlidingWindow(model=model, restore_model=True, rgb=rgb, num_classes=num_classes)


    import time
    start_time = time.time()
    count = 0

    wave_flag = False
    point = Pointing(pointing_mode=pointing_mode)


    while True:
        try:
            f = recv_skeleton_frame(s)

        except EOFError:
            print("Disconnected from Kinect Server")
            break
        fd = decode_frame(f)
        timestamp, frame_type, body_count, engaged = fd[:4]



        #Skeleton Box construction
        #If enagaged skeleton received, we further filter skeleton on x coordinates and update flag
        if engaged:
            #Assumption: rgb=False, dimensions available: 3
            input_data = (timestamp, body_count) + fd[4:]
            frame_data = np.array(extract_data(input_data, rgb)).reshape((1, -1))

            sb_x = frame_data[0][0]
            sb_z = frame_data[0][2]

            #Left and right axis threshold
            if -0.83<sb_x<0.53:engaged = True
            else:engaged = False


        if engaged:
            engaged_bit = 'Engaged'
        else:
            engaged_bit = 'Disengaged'

        if rgb:
            lpoint, rpoint = [0.0, 0.0], [0.0, 0.0]
            lvar, rvar = [0.0, 0.0], [0.0, 0.0]
        else:
            if wave_flag:
                point.get_pointing_main(fd)
                lpoint, rpoint = point.lpoint, point.rpoint
                lvar, rvar = point.lpoint_var, point.rpoint_var

            else:
                lpoint, rpoint = [0.0, 0.0], [0.0, 0.0]
                lvar, rvar = [0.0, 0.0], [0.0, 0.0]


        if engaged:
            data_stream.extend([input_data])
            if len(data_stream) >= window_threshold:
                wave_array = []
                proba_array, encoding_array, motion_label_array, active_arm_array = [], [], [], []

                # Function rewritten for RGB
                data = np.vstack([extract_data(frame, rgb) for frame in data_stream])
                # data = smooth_data(data, polyorder=2)


                if wave_flag and lstm:
                    #Processing the GRU Classification for the 15 frame window
                    pruned_data_for_solver = prune_joints(data, body_part='arms', rgb=rgb)
                    if rgb:
                        assert pruned_data_for_solver.shape == (window_threshold, 10)
                    else:
                        assert pruned_data_for_solver.shape == (window_threshold, 21)

                    class_label, probabilities = solver.predict(pruned_data_for_solver)
                    # Adding the probability values of 5 class first
                    proba_array.append(probabilities)
                else:
                    class_label, probabilities = 4, [0.0, 0.0, 0.0, 0.0, 1.0]
                    # Adding the probability values of 5 class first
                    proba_array.append(probabilities)


                for body_part in body_parts:
                    pruned_data = prune_joints(data, body_part=body_part, rgb=rgb)
                    active_arm = check_active_arm(pruned_data, rgb=rgb)  # Confirm shoulder-elbow or shoulder-wrist and return respectively
                    print(body_part, 'Active' if active_arm else 'Dangling')


                    if wave_flag:
                        active_arm = check_active_arm(pruned_data, rgb=rgb) #Confirm shoulder-elbow or shoulder-wrist and return respectively

                        if active_arm:
                            arm_motion_label, motion_encoding, probabilities = calculate_direction(pruned_data, body_part=body_part, rgb=rgb)
                        else:
                            arm_motion_label, motion_encoding, probabilities = 'blind', 32, [0]*6

                        wave_array.append(check_wave_motion(pruned_data, rgb=rgb))
                        encoding_array.append(motion_encoding), active_arm_array.append(active_arm), \
                        proba_array.append(probabilities), motion_label_array.append(arm_motion_label)

                    else:
                        wave_array.append(check_wave_motion(pruned_data, rgb=rgb))


                # Adding label index of 5 class result
                encoding_array.append(class_label)
                if np.any(wave_array):
                    wave_indx = [i for i, j in enumerate(wave_array) if j == True]
                    if not wave_flag:
                        wave_flag = True
                        encoding_array, active_arm_array, proba_array = send_default_values(body_parts)
                    for indx in wave_indx:
                        encoding_array[indx] = 31
                else:
                    if not wave_flag:
                        encoding_array, active_arm_array, proba_array = send_default_values(body_parts)

            else:
                # print 'buffer not full....sending default values'
                #Still (26) when engaged but buffer unfilled
                encoding_array, active_arm_array, proba_array = send_default_values(body_parts)


            result = collect_all_results(encoding_array, [lpoint,lvar, rpoint, rvar], proba_array, int(engaged))
            timestamp = list(data_stream)[-1][0]

        else:
            wave_flag = False
            # print 'Disengaged....clearing buffer'
            #Blind (31) when disengaged
            encoding_array, active_arm_array, proba_array = send_default_values(body_parts, value_to_add=32)
            result = collect_all_results(encoding_array, [lpoint,lvar, rpoint, rvar], proba_array, int(engaged))
            data_stream.clear()


        assert len(result) == 29
        # print 'Length of result is: ', len(result)
        pack_list = [streams.get_stream_id("Body"), timestamp] + result
        raw_data = struct.pack("<iqiii" + "ffff" * 2 + "f" * 5 + "ff" * 6 + 'i', *pack_list)



        #Debugging mode
        from ..fusion.conf.postures import left_arm_motions, right_arm_motions
        to_print_result = [left_arm_motions[result[0]], right_arm_motions[result[1]], class_list[result[2]]]
        if to_print_result==['blind', 'blind', 'still']:
            pass
        else:
            print('Result is: ', engaged_bit, to_print_result)


        if r is not None:
            r.sendall(raw_data)


        count += 1
        if count % 100 == 0:
            end_time = time.time()
            print('=' * 30)
            print('FPS: ', 100.0 / (end_time - start_time))
            print('=' * 30)
            start_time = end_time
            count = 0

    s.close()
    if r is not None:
        r.close()

    sys.exit(0)
