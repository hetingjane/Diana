#!/usr/bin/env python
import socket
import skeleton_client
import random
import struct
import time
import sys

def process_frame(decoded_frame):
    time.sleep(0.03)        
    return [ 0x1, decoded_frame[0], random.randint(0, 25), random.randint(0, 25) ] + [ random.random() for x in range(12) ] + [1]

if __name__ == '__main__':
    sock = skeleton_client.connect()
    sock_dest = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        sock_dest.connect(('127.0.0.1', 9125))
    except:
        print "Can't connect to fusion server"
        sys.exit(0)
    while True:
        f = skeleton_client.recv_skeleton_frame(sock)
        fd = skeleton_client.decode_frame(f)
        if len(fd)> 0:
            result = process_frame(fd)
            print result[0]
            sock_dest.sendall(struct.pack("!iqii" + "ff"*6 +"i", *result))
        
