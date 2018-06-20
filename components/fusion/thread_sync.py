import queue
import threading

# Setup a FIFO queue for sharing received data across threads
synced_msgs = queue.Queue()

remote_events = queue.Queue()

remote_connected = threading.Event()

gui_events = queue.Queue()

gui_connected = threading.Event()
