import numpy as np
from itertools import chain
from collections import deque
import os
from ..fusion.conf.postures import left_arm_motions, right_arm_motions
from .receiveAndShow import Pointing


class Solver(object):
    def __init__(self, pointing_mode):
        self._rgb = False
        self._window_threshold = 15

        self._LEFT, self._RIGHT = 'la', 'ra'
        self._body_parts = [self._LEFT, self._RIGHT]

        self.joints_list = ['SPINE_BASE', 'SPINE_MID', 'NECK', 'HEAD', 'SHOULDER_LEFT', 'ELBOW_LEFT', 'WRIST_LEFT',\
                       'HAND_LEFT','SHOULDER_RIGHT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT', 'SPINE_SHOULDER', \
                            'HAND_TIP_LEFT','THUMB_LEFT', 'HAND_TIP_RIGHT', 'THUMB_RIGHT']
        self.num_joints = 17

        self._step = 4
        self.joint_dictionary = self._joint_dictionary_for_frame(None)

        self.thresholds_dictionary = {
            'skeleton_box_left_x': -0.83,
            'skeleton_box_right_x': 0.80,
            'arm_motion_threshold': 0.15, #if rgb 40.0,
            'axis_threshold': 0.3, #if rgb 0.5# May change after including z axis
            'dangling_arm_distance_threshold' : 0.08
        }


        self._data_stream = deque([], maxlen=self._window_threshold)
        self._wave_flag = False
        self._pointing_mode = pointing_mode
        self.point = Pointing()


    def feed_input(self, fd):
        self._fd = fd
        self.timestamp, frame_type, body_count, self.engaged = self._fd[:4]
        self._input_data = self._fd[4:]

        self.call_recognition()


    def get_result(self):
        assert len(self.result) == 27
        return self.result


    def printable_result(self):
        # Debugging mode
        LA_motion_label = left_arm_motions[self.result[0]]
        RA_motion_label = right_arm_motions[self.result[1]]

        to_print_result = [LA_motion_label,RA_motion_label]
        if to_print_result == ['la blind', 'ra blind']:
            return None
        else:
            return to_print_result


    def call_recognition(self):
        self.check_enagage()
        arm_encoding_list, probability_list = self.build()
        pointing_list = self.get_pointing_values()
        self.result = arm_encoding_list + list(chain(*pointing_list)) + list(chain(*probability_list)) + [int(self.engaged)]


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
                joint_dict[key] = [(joint_index * self._step + u) for u in range(self._step)] #Current implementation is only for Kinect sensor, NOT RGB
            else:
                joint_dict[key] = [frame[index] for index in self.joint_dictionary[key]]
        return joint_dict


    def check_enagage(self):
        if self.engaged:
            frame = self.get_skeleton_data()[0]

            joint_dict_for_frame = self._joint_dictionary_for_frame(frame)
            sb_x = joint_dict_for_frame['SPINE_BASE'][1]  #Format of joint data in dictionary is <JOINT_TRACKING_STATE, JOINT_x, JOINT_y, JOINT_z>
            if self.thresholds_dictionary['skeleton_box_left_x'] < sb_x < self.thresholds_dictionary['skeleton_box_right_x']:
                self.engaged = True
            else:
                self.engaged = False

        if self.engaged:self._engaged_bit = 'Engaged'
        else:self._engaged_bit = 'Disengaged'


    def get_pointing_values(self):
        if self._wave_flag:
            self.point.get_pointing_main(self._fd)
            lpoint, rpoint = self.point.lpoint, self.point.rpoint
            lvar, rvar = self.point.lpoint_var, self.point.rpoint_var
            self._lpoint_stable, self._rpoint_stable = self.point.lpoint_stable, self.point.rpoint_stable

        else:
            lpoint, rpoint = [0.0, 0.0], [0.0, 0.0]
            lvar, rvar = [0.0, 0.0], [0.0, 0.0]

        return [lpoint, lvar, rpoint, rvar]


    def default_values(self,value_to_add=26):
        proba_array, encoding_array = [], []
        for _ in self._body_parts:
            proba_array.append([0.0]*7+[1.0]), encoding_array.append(value_to_add)
        return encoding_array, proba_array


    def build(self):
        if self.engaged:
            self._data_stream.extend(self.get_skeleton_data())
            if len(self._data_stream) >= self._window_threshold:
                encoding_array, proba_array = self.arm_motion_result()
            else:
                # Still (26) when engaged but buffer unfilled
                encoding_array, proba_array = self.default_values()
        else:
            # print 'Disengaged....clearing buffer' # Blind (33) when disengaged
            self._wave_flag = False
            encoding_array, proba_array = self.default_values(value_to_add=33)
            self._data_stream.clear()
        return encoding_array, proba_array


    def arm_motion_result(self):
        wave_array = []
        proba_array, encoding_array, motion_label_array = [], [], []
        data = np.vstack([frame for frame in self._data_stream])

        for body_part in self._body_parts:
            pruned_data = self.prune_joints(data, body_part=body_part)

            if self._wave_flag:
                active_arm = self.check_active_arm(pruned_data)
                if active_arm:
                    wave = self.check_for_wave(pruned_data)
                    if not wave:
                        motion_encoding, probabilities = self.arm_motion_direction(pruned_data, body_part=body_part)
                        # Decide between stable pointing and moving point
                        # Return still if pointing says still (26 for index and [0]*6 for probabilities)
                        if body_part == self._LEFT and self._lpoint_stable:
                            motion_encoding, probabilities = 26, [0.0]*7+[1.0]
                        elif body_part == self._RIGHT and self._rpoint_stable:
                            motion_encoding, probabilities = 26, [0.0]*7+[1.0]
                    else:
                        motion_encoding, probabilities = 32, [0.0]*8
                else:
                    motion_encoding, probabilities = 33, [0.0]*7+[1.0]
                encoding_array.append(motion_encoding), proba_array.append(probabilities)
            else:
                wave_array.append(self.check_for_wave(pruned_data))

        #Checking condition for waves
        if np.any(wave_array):
            wave_indx = [i for i, j in enumerate(wave_array) if j == True]
            if not self._wave_flag:
                self._wave_flag = True
                encoding_array, proba_array = self.default_values()
            for indx in wave_indx:
                encoding_array[indx] = 32
        else:
            if not self._wave_flag:
                encoding_array, proba_array = self.default_values()

        return encoding_array, proba_array


    def check_for_wave(self, data):
        ELBOW, WRIST = 2, 3

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


    def check_active_arm(self, data):
        SPINE_BASE, WRIST = 0, 3

        avg = np.mean(data[:, [SPINE_BASE * self._step + 1, SPINE_BASE * self._step + 2, SPINE_BASE * self._step + 3]], axis=0)
        ref_z = avg[2]

        first_wrist = data[0, [WRIST * self._step + 1, WRIST * self._step + 2, WRIST * self._step + 3]]
        last_wrist = data[-1, [WRIST * self._step + 1, WRIST * self._step + 2, WRIST * self._step + 3]]


        threshold_z = self.thresholds_dictionary['dangling_arm_distance_threshold']
        if np.abs(first_wrist[-1] - ref_z) > threshold_z and np.abs(last_wrist[-1] - ref_z) > threshold_z:
            return True
        else:
            return False


    def arm_motion_direction(self, data, body_part):
        pass


    def prune_joints(self, data, body_part):
        pass




