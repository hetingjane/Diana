import numpy as np

def default_bit_array(body_part):
    if (body_part=='RA') or (body_part=='LA'): return np.zeros(6)
    elif (body_part=='arms_x') or (body_part=='arms_y'): return np.array([1,0,0])


def get_threshold_for_body_part(body_part):
    d = {
        'RA' : 0.15,
        'LA' : 0.15
    }
    return d[body_part]


def calculate_magnitude(data): #this will give magnitude of every frame with 9 values
    return np.linalg.norm(data)


def get_magnitude(data_list): #this will give magnitude of every window of (window_size, 9)
    magnitude = []
    mag = 0
    for i in range(len(data_list)):
        for j in range(data_list[i].shape[0] - 1):
            mag += calculate_magnitude(data_list[i][j + 1] - data_list[i][j])
        magnitude.append(mag)
        mag = 0
    return magnitude


def calculate_direction(data, body_part): #data is a (window_size,9) array
    if (body_part=='RA')or(body_part=='LA'):
        #print "Processing for "+str(body_part)
        return get_arm_motion(data)
    elif (body_part=='arms_x') or (body_part=='arms_y'):
        return check_arms_apart_together(data)


def check_arms_apart_together(data):
    proba_array = np.zeros(3)
    bit_value = 0

    diff = data[-1] - data[0]
    movement_left = np.sum(np.abs(diff[:len(diff) / 2]))
    movement_right = np.sum(np.abs(diff[len(diff) / 2:]))
    if movement_right > 0 and movement_left > 0:
        first_frame = [np.abs(data[0][i + 3] - data[0][i]) for i in range(3)]
        last_frame = [np.abs(data[-1][i + 3] - data[-1][i]) for i in range(3)]

        if sum(np.subtract(last_frame, first_frame)) >= 0:
            proba_array[1] = 1
            bit_value = 1
        else:
            proba_array[2] = 1
            bit_value = 2
    else:
        proba_array[0]=1
        bit_value = 0

    return proba_array, bit_value


def get_arm_motion(data):

    def get_direction(seg_length, index, bit_array, thresh):
        val_to_set = thresh
        if (index == 0):  # orientation based on delta in x direction
            if (seg_length > 0.0): bit_array[0] = val_to_set  #Direction: Right
            elif (seg_length <= 0.0):bit_array[1] = val_to_set #Direction: Left
        elif (index == 1):  # orientation based on delta in y direction
            if (seg_length > 0.0): bit_array[2] = val_to_set #Direction: Up
            elif (seg_length <= 0.0): bit_array[3] = val_to_set #Direction: Down
        else:  # orientation based on delta in z direction
            if (seg_length > 0.0): bit_array[4] = val_to_set  #Direction: Back
            elif (seg_length <= 0.0): bit_array[5] = val_to_set #Direction: Front
        return bit_array

    def get_bit_array_to_integer_encoding(bit_array):
        encoding = [ [ 1.,  0.,  0.,  0.,  0.,  0.],
                     [ 0.,  1.,  0.,  0.,  0.,  0.],
                     [ 0.,  0.,  1.,  0.,  0.,  0.],
                     [ 0.,  0.,  0.,  1.,  0.,  0.],
                     [ 0.,  0.,  0.,  0.,  1.,  0.],
                     [ 0.,  0.,  0.,  0.,  0.,  1.],
                     [ 1.,  0.,  1.,  0.,  0.,  0.],
                     [ 1.,  0.,  0.,  1.,  0.,  0.],
                     [ 1.,  0.,  0.,  0.,  1.,  0.],
                     [ 1.,  0.,  0.,  0.,  0.,  1.],
                     [ 0.,  1.,  1.,  0.,  0.,  0.],
                     [ 0.,  1.,  0.,  1.,  0.,  0.],
                     [ 0.,  1.,  0.,  0.,  1.,  0.],
                     [ 0.,  1.,  0.,  0.,  0.,  1.],
                     [ 0.,  0.,  1.,  0.,  1.,  0.],
                     [ 0.,  0.,  1.,  0.,  0.,  1.],
                     [ 0.,  0.,  0.,  1.,  1.,  0.],
                     [ 0.,  0.,  0.,  1.,  0.,  1.],
                     [ 1.,  0.,  1.,  0.,  1.,  0.],
                     [ 1.,  0.,  1.,  0.,  0.,  1.],
                     [ 1.,  0.,  0.,  1.,  1.,  0.],
                     [ 1.,  0.,  0.,  1.,  0.,  1.],
                     [ 0.,  1.,  1.,  0.,  1.,  0.],
                     [ 0.,  1.,  1.,  0.,  0.,  1.],
                     [ 0.,  1.,  0.,  1.,  1.,  0.],
                     [ 0.,  1.,  0.,  1.,  0.,  1.],
                     [ 0.,  0.,  0.,  0.,  0.,  0.]]
        return encoding.index(bit_array)


    data_mag = get_magnitude([data])[0]
    mag_threshold = get_threshold_for_body_part('RA')

    #print mag_threshold, data_mag
    bit_array = np.zeros(6)
    proba_array = np.zeros(6)

    if data_mag>= mag_threshold:
        delta = data[-1]-data[0]
        mag = calculate_magnitude(delta)
        for k in range(3):
            delta_joint = [j for (i, j) in enumerate(delta) if (i % 3 == k)]
            square_delta = np.array([j ** 2 for j in delta_joint], dtype=float)
            thresh = sum(square_delta) / float(mag ** 2)
            seg = sum(delta_joint)  # Sum of delta of respective joints

            proba_array = get_direction(seg, k, proba_array, thresh)
            if (thresh) > 0.3:
                bit_array = get_direction(seg, k, bit_array, 1)

    return proba_array, get_bit_array_to_integer_encoding(map(int, list(bit_array)))


