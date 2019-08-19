import time
import numpy as np
from skimage.transform import resize
from pykinect2 import PyKinectV2
from pykinect2.PyKinectV2 import *
from pykinect2 import PyKinectRuntime


class Kinect:
    def __init__(self, segment_size):
        self.segment_size = segment_size
        self.kinect = PyKinectRuntime.PyKinectRuntime(PyKinectV2.FrameSourceTypes_Depth | PyKinectV2.FrameSourceTypes_Body)
        self.latest_frames = None

        self.active_arm_threshold = 0.16
        self.pixel_intensity_threshold = 0.4

    @staticmethod
    def _threshold(value, minimum, maximum):
        if value < minimum:
            return minimum
        if value > maximum:
            return maximum
        return value

    def _is_bright(self, depth_frame):
        #hand_arr = np.squeeze(depth_frame)

        bright_corners = np.sum([depth_frame[i, j] > self.pixel_intensity_threshold for i in [0, -1] for j in [0, -1]])
        return bright_corners >= 3

    def _is_gesture(self, hand, frame_pieces, posx, posy):
        """we don't want to process frames when user is not engaged or hands are resting (at/near spine base y or z)"""

        if len(frame_pieces) != 0:
            spine_base_ind = 0
            if hand == "RH":
                hand_ind = 11
            else:
                hand_ind = 7
            spine_base_y = frame_pieces[spine_base_ind * 9 + 8]
            spine_base_z = frame_pieces[spine_base_ind * 9 + 9]

            hand_y = frame_pieces[hand_ind * 9 + 8]
            hand_z = frame_pieces[hand_ind * 9 + 9]
        else:
            return False

        if (posx == -1 and posy == -1) or \
                ((hand_y < spine_base_y) and ((spine_base_z - hand_z) < self.active_arm_threshold)) or \
                (spine_base_z < hand_z):
            return False
        return True

    def _preprocess_hand_arr(self, depth_data, posx, posy, height, width):
        hand_arr = np.array(depth_data, dtype=np.float32).reshape((height, width))
        posz = hand_arr[int(posx), int(posy)]
        hand_arr -= posz
        hand_arr /= 150
        hand_arr = np.clip(hand_arr, -1, 1)
        hand_arr = resize(hand_arr, (168, 168))
        hand_arr = hand_arr[20:-20, 20:-20]
        hand_arr = hand_arr.reshape((1, 128, 128, 1))

        return hand_arr

    def _segment(self, joint, joint_points, frame):
        point = joint_points[joint]
        height = frame.shape[0]
        width = frame.shape[1]
        x_min = self._threshold(point.x - self.segment_size // 2, 0, width)
        x_max = self._threshold(point.x + self.segment_size // 2, 0, width)
        y_min = self._threshold(point.y - self.segment_size // 2, 0, height)
        y_max = self._threshold(point.y - self.segment_size // 2, 0, height)
        seg_frame = frame[y_min:y_max, x_min:x_max, :]
        seg_frame = self._preprocess_hand_arr(seg_frame, point.x, point.y, self.segment_size, self.segment_size)
        return seg_frame

    def _get_closest_body(self):
        closest_head_z = float('inf')
        closest_body = None
        for body in self.kinect.get_last_body_frame().bodies():
            if body.joints[JointType_Head].z < closest_head_z:
                closest_body = body
        return closest_body

    def get(self):
        if not self.kinect.has_new_body_frame():
            time.sleep(1/120)
            return self.latest_frames

        closest_body = self._get_closest_body()

        joint_points = self.kinect.body_joints_to_depth_space(closest_body.joints)
        frame = self.kinect.get_last_depth_frame()

        # segment depth frame around hands
        # TODO base image size on Z distance, then up/downscale to standard size
        LH_frame = self._segment(JointType_HandLeft, joint_points, frame)
        RH_frame = self._segment(JointType_HandRight, joint_points, frame)

        # this is where we could add a routine for queuing frames or something, right now is LILO
        self.latest_frames = LH_frame, RH_frame

        return self.latest_frames



