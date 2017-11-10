import numpy as np
from itertools import chain



#Normalization by other joints
def normalize_by_joint(data, joint_index, verbose=False):
    dims = 3
    #print 'normalization joint index is: ', joint_index
    b = np.copy(data)
    b = b.astype(np.float64)
    joint = np.zeros(dims)

    for i in range(dims):
        joint[i] = np.mean(b[:, (joint_index*dims + i)])

    m, n = b.shape
    for i in range(n):
        b[:, i] -= joint[i % dims]


    if verbose:
        print 'data before normalization: ', data
        print 'mean of the spine joint:', joint
        print 'data after normalization: ', b

    return b



def normalize_joint_dataset(data_list, joint_index, verbose=False):
    return [normalize_by_joint(data, joint_index, verbose=verbose) for data in data_list]




def get_pruned_dataset(data_list, label_list, window_size):
    temp_data = []
    temp_label = []
    for i in range (len(data_list)):
        num_samples = data_list[i].shape[0] #- 4
        if(num_samples>=window_size):
            temp_data.append(data_list[i])

            for iter in xrange(len(data_list[i]) - window_size + 1):
                temp_label.append(label_list[i])

    return temp_data, temp_label #[data for data in data_list if ((data.shape[0]-4)>= window_size)]


def prune_joints(data, body_part='RA'):
    if (body_part=='RA'):
        joints = [8, 9, 10]#[0, 1, 2, 3, 4, 5, 8, 9, 12]  #only 9 joints considered, all x,y,z values taken, thus, dimensioanlity = 51
        points = list(chain(*[[(i * 3), (i * 3 + 1), (i * 3 + 2)] for i in joints]))
    elif (body_part=='LA'):
        joints = [4, 5, 6] #all 17 joints considered, x values taken, thus dimensioanlity = 17
        points = list(chain(*[[(i * 3), (i * 3 + 1), (i * 3 + 2)] for i in joints]))
    elif (body_part == 'arms_x'):
        joints = [5, 6, 7, 9, 10, 11]  # 3 joints of right and left hand considered: (hand, elbow and wrist) only x : dimensionality = 6
        points = list(chain(*[[(i * 3)] for i in joints])) #points = list(chain(*[[(i * 3), (i * 3 + 1), (i * 3 + 2)] for i in joints]))
    elif (body_part == 'arms_y'):
        joints = [5, 6, 7, 9, 10, 11]  # 3 joints of right and left hand considered: (hand, elbow and wrist) only x : dimensionality = 6
        points = list(chain(*[[(i * 3 + 1)] for i in joints])) #points = list(chain(*[[(i * 3), (i * 3 + 1), (i * 3 + 2)] for i in joints]))
    elif (body_part == 'arms'):
        joints = [5,6,7,9,10,11,12]  # 3 joints of right and left hand considered: (hand, elbow and wrist) only x : dimensionality = 6
        points = list(chain(*[[(i * 3), (i * 3 + 1), (i * 3 + 2)] for i in joints]))
    return data[:, points]


def prune_joints_dataset(data_list, body_part):
    return [prune_joints(data, body_part) for data in data_list]








