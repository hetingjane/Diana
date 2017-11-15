import socket
import struct
import support.streams as streams

_KINECT_DEST_ADDR = 'cwc1'

_FUSION_DEST_ADDR = 'blue'
_FUSION_SRC_ADDR = ''

_GUI_DEST_ADDR = _FUSION_DEST_ADDR
_GUI_SRC_ADDR = _FUSION_SRC_ADDR

_BRANDEIS_SRC_ADDR = ''

_addresses = {
    'source': {
        'fusion': (_FUSION_SRC_ADDR, 9125),
        'brandeis': (_BRANDEIS_SRC_ADDR, 9126),
        'gui': (_GUI_SRC_ADDR, 9127)
    },
    'destination': {
        'kinect': (_KINECT_DEST_ADDR, 8000),
        'fusion': (_FUSION_DEST_ADDR, 9125),
        'gui': (_GUI_DEST_ADDR, 9127)
    }
}

def connect(dest, stream_str, timeout=False):
    """
    Connect to a machine in the system
    :param dest: Accepted values are those defined in _addresses['destination']
    :param stream_str: Accepted values are those defined in streams module
    :return: Socket object on successfull connection, None otherwise
    """
    stream_id = streams.get_stream_id(stream_str)
    addr = _addresses['destination'][dest]

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    if timeout:
        sock.settimeout(10)
    try:
        sock.connect(addr)
        try:
            print "Sending stream info"
            sock.sendall(struct.pack('<i', stream_id))
        except:
            print "Error: Destination '{}' refused to accept stream id".format(dest)
            return None
    except:
        print "Failed to connect to destination '{}' at '{}:{}'".format(*((dest,) + addr))
        return None

    print "Successfully connected to destination '{}'".format(dest)
    return sock

def serve(src, reuse=True):
    serv_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    if reuse:
        serv_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    serv_sock.bind(_addresses['source'][src])
    return serv_sock
