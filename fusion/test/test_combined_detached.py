#!/usr/bin/env python
import socket
import random
import struct
import sys
import time
from conf.postures import left_hand_postures, right_hand_postures
import conf.streams as streams

if __name__ == '__main__':

    timestamp = 0
    
    sock_lh = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock_rh = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock_s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        dest = 'localhost'
        #sock_lh.connect((dest, 9125))
        #sock_rh.connect((dest, 9125))
        sock_s.connect((dest, 9125))
    except:
        print "Can't connect to fusion server"
        sys.exit(0)
    while True:
        print timestamp

        prob_s = [random.random() for x in range(12)]
        sum_s = sum(prob_s)
        prob_s = [i / sum_s for i in prob_s]
        result_s = [streams.get_stream_id("Body"), timestamp, random.randint(0, 25), random.randint(0, 25)] + prob_s + [1]

        prob_lh = [random.random() for x in range(len(left_hand_postures))]
        sum_lh = sum(prob_lh)
        prob_lh = [i / sum_lh for i in prob_lh]
        result_lh = [streams.get_stream_id("LH"), timestamp, random.randint(0, len(left_hand_postures) - 1)] + prob_lh

        prob_rh = [random.random() for x in range(len(right_hand_postures))]
        sum_rh = sum(prob_rh)
        prob_rh = [i/sum_rh for i in prob_rh]
        result_rh = [streams.get_stream_id("RH"), timestamp, random.randint(0, len(right_hand_postures) - 1)] + prob_rh

        try:
            #sock_lh.sendall(struct.pack("!iqi" + "f" * len(left_hand_postures), *result_lh))
            #sock_rh.sendall(struct.pack("!iqi" + "f" * len(right_hand_postures), *result_rh))
            sock_s.sendall(struct.pack("!iqii" + "ff"*6 + "i", *result_s))
        except (socket.error, EOFError):
            print "Server disconnected"
            break

        time.sleep(0.03)
        timestamp += 1