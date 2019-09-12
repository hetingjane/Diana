import socket
import select
import numpy as np
import time  # (needed in this demo only to do the slow counter loop)
from itertools import chain


class SocketAPI:
    def __init__(self, tcp_ip, tcp_port, buffer_size, ack_timeout, terminator, debug=False):
        self.tcp_ip = tcp_ip
        self.tcp_port = tcp_port
        self.buffer_size = buffer_size
        self.ack_timeout = ack_timeout
        self.terminator = terminator
        self.debug = debug

        # Create the global connection to the DataStore server
        self.socket_connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket_connection.connect((self.tcp_ip, self.tcp_port))
        self.socket_connection.setblocking(False)

    # Method to send a command to the server, followed by proper terminator.
    def send_to_server(self, cmd, *args):
        # send a message to the server (IMPORTANT: remmeber to include the terminator)
        msg = cmd + ' ' + ' '.join(args) + self.terminator
        msgBytes = msg.encode('utf-8')
        self.socket_connection.send(msgBytes)
        if self.debug:
            print("Sent: " + msg)

        # wait for a reply
        # (this part is optional but probably a good idea)
        ready = select.select([self.socket_connection], [], [], self.ack_timeout)
        if ready[0]:
            reply = self.socket_connection.recv(self.buffer_size).decode('utf-8')
            if self.debug:
                print("Received:", reply)
        else:
            print("No reply received within", self.ack_timeout, "seconds")

    def set(self, key, value):
        if type(value) == str:
            cmd = "SETS"
        elif type(value) == int:
            cmd = "SETI"
        elif type(value) == float:
            cmd = "SETF"
        elif type(value) == np.ndarray:
            cmd = "SETV"
        else:
            print("value type not supported, posting STRING")
            cmd = "SETS"

        self.send_to_server(cmd, key, str(value))

    def subscribe(self, key):
        self.send_to_server("SUB", key)

    def close(self):
        self.socket_connection.close()