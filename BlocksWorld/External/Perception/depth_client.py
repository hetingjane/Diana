import time
import numpy as np
from realtime_hand_recognition import RealTimeHandRecognition, RealTimeHandRecognitionOneShot
from kinect import Kinect
from socket_api import SocketAPI
from postures import left_hand_postures, right_hand_postures

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
        self.HandModel = RealTimeHandRecognition("RH", 32, 2)

    def run(self):
        while True:
            frames = self.kinect.get()
            if frames is None:
                print("waiting for frames...")
                continue
            time.sleep(1/60)
            LH_out, RH_out = self.HandModel.classifyLR(frames[0], frames[1])
            LH_idx = np.argmax(LH_out)
            LH_label = left_hand_postures[LH_idx][3:]
            RH_idx = np.argmax(RH_out)
            RH_label = right_hand_postures[RH_idx][3:]
            self.socket_api.set("user:hands:left", LH_label)
            self.socket_api.set("user:hands:right", RH_label)
            print(LH_label, RH_label)
            
            #import cv2
            #fram = np.squeeze(np.hstack((frames[0], frames[1])))
            #fram = np.vstack((fram, np.fliplr(fram)))
            #cv2.imshow("frams", fram)
            #cv2.waitKey(6)

if __name__ == '__main__':
    print("starting")
    client = None
    try:
        client = DepthClient()
        client.run()
    finally:
        client.socket_api.close()
