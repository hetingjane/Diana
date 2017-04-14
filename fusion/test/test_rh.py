#!/usr/bin/env python
import socket
import depth_client
import random
import struct
import time
import sys
from conf.postures import right_hand_postures
import conf.streams as streams

def process_frame(decoded_frame):
    time.sleep(0.03)
    return [ streams.get_stream_id("RH"), fd[0], random.randint(0, len(right_hand_postures) - 1) ] + [ random.random() for x in range(len(right_hand_postures)) ]

if __name__ == '__main__':
    sock = depth_client.connect()
    sock_dest = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        sock_dest.connect(('localhost', 9125))
    except:
        print "Can't connect to fusion server"
        sys.exit(0)
    while True:
        f = depth_client.recv_depth_frame(sock)
        fd = depth_client.decode_frame(f)
        result = process_frame(fd)
        print result[0]
        # ID, Timestamp, Max Index, 27 label probabilities
        sock_dest.sendall(struct.pack("!iqi" + "f"*len(right_hand_postures), *result))
        
