import select
import socket
import struct
import threading
from collections import namedtuple

from .conf import streams
from .conf.postures import right_hand_postures, head_postures
from .conf.endpoints import serve
from .thread_sync import synced_msgs
from .sync import Synchronizer


class Fusion(threading.Thread):

    Header = namedtuple('Header', ['id', 'timestamp', 'name'])

    BodyData = namedtuple('BodyData', ['idx_l_arm', 'idx_r_arm', 'idx_body',
                                       'pos_l_x', 'pos_l_y', 'var_l_x', 'var_l_y', 'pos_r_x', 'pos_r_y', 'var_r_x', 'var_r_y',
                                       'p_emblem', 'p_motion', 'p_neutral', 'p_oscillate', 'p_still',
                                       'p_l_arm', 'p_r_arm',
                                       'engaged'])
    HandData = namedtuple('HandData', ['idx_hand', 'probabilities', 'hand_type'])

    HeadData = namedtuple('HeadData', ['idx_head', 'probabilities'])

    SpeechData = namedtuple('SpeechData', ['command'])

    Message = namedtuple('Message', ['header', 'data'])

    def __init__(self):
        threading.Thread.__init__(self)
        self.daemon = True
        self._stop = threading.Event()
        self._connected_clients = {}
        self._synchronizer = Synchronizer(streams.get_active_stream_names())

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
        header_format = "<iq"
        stream_header = self._recv_all(sock, struct.calcsize(header_format))
        header_data = struct.unpack(header_format, stream_header)
        stream_name = streams.get_stream_name(header_data[0])
        header_data += (stream_name,)
        return Fusion.Header(*header_data)

    def _read_body_data(self, sock):
        # Left Max Index, Right Max Index, Body Max Index
        # Left point x, y, var_x, var_y, Right point x, y, var_x, var_y
        # 5 probabilities (emblem, motion, neutral, oscillate, still),
        # 6 probabilities for move left, right, up, down, front, back * 2, Engage (1/0)
        data_format = "<" + "iii" + "4f" * 2 + "5f" + "6f" * 2 + "i"
        raw_data = self._recv_all(sock, struct.calcsize(data_format))
        body_data = struct.unpack(data_format, raw_data)
        larm_probs = body_data[-13:-7]
        rarm_probs = body_data[-7:-1]
        engaged = body_data[-1] == 1
        body_data = body_data[:-13] + (larm_probs, rarm_probs, engaged)
        return Fusion.BodyData(*body_data)

    def _read_hands_data(self, sock, hand):
        # Max Index, Probabilities
        data_format = "<" + "i" + "f" * len(right_hand_postures)
        raw_data = self._recv_all(sock, struct.calcsize(data_format))
        hand_data = struct.unpack(data_format, raw_data)
        if hand == 'LH':
            hand_type = 'left'
        elif hand == 'RH':
            hand_type = 'right'
        else:
            raise ValueError('hand must be either LH or RH: ' + hand)
        return Fusion.HandData(hand_data[0], hand_data[1:], hand_type)

    def _read_head_data(self, sock):
        data_format = "<" + "i" + "f" * len(head_postures)
        raw_data = self._recv_all(sock, struct.calcsize(data_format))
        head_data = struct.unpack(data_format, raw_data)
        return Fusion.HeadData(head_data[0], head_data[1:])

    def _read_speech_data(self, sock):
        # Expect little endian byte order
        endianness = "<"
        command_length = struct.unpack(endianness + "i", self._recv_all(sock, 4))[0]
        command_bytes = self._recv_all(sock, command_length)
        command = struct.unpack(endianness + str(command_length) + "s", command_bytes)[0]
        return Fusion.SpeechData(command)

    def _read_stream_data(self, sock, stream_id):
        stream_name = streams.get_stream_name(stream_id)
        if stream_name in ["LH", "RH"]:
            return self._read_hands_data(sock, stream_name)
        elif stream_name == "Body":
            return self._read_body_data(sock)
        elif stream_name == "Head":
            return self._read_head_data(sock)
        elif stream_name == "Speech":
            return self._read_speech_data(sock)

    def _handle_client(self, sock):
        header = self._read_stream_header(sock)
        data = self._read_stream_data(sock, header.id)
        return Fusion.Message(header, data)

    def stop(self):
        self._stop.set()

    def is_stopped(self):
        return self._stop.is_set()

    def _accept_stream(self, sock, addr):
        try:
            stream_id_bytes = self._recv_all(sock, 4)
            stream_id = struct.unpack('<i', stream_id_bytes)[0]
        except Exception:
            print("Unable to receive complete stream id. Ignoring the client")
            sock.close()
        print("Received stream id. Verifying...")
        if streams.is_valid_id(stream_id):
            stream_name = streams.get_stream_name(stream_id)
            if streams.is_active(stream_name):
                print("Stream is valid and active: {}".format(stream_name))
                print("Checking if stream is already connected...")
                if stream_name not in list(self._connected_clients.values()):
                    print("New stream. Accepting the connection {}:{}".format(addr[0], addr[1]))
                    sock.shutdown(socket.SHUT_WR)
                    self._connected_clients[sock] = stream_name
                    return True
                else:
                    print("Stream already exists. Rejecting the connection.")
                    sock.close()
            else:
                print("Rejecting inactive stream: {}".format(stream_name))
                sock.close()
        else:
            print("Rejecting invalid stream with id: {}".format(stream_id))
            sock.close()
        return False

    def run(self):
        serv_sock = serve('fusion')
        serv_sock.listen(5)

        inputs = [serv_sock]
        outputs = []
        excepts = []

        print("Waiting for clients to connect")

        while not self.is_stopped():
            try:
                read_socks, write_socks, except_socks = select.select(inputs, outputs, excepts, 0.01)
            except socket.error:
                for sock in inputs:
                    try:
                        select.select([sock], [], [], 0)
                    except Exception:
                        print("{} client disconnected".format(self._connected_clients[sock]))
                        inputs.remove(sock)
                        self._connected_clients.pop(sock)
                continue

            for s in read_socks:
                if s is serv_sock:
                    client_sock, client_addr = s.accept()
                    # 1 is for the server socket
                    if self._accept_stream(client_sock, client_addr):
                        inputs += [client_sock]
                else:
                    try:
                        msg = self._handle_client(s)
                    except (socket.error, EOFError):
                        print("{} client disconnected".format(self._connected_clients[s]))
                        inputs.remove(s)
                        self._connected_clients.pop(s)
                        continue

                    self._synchronizer.feed(msg.header.name, msg.header.timestamp, msg)

                    if self._synchronizer.is_synced():
                        synced_msgs.put(self._synchronizer.get_synced_data()[1])

        self._synchronizer.reset()

        print("Stopped fusion thread")

        for s in inputs:
            try:
                s.close()
            except socket.error:
                pass
