#!/usr/bin/env python

import sys, struct
import time
import argparse

import numpy as np
from collections import deque
from skimage.transform import resize
from .realtime_head_recognition import RealTimeHeadRecognition
from ..fusion.conf.endpoints import connect
from ..fusion.conf import streams
from ..fusion.conf import decode

# Timestamp | frame type | width | height | depth_data

def decode_content(raw_frame, offset):
    """
    raw_frame: frame starting from 4 to end (4 for length field)
    offset: index where header ends; header is header_l, timestamp, frame_type
    """
    endianness = "<"

    content_header_format = "iiff"  # width, height, posx, posy
    content_header_size = struct.calcsize(endianness + content_header_format)
    content_header = struct.unpack_from(endianness + content_header_format, raw_frame, offset)

    width, height, posx, posy = content_header
    #print(width, height, posx, posy)

    depth_data_format = str(width * height) + "H"
    depth_data = struct.unpack_from(endianness + depth_data_format, raw_frame, offset + content_header_size)
    
    offset = offset + content_header_size + struct.calcsize(endianness + depth_data_format)  # new offset from where tail starts
    return (width, height, posx, posy, list(depth_data)), offset
    

if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument('kinect_host', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', help='Host name of the machine running Kinect Server', default=None)

    args = parser.parse_args()

    stream_id = streams.get_stream_id("Head")

    gesture_list = ["nod", "shake", "other"]
    num_gestures = len(gesture_list)

    head_classifier = RealTimeHeadRecognition(num_gestures)

    gesture_list += ['blind']

    kinect_socket = connect('kinect', args.kinect_host, 'Head')
    if kinect_socket is None:
        sys.exit(0)

    fusion_socket = connect('fusion', args.fusion_host, 'Head') if args.fusion_host is not None else None


    i = 0
    avg_frame_time = 0.0
    do_plot = True #if len(sys.argv) > 1 and sys.argv[1] == '--plot' else False


    index = 0
    start_time = time.time()
    window = deque(maxlen=30)
    euclidean_skeleton = deque(maxlen=29)
    prev_skeleton = None

    while True:
        try:
            t_begin = time.time()
            (timestamp, frame_type), (width, height, posx, posy, depth_data), (writer_data,) = decode.read_frame(kinect_socket, decode_content)
            t_end = time.time()
        except:
            break
        #print "Time taken for this frame: {}".format(t_end - t_begin)
        avg_frame_time += (t_end - t_begin)
        print(timestamp, frame_type, width, height, end=' ')

        curr_skeleton = np.array([posx, posy])

        if height>0 and width > 0:
            head = np.array(depth_data, dtype=np.float32).reshape((height, width))
            posz = head[int(posx), int(posy)]
            head -= posz
            head /= 150
            head = np.clip(head, -1, 1)
            head = resize(head, (168, 168))
            head = head[20:-20, 20:-20]
            head += 1
            head *= 127.5

            window.append(head)
            if prev_skeleton is not None:
                euclidean_skeleton.append(np.linalg.norm(curr_skeleton - prev_skeleton))
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
                print(gesture_list[gesture_index], probs[gesture_index], head_movement, end=' ')

                if head_movement>13: #0.03
                    gesture_index = 2
                    probs = [0,0,1,0]
                    print(gesture_list[gesture_index], end=' ')
                print("\n")

                pack_list = [stream_id, timestamp, gesture_index] + list(probs)

                raw_data = struct.pack("<iqi" + "f" * (num_gestures+1), *pack_list)

                if fusion_socket is not None:
                    fusion_socket.sendall(struct.pack("<i", len(raw_data)))
                    fusion_socket.sendall(raw_data)

            else:
                pack_list = [stream_id, timestamp, num_gestures] + [0] * num_gestures + [1]
                print('Buffer not full')
                raw_data = struct.pack("<iqi" + "f" * (num_gestures + 1), *pack_list)

                if fusion_socket is not None:
                    fusion_socket.sendall(struct.pack("<i", len(raw_data)))
                    fusion_socket.sendall(raw_data)


            index += 1

        else:
            pack_list = [stream_id, timestamp, num_gestures] + [0] * num_gestures + [1]
            print('blind')
            raw_data = struct.pack("<iqi" + "f" * (num_gestures + 1), *pack_list)

            if fusion_socket is not None:
                fusion_socket.sendall(struct.pack("<i", len(raw_data)))
                fusion_socket.sendall(raw_data)

        if index % 100==0:
            print("="*100, "FPS", 100/(time.time()-start_time))
            start_time = time.time()

    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)