class ArmMotionRecogntion(Solver):
    def __init__(self, pointing_mode):
        super(ArmMotionRecogntion, self).__init__(pointing_mode=pointing_mode)
        from .LSTMSolver import RealTimeArmMotionRecognition
        from .Models import Arms_LSTM

        import tensorflow as tf
        g1 = tf.Graph()
        with g1.as_default():
            print ('Loading Left arm model')
            model_left = Arms_LSTM(logs_path=os.path.abspath('./models/body/la/model.ckpt-0'), n_hidden=30, n_layers=2)
        self._left_arm_model = RealTimeArmMotionRecognition(model_left)


        g2 = tf.Graph()
        with g2.as_default():
            print ('Loading Right arm model')
            model_right = Arms_LSTM(logs_path=os.path.abspath('./models/body/ra/model.ckpt-0'), n_hidden=75, n_layers=2)
        self._right_arm_model = RealTimeArmMotionRecognition(model_right)


    def prune_joints(self, data, body_part):
        if body_part == 'arms':
            joints= ['ELBOW_LEFT','WRIST_LEFT', 'HAND_LEFT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT', 'SPINE_SHOULDER']
        elif body_part == self._RIGHT:
            joints = ['SPINE_BASE', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT', 'SPINE_SHOULDER', 'HAND_TIP_RIGHT']
        elif body_part == self._LEFT:
            joints = ['SPINE_BASE', 'ELBOW_LEFT', 'WRIST_LEFT', 'HAND_LEFT', 'SPINE_SHOULDER', 'HAND_TIP_LEFT']

        indices = list(chain(*[self.joint_dictionary[k] for k in joints]))
        return data[:, indices]


    def arm_motion_direction(self, data, body_part):
        if (body_part == self._RIGHT) or (body_part == self._LEFT):
            if body_part == self._RIGHT:
                arm_motion_array = right_arm_motions
                model = self._right_arm_model
            else:
                arm_motion_array = left_arm_motions
                model = self._left_arm_model


            direction, probabilities = model.predict(data)
            direction = direction.decode('UTF-8')
            # #Rearranging order of probabilities
            probabilities = [probabilities[i] for i in [2, 3, 0, 1, 5, 4, 7, 6]]

            # print ('Direction predicted: ', type(direction), direction)
            arm_motion_label = body_part + ' ' + direction

            try:
                motion_encoding = arm_motion_array.index(arm_motion_label)
            except:
                motion_encoding = 26

            print ('Arm Motion Label :', arm_motion_label, ' motion index: ', motion_encoding)
            return motion_encoding, probabilities

        elif (body_part == 'arms_x') or (body_part == 'arms_y'):
            return None



class PrimalRecognition(Solver):
    def prune_joints(self, data, body_part):
        if body_part == 'arms':
            joints = ['ELBOW_LEFT', 'WRIST_LEFT', 'HAND_LEFT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'HAND_RIGHT',
                      'SPINE_SHOULDER']
        elif body_part == self._RIGHT:
            joints = ['SPINE_BASE', 'SHOULDER_RIGHT', 'ELBOW_RIGHT', 'WRIST_RIGHT', 'SPINE_SHOULDER']
        elif body_part == self._LEFT:
            joints = ['SPINE_BASE', 'SHOULDER_LEFT', 'ELBOW_LEFT', 'WRIST_LEFT', 'SPINE_SHOULDER']

        indices = list(chain(*[self.joint_dictionary[k] for k in joints]))
        return data[:, indices]


    def arm_motion_direction(self, data, body_part):
        if (body_part == self._RIGHT) or (body_part == self._LEFT):
            if body_part == self._RIGHT:
                arm_motion_array = right_arm_motions
            else:
                arm_motion_array = left_arm_motions
            direction, probabilities = self.get_arm_motion(data)

            arm_motion_label = body_part + ' ' + direction
            try:
                motion_encoding = arm_motion_array.index(arm_motion_label)
            except:
                motion_encoding = 26
            return motion_encoding, probabilities

        elif (body_part == 'arms_x') or (body_part == 'arms_y'):
            return None


    def get_cumulative_threshold(self, tracked_info):
        threshold_values = [0.017, 0.101, 0.125]
        n_joints = tracked_info.shape[1]
        joints_to_consider = [i for i in range(n_joints) if np.product(tracked_info[:, i])>= 256.0]
        arm_thresh = sum(threshold_values[k] for k in joints_to_consider)
        return arm_thresh, joints_to_consider


    def get_arm_motion(self, data):
        directions_list = ['right', 'left', 'up', 'down', 'back', 'front', 'servo', 'still']
        orientation = []
        axes = 3

        arm_threshold, keep_ind = 0.15, list(np.arange(1,4))#self.get_cumulative_threshold(data[:, 4 * np.arange(3)])
        keep_ind = list(chain(*[[j * 4 + off for off in range(1, 4)] for j in keep_ind]))
        data = data[:, keep_ind]
        rows, cols = data.shape

        axis_threshold = self.thresholds_dictionary['axis_threshold']
        proba_array = [0] * 8

        if cols > 0:
            motion_mag = sum([np.linalg.norm(data[i+1]-data[i]) for i in range(rows-1)])
            if motion_mag >= arm_threshold:
                delta = data[-1] - data[0]
                mag = np.linalg.norm(delta)

                for k in range(axes):
                    delta_in_axis = [j for (i, j) in enumerate(delta) if (i % axes == k)]
                    mag_contrib_in_axis = round(sum(np.square(delta_in_axis)) / float(mag ** 2), 2)

                    if mag_contrib_in_axis >= axis_threshold:
                        orient_indx = 1 - int(sum(delta_in_axis)>0)
                        dir = directions_list[k*2+orient_indx]
                        orientation.append(dir)

                        proba_index = k * 2 + orient_indx
                        proba_array[proba_index] = mag_contrib_in_axis

                direction = 'move ' + ' '.join(orientation)
            else:
                direction = 'still'
        else:
            print ('arm not seen...sending blind value instead')
            direction = ' blind'
        return direction, proba_array


