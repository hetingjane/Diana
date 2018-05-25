import numpy as np
from itertools import chain
from collections import deque

from ..fusion.conf.postures import left_arm_motions, right_arm_motions
from .receiveAndShow import Pointing


class Solver(object):
    def __init__(self, pointing_mode, lstm=False):
        self._lstm = lstm
        self._rgb = False

        self._window_threshold = 15
        self._feature_size = 10 if self._rgb else 21
        self._logpath = '/s/red/a/nobackup/vision/dkpatil/demo/GRU_5_class/'


        self._LEFT, self._RIGHT = 'la', 'ra'
        self._body_parts = [self._LEFT, self._RIGHT]


        self.joints_list = ['SPINE_BASE', 'SPINE_MID', 'NECK', 'HEAD', 'SHOULDER_LEFT', 'ELBOW_LEFT', 'WRIST_LEFT',\
                       'HAND_LEFT','SHOULDER_RIGHT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT', 'SPINE_SHOULDER', \
                            'HAND_TIP_LEFT','THUMB_LEFT', 'HAND_TIP_RIGHT', 'THUMB_RIGHT']
        self.num_joints = 17

        self.joint_dictionary = self._joint_dictionary_for_frame(None)



        self.thresholds_dictionary = {
            'skeleton_box_left_x': -0.83,
            'skeleton_box_right_x': 0.80,
            'arm_motion_threshold': 0.15, #if rgb 40.0,
            'axis_threshold': 0.3, #if rgb 0.5# May change after including z axis
            'dangling_arm_distance_threshold' : 30
        }

        self._class_list = np.load(self._logpath+'labels_list.npy')
        self._data_stream = deque([], maxlen=self._window_threshold)

        if self._lstm:
            from .GRU_classifier import (GRU_RNN, EGGNOGClassifierSlidingWindow)
            self._num_classes = 5
            model = GRU_RNN(logs_path=self._logpath, features=self._feature_size, n_classes=self._num_classes)
            self._solver = EGGNOGClassifierSlidingWindow(model=model, restore_model=True, num_classes=self._num_classes)

        self._wave_flag = False
        self._pointing_mode = pointing_mode
        self.point = Pointing()


    def feed_input(self, fd):
        self._fd = fd
        self.timestamp, frame_type, body_count, self.engaged = self._fd[:4]
        self._input_data = self._fd[4:]


        self.call_recognition()


    def get_result(self):
        # print 'Length of result is: ', len(self.result)
        assert len(self.result) == 29
        return self.result


    def printable_result(self):
        # Debugging mode
        LA_motion_label = left_arm_motions[self.result[0]]
        RA_motion_label = right_arm_motions[self.result[1]]
        classifier_label = 'body '+ self._class_list[self.result[2]]


        to_print_result = [LA_motion_label,RA_motion_label,classifier_label]
        if to_print_result == ['la blind', 'ra blind', 'body still']:
            return None
        else:
            return to_print_result


    def call_recognition(self):
        self.check_enagage()

        arm_encoding_list, probability_list = self.build()

        pointing_list = self.get_pointing_values()
        self.result = arm_encoding_list + list(chain(*pointing_list)) + list(chain(*probability_list)) + [int(self._engaged)]


    def get_skeleton_data(self):
        base, offset = 9, list(range(6, 10))
        joints_to_consider = list(np.arange(12)) + list(np.arange(20, 25))

        indices = list(chain(*[[(k * base + j) for j in offset] for k in joints_to_consider]))
        data = np.array([self._input_data[i] for i in indices]).reshape((1, -1))
        return data


    def _joint_dictionary_for_frame(self, frame):
        joint_dict = {self.joints_list[i]:[] for i in range(self.num_joints)}
        for joint_index in range(self.num_joints):
            key = self.joints_list[joint_index]
            if frame is None:
                joint_dict[key] = [(joint_index * 4 + u) for u in range(4)] #Current implementation is only for Kinect sensor, NOT RGB
            else:
                joint_dict[key] = [frame[index] for index in self.joint_dictionary[key]]
        return joint_dict


    def check_enagage(self):
        if self.engaged:
            frame = self.get_skeleton_data()[0]
            joint_dict_for_frame = self._joint_dictionary_for_frame(frame)

            sb_x = joint_dict_for_frame['SPINE_BASE'][1]  #Format of joint data in dictionary is <JOINT_TRACKING_STATE, JOINT_x, JOINT_y, JOINT_z>
            if self.thresholds_dictionary['skeleton_box_left_x'] < sb_x < self.thresholds_dictionary['skeleton_box_right_x']:
                self._engaged = True
            else:
                self._engaged = False

        if self.engaged:self._engaged_bit = 'Engaged'
        else:self._engaged_bit = 'Disengaged'


    def get_pointing_values(self):
        if self._wave_flag:
            self.point.get_pointing_main(self._fd)
            lpoint, rpoint = self.point.lpoint, self.point.rpoint
            lvar, rvar = self.point.lpoint_var, self.point.rpoint_var
        else:
            lpoint, rpoint = [0.0, 0.0], [0.0, 0.0]
            lvar, rvar = [0.0, 0.0], [0.0, 0.0]

        return [lpoint, lvar, rpoint, rvar]


    def default_values(self,value_to_add=26):
        proba_array, encoding_array = [], []

        # Adding probability of 1.0 to still label for the 5 class probaility list
        # <Emblem>, <Motion>, <Neutral>, <Oscillate>, <Still>
        proba_array.append([0.0, 0.0, 0.0, 0.0, 1.0])

        for _ in self._body_parts:
            proba_array.append([0] * 6), encoding_array.append(value_to_add)

        # Adding index of 'body still' to the default values
        encoding_array.append(4)
        return encoding_array, proba_array


    def build(self):
        if self._engaged:
            self._data_stream.extend(self.get_skeleton_data())
            if len(self._data_stream) >= self._window_threshold:
                encoding_array, proba_array = self.arm_motion_result()
            else:
                # print 'buffer not full....sending default values'
                # Still (26) when engaged but buffer unfilled
                encoding_array, proba_array = self.default_values()
        else:
            # print 'Disengaged....clearing buffer' # Blind (32) when disengaged
            self._wave_flag = False
            encoding_array, proba_array = self.default_values(value_to_add=32)
            self._data_stream.clear()
        return encoding_array, proba_array


    def arm_motion_result(self):
        wave_array = []
        proba_array, encoding_array, motion_label_array = [], [], []
        data = np.vstack([frame for frame in self._data_stream])
        class_label, body_probabilities = self.classifier_prediction(data)
        proba_array.append(body_probabilities)


        for body_part in self._body_parts:
            pruned_data = self.prune_joints(data, body_part=body_part)
            active_arm = self.check_active_arm(pruned_data)
            # print body_part, 'Active' if active_arm else 'Dangling'

            if self._wave_flag:
                if active_arm:
                    motion_encoding, probabilities = self.arm_motion_direction(pruned_data, body_part=body_part)
                    # Decide between stable pointing and moving point
                    # Return still if pointing says still (26 for index and [0]*6 for probabilities)
                    if body_part == self._LEFT and self.point.lpoint_stable:
                        motion_encoding, probabilities = 26, [0] * 6
                    elif body_part == self._RIGHT and self.point.rpoint_stable:
                        motion_encoding, probabilities = 26, [0] * 6
                else:
                    motion_encoding, probabilities = 32, [0] * 6

                encoding_array.append(motion_encoding), proba_array.append(probabilities)
            else:
                wave_array.append(self.check_for_wave(pruned_data))

        encoding_array.append(class_label)

        #Checking condition for waves
        if np.any(wave_array):
            wave_indx = [i for i, j in enumerate(wave_array) if j == True]
            if not self._wave_flag:
                self._wave_flag = True
                encoding_array, proba_array = self.default_values()
            for indx in wave_indx:
                encoding_array[indx] = 31
        else:
            if not self._wave_flag:
                encoding_array, proba_array = self.default_values()

        return encoding_array, proba_array


    def check_for_wave(self, data):
        ELBOW, WRIST = 1, 2

        elbow_x = data[:, ELBOW * 4 + 1]
        elbow_y = data[:, ELBOW * 4 + 2]
        wrist_x = data[:, WRIST * 4 + 1]
        wrist_y = data[:, WRIST * 4 + 2]

        y_truth = list((wrist_y - elbow_y) > 0)
        x_truth = list((wrist_x - elbow_x) > 0)

        if np.all(y_truth) and x_truth.count(True) > 0 and x_truth.count(False) > 0:
            return True
        else:
            return False


    def prune_joints(self, data, body_part):
        if self._rgb:
            base = 2
            if body_part == 'arms':
                joints, offset = [1, 3, 4, 6, 7], list(range(2))
            elif body_part == self._RIGHT:
                joints, offset = [2, 3, 4], list(range(2))
            elif body_part == self._LEFT:
                joints, offset = [5, 6, 7], list(range(2))
            elif body_part == 'head':
                joints, offset = [0, 1, 14, 15, 16, 17], list(range(2))
        else:
            if body_part == 'arms':
                joints= ['ELBOW_LEFT','WRIST_LEFT', 'HAND_LEFT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT', 'SPINE_SHOULDER']
            elif body_part == self._RIGHT:
                joints = ['SHOULDER_RIGHT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'SPINE_BASE']
            elif body_part == self._LEFT:
                joints = ['SHOULDER_LEFT', 'ELBOW_LEFT', 'WRIST_LEFT', 'SPINE_BASE']
            elif body_part == 'arms_x':
                joints = ['ELBOW_LEFT', 'WRIST_LEFT', 'HAND_LEFT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT']
            elif body_part == 'arms_y':
                joints = ['ELBOW_LEFT', 'WRIST_LEFT', 'HAND_LEFT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT']

        indices = list(chain(*[self.joint_dictionary[k] for k in joints]))
        return data[:, indices]


    def check_active_arm(self, data):
        WRIST, SPINE_BASE = 2, 3
        avg = np.mean(data[:, [SPINE_BASE * 4 + 1, SPINE_BASE * 4 + 2, SPINE_BASE * 4 + 3]], axis=0)
        ref_y, ref_z = avg[1], avg[2]

        first_wrist = data[0, [WRIST * 4 + 1, WRIST * 4 + 2, WRIST * 4 + 3]]
        last_wrist = data[-1, [WRIST * 4 + 1, WRIST * 4 + 2, WRIST * 4 + 3]]

        threshold_z = self.thresholds_dictionary['dangling_arm_distance_threshold']
        if np.abs(first_wrist[-1] - ref_z) > threshold_z and np.abs(last_wrist[-1] - ref_z) > threshold_z:
            # print 'Active'
            return True
        else:
            # print 'Dangling'
            return False


    def classifier_prediction(self, data):
        if self._wave_flag and self._lstm:
            # Processing the GRU Classification for the 15 frame window
            pruned_data_for_solver = self.prune_joints(data, body_part='arms')
            if self._rgb:
                assert pruned_data_for_solver.shape == (self._window_threshold, 10)
            else:
                assert pruned_data_for_solver.shape == (self._window_threshold, 21)

            class_label, probabilities = self._solver.predict(pruned_data_for_solver)
            # Adding the probability values of 5 class first
        else:
            class_label, probabilities = 4, [0.0, 0.0, 0.0, 0.0, 1.0]
            # Adding the probability values of 5 class first
        return class_label, probabilities


    def arm_motion_direction(self, data, body_part):
        pass



class PrimalRecognition(Solver):
    def arm_motion_direction(self, data, body_part):
        if (body_part == self._RIGHT) or (body_part == self._LEFT):
            if body_part == self._RIGHT:
                arm_motion_array = right_arm_motions
            else:
                arm_motion_array = left_arm_motions
            direction, probabilities = self.get_arm_motion(data)
            arm_motion_label = body_part + direction
            try:
                motion_encoding = arm_motion_array.index(arm_motion_label)
            except:
                motion_encoding = 26
            return motion_encoding, probabilities

        elif (body_part == 'arms_x') or (body_part == 'arms_y'):
            return None


    def get_cumulative_threshold(self, tracked_info):
        threshold_values = [0.017, 0.101, 0.146]  # <<<<<<<<<<CHANGE THESE VALUES FOR EVERY JOINT"S SEPARATE VALUE
        n_joints = tracked_info.shape[1]
        joints_to_consider = [i for i in range(n_joints) if np.product(tracked_info[:, i])>= 256.0]
        arm_thresh = sum(threshold_values[k] for k in joints_to_consider)
        return arm_thresh, joints_to_consider


    def get_arm_motion(self, data):
        directions_list = ['right', 'left', 'up', 'down', 'back', 'front']
        orientation = []
        axes = 3

        arm_threshold, keep_ind = self.get_cumulative_threshold(data[:, 4 * np.arange(3)])  # 40.0 if rgb else 0.15  #Change this value
        keep_ind = list(chain(*[[j * 4 + off for off in range(1, 4)] for j in keep_ind]))
        print ('Threshold calculated: ', arm_threshold)

        data = data[:, keep_ind]
        rows, cols = data.shape

        axis_threshold = self.thresholds_dictionary['axis_threshold']
        proba_array = [0] * 6

        if rows > 0:
            motion_mag = sum([np.linalg.norm(data[i+1]-data[i]) for i in range(rows-1)])
            if motion_mag >= arm_threshold:
                delta = data[-1] - data[0]
                mag = np.linalg.norm(delta)

                for k in range(axes):
                    delta_in_axis = [j for (i, j) in enumerate(delta) if (i % axes == k)]
                    mag_contrib_in_axis = round(sum(np.square(delta_in_axis)) / float(mag ** 2), 2)

                    if mag_contrib_in_axis >= axis_threshold:
                        orient_indx = 1 - int(sum(delta_in_axis)>0)
                        direction = directions_list[k*axes+orient_indx]
                        orientation.append(direction)

                        proba_index = k * 2 + orient_indx
                        proba_array[proba_index] = mag_contrib_in_axis

                direction = ' move ' + ' '.join(orientation)
            else:
                direction = ' still'
        else:
            print ('arm not seen...sending blind value instead')
            direction = ' blind'
        return direction, proba_array


class ArmMotionRecogntion(Solver):
    def arm_motion_direction(self, data, body_part):
        pass