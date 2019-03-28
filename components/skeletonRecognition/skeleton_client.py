from __future__ import print_function

import sys
import time
import struct
import argparse

from ..fusion.conf import streams
from ..fusion.conf.endpoints import connect
from .Armsolver import PrimalRecognition, ArmMotionRecogntion
from ..fusion.conf import decode

def decode_content(raw_frame, offset):
    """
    raw_frame: frame starting from 4 to end (4 for length field)
    offset: index where header ends; header is header_l, timestamp, frame_type
    """
    endianness = "<"

    content_header_format = "BB"  # Tracked body count | Engaged
    content_header_size = struct.calcsize(endianness + content_header_format)
    content_header = struct.unpack_from(endianness + content_header_format, raw_frame, offset)

    tracked_body_count, engaged = content_header

    # For each body, a header is transmitted
    # TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    body_format = "Q4B"

    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BB7f"

    frame_format = body_format + (joint_format * 25)

    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack_from(endianness + (frame_format * engaged), raw_frame, offset + content_header_size)
    
    #decoded = (tracked_body_count, engaged) + frame_pieces
    decoded = (tracked_body_count, engaged, frame_pieces)
    offset = offset + content_header_size + struct.calcsize(endianness + frame_format * engaged)  # new offset from where tail starts
    return decoded, offset
    
    
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--kinect-host', help='Kinect host name', type=str, default='127.0.0.1')
    parser.add_argument('--fusion-host', help='Fusion host name', type=str, default='127.0.0.1')
    parser.add_argument('--pointing-mode', default='screen', help='Pointing mode, default set to screen', type=str)
    parser.add_argument('--model', help='Choose between backend models for motion recognition, "primal" or "LSTM"', default="LSTM")

    args = parser.parse_args()
    kinect_host, fusion_host, pointing_mode = args.kinect_host, args.fusion_host, args.pointing_mode


    s = connect('kinect', kinect_host, 'Body')

    if fusion_host is not None:
        fusion_socket = connect('fusion', fusion_host, 'Body')
    else:
        fusion_socket = None


    if s is None:
        sys.exit(0)

    if args.model == "LSTM":
        m = ArmMotionRecogntion(pointing_mode='screen')
    else:
        m = PrimalRecognition(pointing_mode='screen')
    c = 0
    start_time = time.time()

    while True:
        try:
            (timestamp, frame_type), (tracked_body_count, engaged, frame_pieces), (writer_data,) = decode.read_frame(s, decode_content)
        except EOFError:
            print("Disconnected from Kinect Server")
            break
        
        #Pass this to the Recognition object
        fd = (timestamp, frame_type, tracked_body_count, engaged) + frame_pieces
        m.feed_input(fd)
        result = m.get_result()
        timestamp = m.timestamp
        

        display_result = m.printable_result()
        if display_result is not None:
            print('LPOINT', '{:> 7.3}'.format(m.point.lpoint[0]), '{:> 7.3}'.format(m.point.lpoint[1]),
                  'RPOINT', '{:> 7.3}'.format(m.point.rpoint[0]), '{:> 7.3}'.format(m.point.rpoint[1]),
                  '{:24}'.format(display_result[0]), '{:24}'.format(display_result[1]))

        pack_list = [streams.get_stream_id("Body"), timestamp] + result
        raw_data = struct.pack("<iqii" + "ffff" * 2 + "ff" * 8 + 'i', *pack_list)

        if fusion_socket is not None:
            fusion_socket.sendall(raw_data)

        c += 1
        if c % 100 == 0:
            print()
            print ('=' * 30)
            print ('FPS: ', 100.0 / (time.time() - start_time))
            print ('=' * 30)
            start_time = time.time()

    s.close()
    if fusion_socket is not None:
        fusion_socket.close()

    sys.exit(0)



