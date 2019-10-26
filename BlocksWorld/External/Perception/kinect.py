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
    def __init__(self, engage_min, engage_max, modality):
        self.modality = modality
        if self.modality == 'skeleton':
            self.kinect = PyKinectRuntime.PyKinectRuntime(PyKinectV2.FrameSourceTypes_Body)
        elif self.modality == 'depth':
            self.kinect = PyKinectRuntime.PyKinectRuntime(PyKinectV2.FrameSourceTypes_Depth | PyKinectV2.FrameSourceTypes_Body)

        self.latest_frames = None

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

        self._bodies = None

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
            
            spine_base_x = joints[JointType_SpineBase].x
            spine_base_y = joints[JointType_SpineBase].y
            spine_base_z = np.mean(
                hand_arr[int(spine_base_y) - z_space:int(spine_base_y) + z_space, 
                         int(spine_base_x) - z_space:int(spine_base_x) + z_space])

            
            segmented_frames.append((segmented, 
                y, z, spine_base_y, spine_base_z))

        return segmented_frames

    def get(self):
        if not self.kinect.has_new_body_frame():
            return self.latest_frames

        closest_body = Closest_Body_Frame(self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)

        if not closest_body or not closest_body.check_for_bodies():
            return self.latest_frames

        if self.modality == 'depth':
            joint_points = self.kinect.body_joints_to_depth_space(closest_body.closest_body.joints)
            frame = self.kinect.get_last_depth_frame()
            self.latest_frames = self._segment(frame, joint_points, [JointType_HandLeft, JointType_HandRight])
            return self.latest_frames
        elif self.modality == 'skeleton':
            return Skeleton_Frame(closest_body.closest_body.joints, closest_body.engaged)


class Skeleton_Frame(object):
    def __init__(self, joints, engagement):
        self.joints = joints
        self.engagement = engagement
        self.skeleton_frame = self.get_skeleton_data()

    def get_skeleton_data(self):
        spine_base = self.joints[JointType_SpineBase].Position
        spine_mid = self.joints[JointType_SpineMid].Position
        neck = self.joints[JointType_Neck].Position
        head = self.joints[JointType_Head].Position
        shoulder_left = self.joints[JointType_ShoulderLeft].Position
        elbow_left = self.joints[JointType_ElbowLeft].Position
        wrist_left = self.joints[JointType_WristLeft].Position
        hand_left = self.joints[JointType_HandLeft].Position
        shoulder_right = self.joints[JointType_ShoulderRight].Position
        elbow_right = self.joints[JointType_ElbowRight].Position
        wrist_right = self.joints[JointType_WristRight].Position
        hand_right = self.joints[JointType_HandRight].Position
        spine_shoulder = self.joints[JointType_SpineShoulder].Position
        hand_tip_left = self.joints[JointType_HandTipLeft].Position
        thumb_left = self.joints[JointType_ThumbLeft].Position
        hand_tip_right = self.joints[JointType_HandTipRight].Position
        thumb_right = self.joints[JointType_ThumbRight].Position

        skeleton_frame = [spine_base.x, spine_base.y, spine_base.z, spine_mid.x, spine_mid.y, spine_mid.z, neck.x,
                          neck.y, neck.z, head.x, head.y, head.z,
                          shoulder_left.x, shoulder_left.y, shoulder_left.z, elbow_left.x, elbow_left.y, elbow_left.z,
                          wrist_left.x, wrist_left.y, wrist_left.z,
                          hand_left.x, hand_left.y, hand_left.z, shoulder_right.x, shoulder_right.y, shoulder_right.z,
                          elbow_right.x, elbow_right.y, elbow_right.z,
                          wrist_right.x, wrist_right.y, wrist_right.z, hand_right.x, hand_right.y, hand_right.z,
                          spine_shoulder.x, spine_shoulder.y, spine_shoulder.z,
                          hand_tip_left.x, hand_tip_left.y, hand_tip_left.z, thumb_left.x, thumb_left.y, thumb_left.z,
                          hand_tip_right.x, hand_tip_right.y, hand_tip_right.z,
                          thumb_right.x, thumb_right.y, thumb_right.z]
        return skeleton_frame
