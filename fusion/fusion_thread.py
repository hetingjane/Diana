import select
import socket
import struct

from support import streams
from support.postures import right_hand_postures, head_postures
from support.endpoints import serve
from thread_sync import *


class Fusion(threading.Thread):

    connected_clients = {}

    def __init__(self):
        threading.Thread.__init__(self)
        self.daemon = True
        self._data_received = {}
        self._stop = threading.Event()
        self._synced = False

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
        stream_id, timestamp = struct.unpack(header_format, stream_header)
        if streams.is_valid(stream_id):
            return stream_id, timestamp
        else:
            raise KeyError("Invalid stream ID: {}".format(stream_id))

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

    def _read_head_data(self, sock):
        data_format = "!" + "i" + "f" * len(head_postures)
        raw_data = self._recv_all(sock, struct.calcsize(data_format))
        return struct.unpack(data_format, raw_data)

    def _read_stream_data(self, sock, stream_id):
        if streams.get_stream_type(stream_id) in ["LH", "RH"]:
            return self._read_hands_data(sock)
        elif streams.get_stream_type(stream_id) == "Body":
            return self._read_body_data(sock)
        elif streams.get_stream_type(stream_id) == "Head":
            return self._read_head_data(sock)

    def _handle_client(self, sock):
        stream_type, timestamp = self._read_stream_header(sock)
        data = self._read_stream_data(sock, stream_type)
        return (stream_type, timestamp) + data

    def _all_connected(self):
        """
        Check if all the streams in active_streams as set in streams module have been connected
        :return: True if all active streams are connected, False otherwise
        """
        active_streams = streams.get_active_streams()
        for s in active_streams:
            if s not in self.connected_clients.values():
                return False
        return True

    def _set_sync(self, sync_ts):
        if not self._synced:
            print "Synchronized at timestamp: " + str(sync_ts)
            self._synced = True
            # Remove all older timestamps the instant we find a sync timestamp
            for t in self._data_received.keys():
                if t < sync_ts:
                    self._data_received.pop(t)

    def _unset_sync(self):
        if self._synced:
            print "Synchronization lost."
            self._synced = False
            self._data_received.clear()

    def _is_synced(self):
        return self._synced

    def stop(self):
        self._stop.set()

    def is_stopped(self):
        return self._stop.is_set()

    def run(self):
        serv_sock = serve('fusion')
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
                        print "Client disconnected."
                        inputs.remove(sock)
                        self.connected_clients.pop(sock)
                        self._unset_sync()
                continue

            for s in read_socks:
                if s is serv_sock:
                    client_sock, client_addr = s.accept()
                    # 1 is for the server socket
                    try:
                        stream_id_bytes = client_sock.recv(4, socket.MSG_WAITALL)
                        stream_id = struct.unpack('<i', stream_id_bytes)[0]
                    except:
                        print "Unable to receive complete stream id. Ignoring the client"
                        client_sock.close()
                    print "Received stream id. Verifying..."
                    if streams.is_valid(stream_id):
                        stream_str = streams.get_stream_type(stream_id)
                        print "Stream is valid: {}".format(stream_str)
                        print "Checking if stream is already connected..."
                        if stream_str not in self.connected_clients.values():
                            print "New stream. Accepting the connection {}:{}".format(client_addr[0], client_addr[1])
                            client_sock.shutdown(socket.SHUT_WR)
                            inputs += [client_sock]
                            self.connected_clients[client_sock] = stream_str
                        else:
                            print "Stream already exists. Rejecting the connection."
                            client_sock.close()
                    else:
                        print "Rejecting invalid stream with stream mask: {}".format(stream_id)
                        client_sock.close()
                else:
                    try:
                        data = self._handle_client(s)
                    except (socket.error, EOFError):
                        print "Client disconnected."
                        inputs.remove(s)
                        self.connected_clients.pop(s)
                        self._unset_sync()
                        continue

                    # Read and discard data unless enough clients connect
                    if self._all_connected():
                        cur_ts = data[1]

                        # Add data to appropriate timestamp bucket
                        if not self._data_received.has_key(cur_ts):
                            self._data_received[cur_ts] = []
                        self._data_received[cur_ts] += [data]

                        # Try to sync in presence of this new data
                        # Will run every time until we are synced
                        if not self._is_synced():
                            # Try to find a sync point if it exists
                            for ts in sorted(self._data_received.keys()):
                                # Weak check for all data received
                                if len(self._data_received[ts]) == streams.get_active_streams_count():
                                    # This is the sync point
                                    sync_ts = ts
                                    self._set_sync(sync_ts)
                                    break
                        else:
                            # Already synced
                            sync_ts = min(self._data_received.keys())
                            if len(self._data_received[sync_ts]) == streams.get_active_streams_count():
                                # Create a shared data object
                                s_data = {}
                                for dt in self._data_received[sync_ts]:
                                    stream_type = dt[0]
                                    s_data[stream_type] = dt

                                synced_data.put(s_data)
                                self._data_received.pop(sync_ts)

        print "Stopped network thread"

        for s in inputs:
            s.close()