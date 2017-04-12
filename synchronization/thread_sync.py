import Queue
import threading

# Setup a FIFO queue for sharing received data across threads
synced_data = Queue.Queue()

output_data = Queue.Queue()

remote_connected = threading.Event()