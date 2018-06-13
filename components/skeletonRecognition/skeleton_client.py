from __future__ import print_function

import sys
import time
import struct
import argparse

from ..fusion.conf import streams
from ..fusion.conf.endpoints import connect
from .Armsolver import PrimalRecognition, ArmMotionRecogntion
import components.timer


def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect little endian byte order
    endianness = "<"

    # [ commonTimestamp | frame type | Tracked body count | Engaged
    header_format = "qiBB"

    timestamp, frame_type, tracked_body_count, engaged = struct.unpack(endianness + header_format,raw_frame[:struct.calcsize(header_format)])

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
    kinect_host, fusion_host, pointing_mode = args.kinect, args.fusion_host, args.pointing_mode


    s = connect('kinect', kinect_host, 'Body')

    if fusion_host is not None:
        fusion_socket = connect('fusion', fusion_host, 'Body')
    else:
        fusion_socket = None


    if s is None:
        sys.exit(0)

    m = PrimalRecognition(pointing_mode='screen')
    c = 0
    start_time = components.timer.safetime()

    while True:
        try:
            f = recv_skeleton_frame(s)

        except EOFError:
            print("Disconnected from Kinect Server")
            break
        fd = decode_frame(f)
        

        #Pass this to the Recognition object
        m.feed_input(fd)
        result = m.get_result()
        timestamp = m.timestamp
        

        display_result = m.printable_result()
        if display_result is not None:
            print (result[:3], display_result)

        pack_list = [streams.get_stream_id("Body"), timestamp] + result
        raw_data = struct.pack("<iqiii" + "ffff" * 2 + "f" * 5 + "ff" * 6 + 'i', *pack_list)

        if fusion_socket is not None:
            fusion_socket.sendall(raw_data)

        c += 1
        if c % 100 == 0:
            print ('=' * 30)
            print ('FPS: ', 100.0 / (components.timer.safetime() - start_time))
            print ('=' * 30)
            start_time = components.timer.safetime()

    s.close()
    if fusion_socket is not None:
        fusion_socket.close()

    sys.exit(0)



