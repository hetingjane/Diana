import socket, sys, struct
import time

src_addr = '129.82.45.102'
src_port = 8123

def connect():
    """
    Connect to a specific port
    """

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    try:
        sock.connect((src_addr, src_port))
        # Socket will only be used to read, so make it unidirectional
        sock.shutdown(socket.SHUT_WR)
    except:
        print "Error connecting to {}:{}".format(src_addr, src_port)
        return None
    print "Successfully connected to {}:{}".format(src_addr, src_port)
    return sock

def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect network byte order
    endianness = "!"

    # For each body, a header is transmitted
    # [ commonTimestamp | TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    header_format = "qQBBBB"
    
    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BBfffffff"

    frame_format = header_format + (joint_format * 25)

    body_frame_length = struct.calcsize(endianness + frame_format)
    
    # Confirm that the length of the frame is a valid one, i.e.
    # length of raw_frame is a multiple of length of single body frame
    # indicating multiple bodies in a single frame of skeleton data
    assert len(raw_frame) % body_frame_length == 0, "Frame length {} is not a multiple of single body frame length {}".format(len(raw_frame), body_frame_length)

    body_count = len(raw_frame) // body_frame_length
    
    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack(endianness + (frame_format * body_count), raw_frame)

    # Need to give the user a way to iterate over body frames if needed
    return frame_pieces

def recv_all(sock, size):
    result = b''
    while len(result) < size:
        data = sock.recv(size - len(result))
        if not data:
            raise EOFError("Error: Received only {} bytes into {} byte message".format(len(data), size))
        result += data
    return result

def recv_skeleton_frame(sock):
    """
    Experimental function to read each stream frame from the server
    """
    (frame_size,) = struct.unpack("!i", recv_all(sock, 4))

    return recv_all(sock, frame_size) 
    

# By default read 100 frames
if __name__ == '__main__':

    # Time the network performance 
    s = connect()
    
    i = 0

    avg_frame_time = 0.0
    
    while i<1000:
        t_begin = time.time()
        f = recv_skeleton_frame(s)
        t_end = time.time()
        
        print 'Time take for this frame: {}'.format(t_end - t_begin)
        avg_frame_time += (t_end - t_begin)
        print len(decode_frame(f))
        print
        print
        i += 1

    print "Total frame time: {}".format(avg_frame_time)

    avg_frame_time /= i
    
    print "Average frame time over {} frames: {}".format(i, avg_frame_time)
    
    s.close()
    sys.exit(0)