def calculate_direction_dataset(data_list, body_part, depth=2):
    #calculates direction of every (window_size, 9) array
    if depth==2:
        return [calculate_direction(data, body_part) for data in data_list]
    elif depth==3:
        return [[calculate_direction(data, body_part) for data in video] for video in data_list]


def check_engage_disengage(data):
    def get_angle_of_shoulder_vector(data):
        ref_shoulder_vector = [0.99478572, -0.01661349,  0.1006249]
        shoulder_vector = np.array([data[i] for i in [24, 25, 26]]) - np.array([data[i] for i in [12, 13, 14]])

        try:
            shoulder_vector /= np.linalg.norm(shoulder_vector)
        except:
            shoulder_vector /= 1
        angle = np.dot(ref_shoulder_vector, shoulder_vector)
        if (angle < 0.8):
            #print "Angle threshold failed"
            return 0
        else:
            return 1


    def get_distance_bool(data):
        if data[2]>2.1:
            #print "Distance threshold failed"
            return 0
        else: return 1

    if (get_distance_bool(data)): #and get_angle_of_shoulder_vector(data)):
        #print "Engage"
        return 1
    else:
        #print "Disengage"
        return 0


'''
def get_head_motion(data):

    def magnitudes_for_axes(data):
        delta_x = 0
        delta_y = 0
        delta_z = 0
        for i in range(len(data) - 1):
            delta = data[i + 1] - data[i]
            delta_x += calculate_magnitude(delta[[0, 3, 6]])
            delta_y += calculate_magnitude(delta[[1, 4, 7]])
            delta_z += calculate_magnitude(delta[[2, 5, 8]])
        return delta_x, delta_y, delta_z

    bit_array=np.zeros(3)
    delta_x, delta_y, delta_z= magnitudes_for_axes(data)
    shake_threshold = 0.00051590
    nod_threshold = 0.003590
    if (delta_x > delta_y) and (delta_x > delta_z) and (delta_x>shake_threshold):
        bit_array[1] = 1
    elif (delta_x < delta_y) and (delta_x < delta_z) and (delta_x<nod_threshold):
        bit_array[2] = 1
    else: bit_array[0] = 1

    return bit_array
'''


'''
data = np.random.randint(1,19, size=9)
print data
calculate_direction(data)
'''