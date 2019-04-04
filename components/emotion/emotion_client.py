import sys, struct
import argparse
import socket

from components.fusion.conf.endpoints import connect
from components.fusion.conf import decode
from components.fusion.conf import streams
# Timestamp | frame type | command_length | command

def decode_content(raw_frame, offset):
    """
    raw_frame: frame starting from 4 to end (4 for length)
    offset: index where header ends  # header is header_l, timestamp, frame_type
    """
    endianness = "<"

    content_header_format = "BBBBddd"  # FaceFound, Engaged, LookingAway, WearingGlasses, Pitch, Yaw, Roll
    content_header_size = struct.calcsize(endianness + content_header_format)
    content_header = struct.unpack_from(endianness + content_header_format, raw_frame, offset)

    FaceFound, Engaged, LookinAway, WearingGlasses, Pitch, Yaw, Roll = content_header
    
   
    
    offset = offset + content_header_size    # new offset from where tail starts
    
    
    return (FaceFound, Engaged, LookinAway, WearingGlasses, Pitch, Yaw, Roll), offset
    
    
if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument('--kinect-host', help='Host name of the machine running Kinect Server',default='127.0.0.1')
    parser.add_argument('--fusion-host', help='Host name of the machine running Kinect Server', default='127.0.0.1')

    args = parser.parse_args()
    
    kinect_socket = connect('kinect', args.kinect_host, 'Face')
    if kinect_socket is None:
        sys.exit(0)
        
    emotion_frame_id = streams.get_stream_id("Emotion")
    fusion_socket = connect('fusion', args.fusion_host, 'Emotion') if args.fusion_host is not None else None

    while True:
        try:
            (timestamp, _), (FaceFound, Engaged, LookinAway, WearingGlasses, Pitch, Yaw, Roll), _ = decode.read_frame(kinect_socket, decode_content)
        except socket.error:
            print("Unable to receive speech frame")
            break

        print('found:', FaceFound, 'engaged:', Engaged, 'attentive:', LookinAway, 'glasses:', WearingGlasses,
              'pitch:', '{: 5.1f}'.format(Pitch), 'yaw:', '{: 5.1f}'.format(Yaw), 'roll:', '{: 5.1f}'.format(Roll))

        if FaceFound == 0:
            prob = 0.0
            att = 0
        else:
            prob = 1.0
            if -25.0 <= Yaw <= 25.0:
                att = 2
            elif Yaw > 25.0:
                att = 0
            else:
                att = 1

        if fusion_socket is not None:
            try:
                raw_data = struct.pack("<iqif" , emotion_frame_id, timestamp, att, prob)
                fusion_socket.sendall(struct.pack("<i", len(raw_data)))
                fusion_socket.sendall(raw_data)
            except socket.error:
                print("Error: Connection to fusion lost")
                fusion_socket.close()
                fusion_socket = None

    kinect_socket.close()
    if fusion_socket is not None:
        fusion_socket.close()
    sys.exit(0)
