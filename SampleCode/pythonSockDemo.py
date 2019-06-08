# pythonSockDemo.py
#
#	This file demonstrates how to connect via sockets to the DataStore server
#	(implemented in Unity as SocketInterfaceModule.cs).  Run with python3.

import socket
import select
import time		# (needed in this demo only to do the slow counter loop)

TCP_IP = '127.0.0.1'	# IP address to connect to
TCP_PORT = 38276		# port number to connect to
BUFFER_SIZE = 1024		# size of receive buffer (1k should be plenty)
ACK_TIMEOUT = 2			# how long to wait for a response from the server
TERMINATOR = "\r\n"		# message terminator (required)

# Create the global connection to the DataStore server
socketConn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
socketConn.connect((TCP_IP, TCP_PORT))
socketConn.setblocking(0)

# Method to send a command to the server, followed by proper terminator.
def sendToServer(cmd, *args):
	# send a message to the server (IMPORTANT: remmeber to include the terminator)
	msg = cmd + ' ' + ' '.join(args) + TERMINATOR
	msgBytes = msg.encode('utf-8')
	socketConn.send(msgBytes)
	print("Sent: " + msg)

	# wait for a reply
	# (this part is optional but probably a good idea)
	ready = select.select([socketConn], [], [], ACK_TIMEOUT)
	if ready[0]:
		reply = socketConn.recv(BUFFER_SIZE).decode('utf-8')
		print("Received:", reply)
	else:
		print("No reply received within", ACK_TIMEOUT, "seconds")

# Some helper methods for sending specific commands
def setInt(key, value):
	sendToServer("SETI", key, str(value))

def setString(key, value):
	sendToServer("SETS", key, str(value))

def subscribe(key):
	sendToServer("SUB", key)
	
# DEMO: Set the value of "demo:python:counter" to increase continuously
counter = 0
while True:
	setInt("demo:python:counter", counter)
	counter += 1
	time.sleep(1)

