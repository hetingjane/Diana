import time
import numpy as np
from realtime_hand_recognition import RealTimeHandRecognition, RealTimeHandRecognitionOneShot
from kinect import Kinect
from socket_api import SocketAPI
from postures import hand_postures

# kinect frame processing settings
SEGMENT_SIZE = 224  # the resulting size of the segmented depth frame (square)
ENGAGE_MIN = 0
ENGAGE_MAX = 10

# socket interface settings
TCP_IP = '127.0.0.1'  # IP address to connect to
TCP_PORT = 38276  # port number to connect to
BUFFER_SIZE = 1024  # size of receive buffer (1k should be plenty)
TERMINATOR = "\r\n"  # message terminator (required)
ACK_TIMEOUT = 2  # how long to wait for a response from the server

class DepthClient:
    def __init__(self):
        self.kinect = Kinect(ENGAGE_MIN, ENGAGE_MAX, modality='depth')
        socket_started = False
        self.socket_api = None
        while (not socket_started):
            try:
                self.socket_api = SocketAPI(TCP_IP, TCP_PORT, BUFFER_SIZE, ACK_TIMEOUT, TERMINATOR)
                socket_started = True
            except:
                print("Socket didn't start, waiting 3 seconds...")
                time.sleep(3)
        print("Connected!")
        self.HandModel = RealTimeHandRecognition("RH", 32, 2, len(hand_postures) - 1)
        
    def get_labels(self, idx_L, idx_R):
        label_L = hand_postures[idx_L]
        if label_L == '':
            label_L = 'other'
        else:
            label_L = label_L.replace("DIRECTION", "right")
        
        label_R = hand_postures[idx_R]
        if label_R == '':
            label_R = 'other'
        else:
            label_R = label_R.replace("DIRECTION", "left")
        
        return label_L, label_R

    def run(self):
        while True:
            frames = self.kinect.get()
            if frames is None:
                print("waiting for frames...")
                time.sleep(1/60)
            else:
                LH_pred, RH_pred = self.HandModel.get_predsmax(frames[0], frames[1])
                LH_label, RH_label = self.get_labels(LH_pred, RH_pred)
                self.socket_api.set("user:hands:left", LH_label)
                self.socket_api.set("user:hands:right", RH_label)
                print(LH_label, RH_label)

if __name__ == '__main__':
    print("starting")
    client = None
    try:
        client = DepthClient()
        client.run()
    finally:
        client.socket_api.close()
