import select
import socket
import struct

from conf import streams
from conf.postures import body_postures, right_hand_postures
from thread_sync import *


class Fusion(threading.Thread):

    def __init__(self):
        self.data_received = {}
        self._stop = threading.Event()
        self.synced = False
        threading.Thread.__init__(self)

    def _recv_all(self, sock, size):
        result = b''
        while len(result) < size:
            data = sock.recv(size - len(result))
            if not data:
                raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
            result += data
        return result

    def _read_stream_header(self, sock):
        # ID, Timestamp
        header_format = "!iq"
        stream_header = self._recv_all(sock, struct.calcsize(header_format))
        stream_type, timestamp = struct.unpack(header_format, stream_header)
        if streams.is_valid(stream_type):
            return stream_type, timestamp
        else:
            raise KeyError("Stream " + str(stream_type) + " not found in the list of valid streams")

    def _read_body_data(self, sock):
        # Left Max Index, Right Max Index, 6 probabilities for move left, right, up, down, front, back * 2, Engage (1/0)
        data_format = "!" + "ii" + "ffffff" * 2 + "i"
        raw_data = self._recv_all(sock, struct.calcsize(data_format))
        return struct.unpack(data_format, raw_data)

    def _read_hands_data(self, sock):
        # Max Index, Probabilities
        data_format = "!" + "i" + "f" * len(right_hand_postures)
        raw_data = self._recv_all(sock, struct.calcsize(data_format))
        return struct.unpack(data_format, raw_data)

    def _read_stream_data(self, sock, stream_id):
        if streams.get_stream_type(stream_id) in ["LH", "RH"]:
            return self._read_hands_data(sock)
        elif streams.get_stream_type(stream_id) == "Body":
            return self._read_body_data(sock)

    def _handle_client(self, sock):
        stream_type, timestamp = self._read_stream_header(sock)
        data = self._read_stream_data(sock, stream_type)
        return (stream_type, timestamp) + data

    def stop(self):
        self._stop.set()

    def is_stopped(self):
        return self._stop.is_set()

    def run(self):
        serv_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        serv_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

        serv_sock.bind(('', 9125))

        serv_sock.listen(5)

        inputs = [serv_sock]
        outputs = []
        excepts = []

        print "Waiting for clients to connect"

        while not self.is_stopped():
            try:
                read_socks, write_socks, except_socks = select.select(inputs, outputs, excepts, 0.01)
            except socket.error:
                for sock in inputs:
                    try:
                        select.select([sock], [], [], 0)
                    except:
                        print str(sock.getpeername()) + " disconnected."
                        inputs.remove(sock)
                continue

            for s in read_socks:
                if s is serv_sock:
                    client_sock, client_addr = s.accept()
                    # 1 is for the server socket
                    if len(inputs) < streams.get_stream_count() + 1:
                        print "Accepted client {}:{}".format(client_addr[0], client_addr[1])
                        client_sock.shutdown(socket.SHUT_WR)
                        inputs += [client_sock]
                    else:
                        print "Rejecting client {}:{}".format(client_addr[0], client_addr[1])
                        client_sock.close()
                else:
                    try:
                        data = self._handle_client(s)
                    except (socket.error, EOFError):
                        print str(s.getpeername()) + " disconnected."
                        inputs.remove(s)
                        self.synced = False
                        continue

                    # Read and discard data unless enough clients connect
                    if len(inputs) == streams.get_stream_count() + 1:
                        cur_ts = data[1]

                        # Add data to appropriate timestamp bucket
                        if not self.data_received.has_key(cur_ts):
                            self.data_received[cur_ts] = []
                        self.data_received[cur_ts] += [data]

                        # Can we sync in presence of this new data
                        # Will run every time until we are synced
                        if not self.synced:
                            # Try to find a sync point if it exists
                            for ts in self.data_received.keys():
                                if len(self.data_received[ts]) == streams.get_stream_count():
                                    # This is the sync point, remove all older timestamp keys
                                    sync_ts = ts
                                    print "Synchronized at timestamp: " + str(sync_ts)
                                    # Remove all older keys the instant we find a sync timestamp
                                    for t in self.data_received.keys():
                                        if t < sync_ts:
                                            self.data_received.pop(t)
                                    self.synced = True
                                    break

                        if self.synced:
                            sync_ts = min(self.data_received.keys())
                            if len(self.data_received[sync_ts]) == streams.get_stream_count():
                                # Create a shared data object
                                s_data = {}
                                for dt in self.data_received[sync_ts]:
                                    stream_type = dt[0]
                                    s_data[stream_type] = dt

                                synced_data.put(s_data)

                                self.data_received.pop(sync_ts)

        print "Stopped network thread"

        for s in inputs:
            s.close()