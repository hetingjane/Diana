import socket
import struct
from collections import namedtuple

from . import streams

HostInfo = namedtuple('HostInfo', 'port,can_connect,can_serve')

_hosts = {
    'kinect': HostInfo(8000, True, False),
    'fusion': HostInfo(9125, True, True),
    'brandeis': HostInfo(9126, False, True),
    'gui': HostInfo(9127, False, True)
}

def connect(hostrole, hostname, stream_str, timeout=False):
    """
    Connect to a host
    :param hostrole Role of the host in the system [fusion|kinect]
    :param hostname Host name of the machine to which you are connecting
    :param stream_str: Accepted values are those defined in streams module
    :param timeout: True to set the socket to timeout after 10s, False means no timeout
    :return: Socket object on successfull connection, None otherwise
    """

    port, can_connect, _ = _hosts[hostrole]
    if can_connect:
        addr = (hostname, port)
    else:
        raise ValueError("{} is configured to be not connectable".format(hostrole))

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    if timeout:
        sock.settimeout(10)

    try:
        sock.connect(addr)
    except socket.error:
        print "Failed to connect to {} at '{}:{}'".format(hostrole, addr[0], addr[1])
        return None

    try:
        print "Sending stream info"
        stream_id = streams.get_stream_id(stream_str)
        sock.sendall(struct.pack('<i', stream_id))
    except socket.error:
        print "Error: {} refused to accept stream id".format(hostrole)
        return None

    print "Successfully connected to {}".format(hostrole)
    return sock


def serve(hostrole, hostname='', reuse=True):
    """
    Initialize a server socket
    :param hostrole: Role of server socket
    :param hostname: Hosting address, by default ''
    :param reuse: Reuse the server socket
    :return: Returns the initialized server socket
    """
    port, _, can_serve = _hosts[hostrole]
    if can_serve:
        addr = (hostname, port)
    else:
        raise ValueError("{} is configured to be not servable".format(hostrole))

    serv_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    if reuse:
        serv_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    serv_sock.bind(addr)
    return serv_sock
