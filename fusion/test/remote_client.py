import socket
import struct
import sys

def recv_all(sock, size):
    result = b''
    while len(result) < size:
        data = sock.recv(size - len(result))
        if not data:
            raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
        result += data
    return result

if __name__ == '__main__':
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        s.connect((sys.argv[1], 9126))
    except:
        print "Can't connect"
        sys.exit(0)
    print "Connected as {}:{}".format(*s.getsockname())
    while True:
        try:
            data_size = recv_all(s, struct.calcsize("!i"))
            data_size, = struct.unpack("!i", data_size)

            data = recv_all(s, data_size)
            data_s, = struct.unpack("!" + str(data_size) + "s", data)
        except (socket.error, EOFError):
            print "Server disconnected"
            break

        print data_s.decode('utf-8')

    s.close()
