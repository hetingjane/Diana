import time
import cv2
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
        z_space = 3

        hand_arr = np.array(depth_data, dtype=np.float32).reshape((self.sensor_height, self.sensor_width))
        segmented_frames = []
        for joint in joints_to_segment:
            x = joints[joint].x
            y = joints[joint].y

            if x < z_space or x >= self.sensor_width - z_space or y < z_space or y >=self.sensor_height - z_space:
                return None

            z = np.mean(hand_arr[int(y) - z_space:int(y) + z_space, int(x) - z_space:int(x) + z_space])

            if z == 0:
                return None

            pad_left, pad_right, pad_top, pad_bottom = 0, 0, 0, 0

            x_start = int(x - (self.cube_size * self.fx) / (2 * z))
            if x_start < 0:
                pad_left = -x_start
                x_start = 0
            x_end = int(x + (self.cube_size * self.fx) / (2 * z))

            if x_end > self.sensor_width:
                pad_right = x_end - self.sensor_width - 1
                x_end = self.sensor_width

            y_start = int(y - (self.cube_size * self.fy) / (2 * z))
            if y_start < 0:
                pad_top = -y_start
                y_start = 0

            y_end = int(y + (self.cube_size * self.fy) / (2 * z))
            y_end = min(y_end, self.sensor_height - 1)
            if y_end > self.sensor_height:
                pad_bottom = y_end - self.sensor_height
                y_end = 0

            segmented = np.copy(hand_arr)
            segmented = segmented[y_start:y_end, x_start:x_end]
            segmented = np.clip(segmented, z-150, z+150)
            segmented -= z
            segmented /= 150
            # switch all black (-1.0) to white (1.0)
            segmented = np.where(segmented == -1, 1, segmented)

            # when the joint is at/beyond the boundary of the depth image
            segmented = np.pad(segmented, ((pad_top, pad_bottom), (pad_left, pad_right)), 'maximum')

            segmented = resize(segmented, (168, 168))
            segmented = segmented[20:-20, 20:-20]
            segmented = segmented.reshape((1, 128, 128, 1))
            segmented_frames.append(segmented)

        return segmented_frames

    def get(self):
        if not self.kinect.has_new_body_frame():
            return self.latest_frames

        closest_body = Closest_Body_Frame(self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)

        if not closest_body or not closest_body.check_for_bodies():
            return self.latest_frames

        joint_points = self.kinect.body_joints_to_depth_space(closest_body.closest_body.joints)
        frame = self.kinect.get_last_depth_frame()

        #test_data = np.array(frame, dtype=np.uint8).reshape((self.sensor_height, self.sensor_width))
        #cv2.fastNlMeansDenoising(test_data, test_data)
        #test_data += np.abs(np.min(test_data))
        #test_data /= np.max(test_data)
        #cv2.imshow("raw", test_data)

        # segment and preprocess depth frame around hands
        self.latest_frames = self._segment(frame, joint_points, [JointType_HandLeft, JointType_HandRight])
        # this is where we could add a routine for queuing frames or something, right now is LILO

        return self.latest_frames



