from PyQt5.QtWidgets import *
from GUI import App
import threading
import Queue
import sys
import socket
import struct

from support.constants import *

class ThreadedClient:

    def __init__(self):

        self.queue = Queue.Queue()
        self.count = 1
        self.sock = self.connect()

        self.running = 1
        self.thread1 = threading.Thread(target=self.workerThread1)
        self.thread1.start()

        app = QApplication(sys.argv)
        app.aboutToQuit.connect(self.endApplication)
        self.gui = App(self.queue, self.endApplication)
        sys.exit(app.exec_())

    def connect(self):
        src_addr = FUSION_SRC_ADDR
        src_port = FUSION_GUI_PORT

        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        #sock.settimeout(10)

        try:
            sock.connect((src_addr, src_port))
        except:
            print "Error connecting to {}:{}".format(src_addr, src_port)
            return None
        print "Successfully connected to host "
        return sock

    def receive_all(self, size):
        result = b''
        while len(result) < size:
            data = self.sock.recv(size - len(result))
            if not data:
                raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
            result += data
        return result

    def map_events(self, event):
        event = event.replace("grab move", "carry")
        event = event.replace("push back", "pull")
        return event

    def receive(self):
        events = self.receive_all(struct.calcsize("!i"))
        events, = struct.unpack("!i", events)

        event_list = []
        for e in range(events):
            event_length = self.receive_all(struct.calcsize("!i"))
            event_length, = struct.unpack("!i", event_length)

            event = self.receive_all(event_length)

            event = self.map_events(struct.unpack("!" + str(event_length) + "s", event)[0])
            print event
            event_list.append(event)

        probabilities = self.receive_all(struct.calcsize("!79f"))
        probabilities = list(struct.unpack("!79f", probabilities))
        decoded_frame =  probabilities + event_list
        return decoded_frame


    def workerThread1(self):

        while self.running:
            decoded_frame = self.receive()
            self.queue.put(decoded_frame)

        self.sock.close()

    def endApplication(self):
        self.running = 0


ThreadedClient()
