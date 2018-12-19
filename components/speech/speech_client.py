import sys, struct
import argparse
import socket

from ..fusion.conf.endpoints import connect
from ..fusion.conf import decode

# Timestamp | frame type | command_length | command

def decode_content(raw_frame, offset):
    """
    raw_frame: frame starting from 4 to end (4 for length)
    offset: index where header ends  # header is header_l, timestamp, frame_type
    """
    endianness = "<"

    content_header_format = "i"  # command_length
    content_header_size = struct.calcsize(endianness + content_header_format)
    content_header, = struct.unpack_from(endianness + content_header_format, raw_frame, offset)

    command_length = content_header
    command_format = str(command_length) + "s"
    
    command = struct.unpack_from(endianness + command_format, raw_frame, offset + content_header_size)[0]
    command = command.decode('ascii')
    
    offset = offset + content_header_size + struct.calcsize(endianness + command_format)  # new offset from where tail starts
    return (command_length, command), offset
    
    
if __name__ == '__main__':

    parser = argparse.ArgumentParser()
    parser.add_argument('kinect_host', help='Host name of the machine running Kinect Server')
    parser.add_argument('--fusion-host', help='Host name of the machine running Kinect Server', default=None)

    args = parser.parse_args()

    k = connect('kinect', args.kinect_host, 'Speech')
    if k is None:
        sys.exit(0)

    f = connect('fusion', args.fusion_host, 'Speech') if args.fusion_host is not None else None

    while True:
        try:
            (timestamp, frame_type), (command_length, command), (writer_data,) = decode.read_frame(k, decode_content)
            print("writer_data", writer_data)
        except socket.error:
            print("Unable to receive speech frame")
            break
		
        if len(command) > 0:
            print(timestamp, frame_type, command)
            print("\n\n")

        if f is not None:
            try:
                # Excluding frame size
                f.sendall(struct.pack("<iqi" + str(len(command)) + "s", frame_type, timestamp, len(command), command.encode('ascii')))
            except socket.error:
                print("Error: Connection to fusion lost")
                f.close()
                f = None

    k.close()
    if f is not None:
        f.close()
    sys.exit(0)
