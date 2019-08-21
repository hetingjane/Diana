import time
import numpy as np
from skimage.transform import resize
from pykinect2 import PyKinectV2
from pykinect2.PyKinectV2 import *
from pykinect2 import PyKinectRuntime


class Closest_Body_Frame(object):
    def __init__(self, body_frame, engage_min, engage_max):
        self.body_frame = body_frame
        self.engage_min = engage_min
        self.engage_max = engage_max
        self.engaged = False
        self.bodies_tracked = 0
        self.closest_body = None

        tracked_bodies = {}
        for body in self.body_frame.bodies:
            if body.is_tracked:
                tracked_bodies[self.distance_from_kinect(body)] = body
                self.bodies_tracked += 1

        if self.bodies_tracked > 0:
            self.closest_body = tracked_bodies[min(tracked_bodies.keys())]
            self.engaged = self.is_engaged(self.closest_body)

    def distance_from_kinect(self, body):
        pos = body.joints[JointType_SpineBase].Position
        return pos.x ** 2 + pos.y ** 2 + pos.z ** 2

    def is_engaged(self, body):
        if body is None:
            return False
        dist = self.distance_from_kinect(body)
        return self.engage_min < dist and self.engage_max > dist

    def check_for_bodies(self):
        if self.bodies_tracked > 0:
            return True
        return False

class Kinect:
    def __init__(self, segment_size, engage_min, engage_max):
        self.segment_size = segment_size
        self.kinect = PyKinectRuntime.PyKinectRuntime(PyKinectV2.FrameSourceTypes_Depth | PyKinectV2.FrameSourceTypes_Body)
        self.latest_frames = None

        self.active_arm_threshold = 0.16
        self.pixel_intensity_threshold = 0.4
        self.engage_min = engage_min
        self.engage_max = engage_max
        self.sensor_height = 424
        self.sensor_width = 512
        self.cbf = None
        self.df = None
        self.fx = 288.03
        self.fy = 287.07
        self.cube_size = 396
        self.fallback_size = 200

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

    def _segment(self, depth_data, joints, joints_to_segment):
        x_start = 0
        y_start = 0
        x_end = self.fallback_size
        y_end = self.fallback_size
        depth_valid = True

        hand_arr = np.array(depth_data, dtype=np.float32).reshape((self.sensor_height, self.sensor_width))
        segmented_frames = []
        for joint in joints_to_segment:
            x = joints[joint].x
            y = joints[joint].y

            if x < 0 or x >= self.sensor_width or y < 0 or y >=self.sensor_height:
                continue

            z = hand_arr[int(x), int(y)]

            if z == 0:
                continue

            segmented = np.copy(hand_arr)
            segmented -= z
            segmented /= 150
            segmented = np.clip(segmented, -1, 1)

            x_start = int(x - (self.cube_size * self.fx) / (2 * z))
            x_start = max(x_start, 0)
            x_end = int(x + (self.cube_size * self.fx) / (2 * z))
            x_end = min(x_end, self.sensor_width - 1)

            y_start = int(y - (self.cube_size * self.fy) / (2 * z))
            y_start = max(y_start, 0)
            y_end = int(y + (self.cube_size * self.fy) / (2 * z))
            y_end = min(y_end, self.sensor_height - 1)

            segmented = segmented[y_start:y_end, x_start, x_end]


            segmented = np.pad(segmented)

        hand_arr = resize(hand_arr, (168, 168))
        hand_arr = hand_arr[20:-20, 20:-20]
        hand_arr = hand_arr.reshape((1, 128, 128, 1))

        return mask

    def get(self):
        if not self.kinect.has_new_body_frame():
            return self.latest_frames

        closest_body = Closest_Body_Frame(self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)

        if not closest_body or not closest_body.check_for_bodies():
            return self.latest_frames

        joint_points = self.kinect.body_joints_to_depth_space(closest_body.closest_body.joints)
        frame = self.kinect.get_last_depth_frame()

        # segment and preprocess depth frame around hands
        self.latest_frames = self._segment(frame, joint_points, [JointType_HandLeft, JointType_HandRight])
        # this is where we could add a routine for queuing frames or something, right now is LILO

        return self.latest_frames



