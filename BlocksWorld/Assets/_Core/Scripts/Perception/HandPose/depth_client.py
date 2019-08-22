import time
import numpy as np
import cv2
from realtime_hand_recognition import RealTimeHandRecognition, RealTimeHandRecognitionOneShot
from kinect import Kinect
from socket_api import SocketAPI

# kinect frame processing settings
SEGMENT_SIZE = 224  # the resulting size of the segmented depth frame (square)
ENGAGE_MIN = 0
ENGAGE_MAX = 10

# socket interface settings
TCP_IP = '10.83.176.22'  # IP address to connect to
TCP_PORT = 38276  # port number to connect to
BUFFER_SIZE = 1024  # size of receive buffer (1k should be plenty)
TERMINATOR = "\r\n"  # message terminator (required)
ACK_TIMEOUT = 2  # how long to wait for a response from the server

class DepthClient:
    def __init__(self):
        self.kinect = Kinect(SEGMENT_SIZE, ENGAGE_MIN, ENGAGE_MAX)
        #self.socket_api = SocketAPI(TCP_IP, TCP_PORT, BUFFER_SIZE, ACK_TIMEOUT, TERMINATOR)
        self.HandModel = RealTimeHandRecognition("RH", 32, 2)

    def run(self):
        cv2.namedWindow("left")
        cv2.moveWindow("left", 20, 20)
        cv2.namedWindow("right")
        cv2.moveWindow("right", 200, 20)
        while True:
            frames = self.kinect.get()
            if frames is None:
                print("waiting for frames...")
                time.sleep(1/30)
                continue
            cv2.imshow("left", frames[0].reshape((128,128,1))/2+.5)
            cv2.imshow("right", frames[1].reshape((128, 128, 1))/2+.5)
            (LH_probs, LH_out), (RH_probs, RH_out) = self.HandModel.classifyLR(frames[0], frames[1])
            #self.socket_api.send_to_server(self, "pose:leftHand", LH_out)
            #self.socket_api.send_to_server(self, "pose:rightHand", RH_out)
            print(np.argmax(LH_out), np.argmax(RH_out))

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

if __name__ == '__main__':
    client = DepthClient()
    client.run()