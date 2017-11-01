from itertools import chain

import numpy as np

from Compute import (calculate_direction_dataset, default_bit_array)
from Preprocessing import  prune_joints_dataset


def extract_data(a):
    joints_to_consider = list(np.arange(12))  + list(np.arange(20, 25))
    data = list(chain(*[(a[(i * 9 + 9)], a[(i * 9 + 10)], a[(i * 9 + 11)]) for i in joints_to_consider]))
    #data = list(chain(*[(a[(i*9+8)],a[(i*9+9)],a[(i*9+10)]) for i in joints_to_consider]))
    return data


def process_window_data(data, body_part):
    d = [prune_joints_dataset(d, body_part=body_part) for d in data]
    d = list(chain(*d))
    return calculate_direction_dataset(d, body_part)[0]


def collect_all_results(map_array, point_array, proba_array, value):
    return map_array + list(chain(*point_array)) + list(chain(*proba_array)) + [value]


def send_default_values(body_parts, engage_bit, value_to_add= 26):
    proba_array, map_array = [], []
    for b in body_parts:
        proba_array.append(default_bit_array(b, engage_bit).tolist()), map_array.append(value_to_add)

    return map_array, proba_array


def code_to_label_encoding(index):
    label_list = [
'Right',
'Left',
'Up',
'Down',
'Back',
'Front',
'Right Up',
'Right Down',
'Right Back',
'Right Front',
'Left Up',
'Left down',
'Left Back',
'Left Front',
'Up Back',
'Up Front',
'Back Down',
'Back Front',
'Right Up Back',
'Right Up Front',
'Right Down Back',
'Right Down Front',
'Left Up Back',
'Left Up Front',
'Left Down Back',
'Left Down Front',
'Still',
'Arms Apart', 'Arms together', 'Stack up', 'Stack down'
    ]
    return label_list[index]

'''
    # mag_threshold = get_threshold_for_body_part(body_part)
    #magnitude = get_magnitude(d)[0]  #returns a list with only one element, hence access the first index of the list to get the magnitude
    #print 'magnitude is: ',magnitude
    #print 'mag_threshold is: ', mag_threshold


    if magnitude>= mag_threshold: #determines if the magnitude of the window is greater than the threshold, if not report body still
        return calculate_direction_dataset(d, body_part)[0] #Directions determined for every delta frames, so deltas=(window_size-1)
    else:
        return default_bit_array(body_part)
    '''
