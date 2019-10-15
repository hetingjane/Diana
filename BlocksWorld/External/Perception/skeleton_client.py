import time
from kinect import Kinect
from socket_api import SocketAPI
import armsolver

# kinect frame processing settings
ENGAGE_MIN = 0
ENGAGE_MAX = 10

# socket interface settings
TCP_IP = '127.0.0.1'  # IP address to connect to
TCP_PORT = 38276  # port number to connect to
BUFFER_SIZE = 1024  # size of receive buffer (1k should be plenty)
TERMINATOR = "\r\n"  # message terminator (required)
ACK_TIMEOUT = 2  # how long to wait for a response from the server

class SkeletonClient:
    def __init__(self):
        self.kinect = Kinect(ENGAGE_MIN, ENGAGE_MAX, modality='skeleton')
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
        self.model = armsolver.ArmMotionRecogntion()
        #self.model = armsolver.PrimalRecognition()

    def run(self):
        while True:
            frame = self.kinect.get()
            if frame is None:
                # print("waiting for frames...")    
                continue
            time.sleep(1/60)

            engaged = frame.engagement
            skeleton_frame = frame.skeleton_frame

            fd = [engaged] + skeleton_frame
            self.model.feed_input(fd)

            result = self.model.printable_result()
            LA_label, RA_label = result[0], result[1]

            if(LA_label == "servo"): 
                LA_label = "still"

            if(RA_label == "servo"):
                RA_label = "still"

            self.socket_api.set("user:arms:left", LA_label)
            self.socket_api.set("user:arms:right", RA_label)
            print(LA_label, RA_label)


if __name__ == '__main__':
    print("starting")
    client = None
    try:
        client = SkeletonClient()
        client.run()
    finally:
        client.socket_api.close()
