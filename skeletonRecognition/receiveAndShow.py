'''
# import socket
# import struct
# import matplotlib.pyplot as plt
# import time
# from matplotlib.animation import FuncAnimation
# import threading

#src_addr = '129.82.45.102'
src_addr = '127.0.0.1'
src_port = 8000

stream_id = 32;

def connect():
    """
    Connect to a specific port
    """

    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    try:
        sock.connect((src_addr, src_port))
    except:
        print("Error connecting to {}:{}".format(src_addr, src_port))
        return None
    try:
        print("Sending stream info")
        sock.sendall(struct.pack('<i', stream_id));
    except:
        print("Error: Stream rejected")
        return None
    print("Successfully connected to host")
    return sock


def decode_frame(raw_frame):
    # The format is given according to the following assumption of network data

    # Expect little endian byte order
    endianness = "<"

    # [ commonTimestamp | frame type | Tracked body count | Engaged
    header_format = "qiBB"

    timestamp, frame_type, tracked_body_count, engaged = struct.unpack(endianness + header_format, raw_frame[:struct.calcsize(header_format)])

    # For each body, a header is transmitted
    # TrackingId | HandLeftConfidence | HandLeftState | HandRightConfidence | HandRightState ]
    body_format = "Q4B"

    # For each of the 25 joints, the following info is transmitted
    # [ JointType | TrackingState | Position.X | Position.Y | Position.Z | Orientation.W | Orientation.X | Orientation.Y | Orientation.Z ]
    joint_format = "BB7f"

    frame_format = body_format + (joint_format * 25)

    
    # Unpack the raw frame into individual pieces of data as a tuple
    frame_pieces = struct.unpack(endianness + (frame_format * (1 if engaged else 0)), raw_frame[struct.calcsize(header_format):])
    
    decoded = (timestamp, frame_type, tracked_body_count, engaged) + frame_pieces

    return decoded


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
    To read each stream frame from the server
    """
    (load_size,) = struct.unpack("<i", recv_all(sock, struct.calcsize("<i")))
    #print load_size
    return recv_all(sock, load_size)
'''
    
import numpy as np



# following codes get the elbow and wrist information from the kinect sensor
WRISTLEFT = 6 # JointType specified by kinect
WRISTRIGHT = 10
ELBOWLEFT = 5
ELBOWRIGHT = 9
# joint_interest = ['WRISTLEFT', 'WRISTRIGHT', 'ELBOWLEFT', 'ELBOWRIGHT']
joint_interest_coded = [WRISTLEFT, WRISTRIGHT, ELBOWLEFT, ELBOWRIGHT]

def getWristElbow(src):
    '''
    This function returns the coordinates for left/right wrists/elbows (4 sets of 3 values: x, y, z)
    '''
    result = {}
    for i in range(len(joint_interest_coded)):
        result[joint_interest_coded[i]] = None
        
    if len(src) <= 9:
        return result
    
    for i in range(25):
        if src[(i+1)*9] in joint_interest_coded:
            result[src[(i+1)*9]] = src[(i+1)*9 + 2: (i+2)*9 + 5]
    return result
    

def calcPointing(wrist, elbow):
    '''
    Both wrist and elbow should contain (x,y,z) coordinates
    Table plane: y = -0.582
    Line equation: 
    y = (y2-y1)/(x2-x1) * (x-x1) + y1
    z = (z2-z1)/(y2-y1) * (y-y1) + z1
    so:
    x = x1 - (y1-y) / (y2-y1) * (x2-x1)
    z = z1 - (y1-y) / (y2-y1) * (z2-z1)
    '''
    table_y = -0.582
    if elbow[1] == wrist[1]:
        return [-np.inf, -np.inf]

    table_x = wrist[0] - (wrist[1]-table_y) / (elbow[1] - wrist[1]) * (elbow[0] - wrist[0])
    table_z = wrist[2] - (wrist[1]-table_y) / (elbow[1] - wrist[1]) * (elbow[2] - wrist[2])

    return [table_x, table_z]

lpoint = [-0.5, 1.3] # inferred pointing coordinate on the table from left arm
rpoint = [0.5, 1.3] # inferred pointing coordinate on the table from left arm

def calculate_point(fd):
    result = getWristElbow(fd)
    lwrist = result[WRISTLEFT]
    rwrist = result[WRISTRIGHT]
    lelbow = result[ELBOWLEFT]
    relbow = result[ELBOWRIGHT]
    if lwrist is not None:
        lpoint = calcPointing(lwrist, lelbow)
        rpoint = calcPointing(rwrist, relbow)
    else:
        lpoint = [-np.inf, -np.inf]
        rpoint = [-np.inf, -np.inf]

    return lpoint, rpoint

'''
def updatePoint():

    Connect to the kinect server and get the coordinate

    global lpoint
    global rpoint
    s = connect()
    if s is None:
        sys.exit(0)
        
    while True:
        try:
            f = recv_skeleton_frame(s)
            result = getWristElbow(decode_frame(f))
            lwrist = result[WRISTLEFT]
            rwrist = result[WRISTRIGHT]
            lelbow = result[ELBOWLEFT]
            relbow = result[ELBOWRIGHT]
            if lwrist is None:
                continue
            lpoint = calcPointing(lwrist, lelbow)
            rpoint = calcPointing(rwrist, relbow)
            #print(lpoint, ' ', rpoint)
        except:
            s.close()
            break

if __name__ == '__main__':
    
    threading.Thread(target=updatePoint).start()
    
    # Plot where the pointing position is on the table
    # The table has a width of (-1,1) and depth of (1.0, 1.6)
    fig = plt.figure(figsize=(7, 7))
    ax = fig.add_subplot(111)
    
    def animate(i):
        ax.clear()
        ax.set_xlim(-1, 1)#, ax.set_xticks([])
        ax.set_ylim(1.0, 1.6)#, ax.set_yticks([])
        plt.gca().invert_yaxis()
        ax.plot(lpoint[0], lpoint[1], 'bo', label='left hand')
        ax.plot(rpoint[0], rpoint[1], 'ro', label='right hand')
    
    ani = FuncAnimation(fig, animate)
    plt.show()
'''
