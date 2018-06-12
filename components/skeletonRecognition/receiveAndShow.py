import numpy as np
from scipy.signal import savgol_filter


class Pointing:
    def __init__(self, pointing_mode='screen'):
        if pointing_mode == 'screen':
            self.screen_mode = True
        elif pointing_mode == 'desk':
            self.screen_mode = False
        else:
            raise ValueError('Pointing mode is not recognized!\n Accepted: screen, desk\n Received: %s' % pointing_mode)

        if not self.screen_mode:
            # use this if in desk mode
            self.WRISTLEFT = 6  # JointType specified by kinect
            self.WRISTRIGHT = 10
            self.ELBOWLEFT = 5
            self.ELBOWRIGHT = 9
            self.joint_interest_coded = [self.WRISTLEFT, self.WRISTRIGHT, self.ELBOWLEFT, self.ELBOWRIGHT]
        else:
            # use this if in screen mode
            self.HANDTIPLEFT = 21  # JointType specified by kinect
            self.HANDTIPRIGHT = 23
            self.SHOULDERLEFT = 4
            self.SHOULDERRIGHT = 8
            self.joint_interest_coded = [self.HANDTIPLEFT, self.HANDTIPRIGHT, self.SHOULDERLEFT, self.SHOULDERRIGHT]

        self.joint_info = {i: None for i in self.joint_interest_coded}  # contains left/right wrists/elbows coordinates
        self.joint_info_buffer = {i: [] for i in self.joint_interest_coded}

        self.lpoint_buffer = []
        self.rpoint_buffer = []
        self.lpoint_tmp = (0.0, -0.6)
        self.rpoint_tmp = (0.0, -0.6)
        self.lpoint = (0.0, -0.6)  # inferred pointing coordinate on the table from left arm
        self.rpoint = (0.0, -0.6)  # inferred pointing coordinate on the table from right arm

        self.lpoint_var = (0, 0)  # variance of left point, sent to Brandeis
        self.rpoint_var = (0, 0)  # variance of right point, sent to Brandeis
        self.lpoint_stable = False  # whether left hand pointing is stable
        self.rpoint_stable = False  # whether right hand pointing is stable

    def get_pointing_main(self, src, is_smoothing_joint=True, is_smoothing_point=True):

        if not self._get_wrist_elbow(src):
            return

        if self.screen_mode:
            try:
                if is_smoothing_joint:
                    self._smoothing_joint(5, 2)
                    self._smoothing_joint_mean(5)
                self._get_pointing(True)  # True is coordinates on screen
                if is_smoothing_point:
                    pass
                    self._smoothing_point_mean(5)
                    self._smoothing_point(5, 2)
                self.lpoint = (self.lpoint_tmp[0] - 0.25, self.lpoint_tmp[1])
                self.rpoint = (self.rpoint_tmp[0] + 0.25, self.rpoint_tmp[1])
            except Exception as e:
                print(e)
        else:
            try:
                self._smoothing_joint_desk(3, 2)
                self._get_pointing(False)
                self._smoothing_point(3, 2)
                self.lpoint, self.rpoint = self.lpoint_tmp, self.rpoint_tmp
            except Exception as e:
                print(e)

        self.lpoint_var = np.std(self.lpoint_buffer, axis=0)
        self.rpoint_var = np.std(self.rpoint_buffer, axis=0)
        if np.any((np.amax(self.lpoint_buffer, axis=0) - np.amin(self.lpoint_buffer, axis=0)) > [0.005, 0.005]):
            self.lpoint_stable = False
        else:
            self.lpoint_stable = True
        if np.any((np.amax(self.rpoint_buffer, axis=0) - np.amin(self.rpoint_buffer, axis=0)) > [0.005, 0.005]):
            self.rpoint_stable = False
        else:
            self.rpoint_stable = True

    def _get_wrist_elbow(self, src):
        '''
        This function retrieves the coordinates for left/right wrists/elbows (4 sets of 3 values: x, y, z)
        @:param src: decoded frame retrieved from the decode_frame() function
        '''
        try:
            for i in range(25):
                if src[(i + 1) * 9] in self.joint_interest_coded:
                    self.joint_info[src[(i + 1) * 9]] = src[(i + 1) * 9 + 2: (i + 2) * 9 + 5]
            return True
        except IndexError:
            print('Not enough coordinates to unpack')
            return False

    def _smoothing_joint(self, window_length=5, polyorder=2):
        for k, v in list(self.joint_info_buffer.items()):
            if len(v) >= window_length:
                self.joint_info_buffer[k].pop(0)
                self.joint_info_buffer[k].append(self.joint_info[k])
                joint_smoothed = savgol_filter(self.joint_info_buffer[k], window_length, polyorder, axis=0).tolist()
                self.joint_info[k] = joint_smoothed[window_length // 2]
            else:
                self.joint_info_buffer[k].append(self.joint_info[k])

    def _smoothing_joint_mean(self, window_length=5):
        for k, v in list(self.joint_info_buffer.items()):
            if len(v) >= window_length:
                self.joint_info_buffer[k].pop(0)
                self.joint_info_buffer[k].append(self.joint_info[k])
                self.joint_info[k] = np.mean(self.joint_info_buffer[k], axis=0)
            else:
                self.joint_info_buffer[k].append(self.joint_info[k])

    def _smoothing_point(self, window_length=5, polyorder=2):
        '''
        Smoothing function for left and right pointing coordinates
        :param window_length:
        :param polyorder:
        :return:
        '''
        if len(self.lpoint_buffer) >= window_length:
            self.lpoint_buffer.pop(0)
            self.lpoint_buffer.append(self.lpoint_tmp)
            self.lpoint_buffer = savgol_filter(self.lpoint_buffer, window_length, polyorder, axis=0).tolist()
            self.lpoint_tmp = self.lpoint_buffer[int(window_length / 2)]
        else:
            self.lpoint_buffer.append(self.lpoint_tmp)

        if len(self.rpoint_buffer) >= window_length:
            self.rpoint_buffer.pop(0)
            self.rpoint_buffer.append(self.rpoint_tmp)
            self.rpoint_buffer = savgol_filter(self.rpoint_buffer, window_length, polyorder, axis=0).tolist()
            self.rpoint_tmp = self.rpoint_buffer[int(window_length / 2)]
        else:
            self.rpoint_buffer.append(self.rpoint_tmp)

    def _smoothing_point_mean(self, window_length=5):
        if len(self.lpoint_buffer) >= window_length:
            self.lpoint_buffer.pop(0)
            self.lpoint_buffer.append(self.lpoint_tmp)
            self.lpoint_tmp = np.mean(self.lpoint_buffer, axis=0)
        else:
            self.lpoint_buffer.append(self.lpoint_tmp)

        if len(self.rpoint_buffer) >= window_length:
            self.rpoint_buffer.pop(0)
            self.rpoint_buffer.append(self.rpoint_tmp)
            self.rpoint_tmp = np.mean(self.rpoint_buffer, axis=0)
        else:
            self.rpoint_buffer.append(self.rpoint_tmp)

    def _get_pointing(self, screen=True):
        if not screen:
            l_coord1 = self.joint_info[self.WRISTLEFT]
            r_coord1 = self.joint_info[self.WRISTRIGHT]
            l_coord2 = self.joint_info[self.ELBOWLEFT]
            r_coord2 = self.joint_info[self.ELBOWRIGHT]
        else:
            l_coord1 = self.joint_info[self.HANDTIPLEFT]
            r_coord1 = self.joint_info[self.HANDTIPRIGHT]
            l_coord2 = self.joint_info[self.SHOULDERLEFT]
            r_coord2 = self.joint_info[self.SHOULDERRIGHT]

        self.lpoint_tmp = self._calc_coordinates(l_coord1, l_coord2, screen)
        self.rpoint_tmp = self._calc_coordinates(r_coord1, r_coord2, screen)

    def _calc_coordinates(self, wrist, elbow, screen=True):
        if screen:
            '''
            Both wrist and elbow should contain (x,y,z) coordinates
            screen plane: z=0, ie pz = 0
            Line equation:
                (ex - px)/(ex - wx) = (ez - pz)/(ez - wz) = ez/(ez - wz)
                (ey - py)/(ey - wy) = (ez - pz)/(ez - wz) = ez/(ez - wz)
            so:
                px = ex - ez(ex-wx) / (ez-wz)
                py = ey - ez(ey-wy) / (ez-wz)
            '''
            if (elbow[2] - wrist[2]) == 0:
                return -np.inf, -np.inf
            screen_x = elbow[0] - elbow[2] * (elbow[0] - wrist[0]) / (elbow[2] - wrist[2])
            screen_y = elbow[1] - elbow[2] * (elbow[1] - wrist[1]) / (elbow[2] - wrist[2])

            return screen_x, screen_y
        else:

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
            if (elbow[1] - wrist[1]) == 0:
                return -np.inf, -np.inf
            table_y = -0.582
            table_x = wrist[0] - (wrist[1] - table_y) / (elbow[1] - wrist[1]) * (elbow[0] - wrist[0])
            table_z = wrist[2] - (wrist[1] - table_y) / (elbow[1] - wrist[1]) * (elbow[2] - wrist[2])

            return table_x, table_z

    def _smoothing_joint_desk(self, window_length=3, polyorder=2):
        for k, v in list(self.joint_info_buffer.items()):
            if len(v) >= window_length:
                self.joint_info_buffer[k].pop(0)
                self.joint_info_buffer[k].append(self.joint_info[k])
                self.joint_info_buffer[k] = \
                    savgol_filter(self.joint_info_buffer[k], window_length, polyorder, axis=0).tolist()
                self.joint_info[k] = np.mean(self.joint_info_buffer[k], axis=0)
            else:
                self.joint_info_buffer[k].append(self.joint_info[k])
