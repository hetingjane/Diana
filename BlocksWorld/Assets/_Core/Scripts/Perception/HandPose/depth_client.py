import struct
import threading
import time
import argparse
import numpy as np
from skimage.transform import resize
import sys

import postures
from realtime_hand_recognition import RealTimeHandRecognition, RealTimeHandRecognitionOneShot
from base_classifier import BaseClassifier
from one_shot_classifier import OneShotClassifier
from blacklist import get_blacklist

from kinect import Kinect
from pykinect2.PyKinectV2 import *

from socket_api import SocketAPI

# kinect frame processing settings
SEGMENT_SIZE = 224  # the resulting size of the segmented depth frame (square)

# socket interface settings
TCP_IP = '10.83.176.22'  # IP address to connect to
TCP_PORT = 38276  # port number to connect to
BUFFER_SIZE = 1024  # size of receive buffer (1k should be plenty)
TERMINATOR = "\r\n"  # message terminator (required)
ACK_TIMEOUT = 2  # how long to wait for a response from the server

class DepthClient:
    def __init__(self):
        self.kinect = Kinect(SEGMENT_SIZE)
        self.socket_api = SocketAPI(TCP_IP, TCP_IP, BUFFER_SIZE, ACK_TIMEOUT, TERMINATOR)
        self.HandModel = RealTimeHandRecognition("RH", 32, 2)

    def run(self):
        LH_frame, RH_frame = self.kinect.get()
        (LH_probs, LH_out), (RH_probs, RH_out) = self.HandModel.classifyLR(LH_frame, RH_frame)
        self.socket_api.send_to_server(self, "pose:leftHand", LH_out)
        self.socket_api.send_to_server(self, "pose:rightHand", RH_out)

if __name__ == '__main__':
    client = DepthClient()
    while True:
        client.run()