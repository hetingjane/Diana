# -*- coding: utf-8 -*-
"""
Created on Thu Jul 18 11:07:13 2019

@author: mrdra
"""

from pykinect2 import PyKinectV2
from pykinect2.PyKinectV2 import *
from pykinect2 import PyKinectRuntime

import ctypes
import cv2
import numpy as np
import _ctypes
import sys
import time
import threading
from collections import namedtuple

joint_3d = namedtuple("joint_3d", "x y z")

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
        return pos.x**2 + pos.y**2 + pos.z**2
    
    def is_engaged(self, body):
        if body is None:
            return False
        dist = self.distance_from_kinect(body)
        return self.engage_min < dist and self.engage_max > dist

    def check_for_bodies(self):
        if self.bodies_tracked > 0:
            return True
        return False

class Preprocessed_Frame(object):
    def __init__(self, frame, left_mask, right_mask, timestamp, engagement):
        self.depth_frame = frame
        self.left_mask = left_mask
        self.right_mask = right_mask
        self.timestamp = timestamp
        self.engagement = engagement

    def masks_valid(self):
        return self.left_mask_valid and self.right_mask_valid

    def left_mask_valid(self):
        if self.left_mask:
            return True
        return False

    def right_mask_valid(self):
        if self.right_mask:
            return True
        return False

class CANet_Preprocessor(threading.Thread):
    def __init__(self, engage_min, engage_max):
        threading.Thread.__init__(self)
        self.kinect = PyKinectRuntime.PyKinectRuntime(PyKinectV2.FrameSourceTypes_Depth | PyKinectV2.FrameSourceTypes_Body)
        self.engage_min = engage_min
        self.engage_max = engage_max
        self.cbf = None
        self.df = None
        self.fx = 288.03
        self.fy = 287.07
        self.sensor_height = 424
        self.sensor_width = 512
        self.z_space = 3
        self.hand_cube_size = 300
        self.body_cube_size = 1500
        self.cube_sizeZ = 3000000
        self.fallback_size = 168
        self.fallback_value = 255
        self.depth_valid = False
        self.event = threading.Event()
        self.frame = None

    def new_depth_frame_arrived(self):
        return self.kinect.has_new_depth_frame()

    def new_body_frame_arrived(self):
        return self.kinect.has_new_body_frame()

    def get_frame(self):
        self.event.clear()
        return self.frame

    def event_set(self):
        return self.event.is_set()

    def body_engaged(self):
        if self.cbf:
            return self.cbf.engaged
        return False

    def get_3d_joint_positions(self):
        joint_points = self.kinect.body_joints_to_depth_space(self.cbf.closest_body.joints)
        joint_points_3d = {}
        for joint in range(joint_points.shape[0]):
            try:
                x = int(joint_points[joint].x)
                y = int(joint_points[joint].y)
            except:
                x = self.sensor_width
                y = self.sensor_height
            if (self.z_space <= x < self.sensor_width - self.z_space) and (self.z_space <= y < self.sensor_height - self.z_space):
                z = np.mean(self.df[y - self.z_space:y + self.z_space, x - self.z_space:x + self.z_space])
                joint_points_3d[joint] = joint_3d(x, y, z)
        return joint_points_3d

    def preprocess_frame(self, joint_points):
        potential_joints = [JointType_SpineBase, JointType_SpineMid, JointType_SpineShoulder]
        pot_z = []

        self.df = np.array(self.df, dtype=np.float32).reshape((self.sensor_height, self.sensor_width))
        for joint in potential_joints:
            if joint in joint_points.keys():
                pot_z.append(joint_points[joint].z)

        if len(pot_z) == 0 or np.max(pot_z) == 0:
            self.depth_valid = False
            z = (np.max(self.df) + np.min(self.df))/2
        else:
            z = np.max(pot_z) + 100

        self.df = np.clip(self.df, z - 600, z + 600)
        self.df -= z
        self.df /= 600
        # switch all black (-1.0) to white (1.0)
        self.df = np.where(self.df == -1, 1, self.df)

    def crop_frame(self, frame, x_start, x_end, y_start, y_end):
        return frame[max(0, y_start):min(frame.shape[0]-1, y_end), max(0, x_start):min(frame.shape[1]-1, x_end)]

    def segment(self, joint_points, joint, cube_size):
        x_start = 0
        y_start = 0
        x_end = self.sensor_width
        y_end = self.sensor_height
        self.depth_valid = True

        if joint in joint_points:
            x = joint_points[joint].x
            y = joint_points[joint].y
            z = joint_points[joint].z
        
            if z == 0:
                self.depth_valid = False
            
            if self.depth_valid:
                x_start = int(x - (cube_size*self.fx)/(2*z))
                x_end = int(x + (cube_size*self.fx)/(2*z))

                y_start = int(y - (cube_size*self.fy)/(2*z))
                y_end = int(y + (cube_size*self.fy)/(2*z))

        return x_start, x_end, y_start, y_end

    def gen_mask(self, joint_points, joint, cube_size):
        mask = np.zeros(self.df.shape, dtype=np.float32)
        x_start, x_end, y_start, y_end = self.segment(joint_points, joint, cube_size)
        mask[max(y_start, 0):min(y_end, mask.shape[0]-1), max(x_start, 0):min(x_end, mask.shape[1]-1)] = 1
        return mask

    def run(self):
        while True:
            if self.kinect.has_new_body_frame():
                self.cbf = Closest_Body_Frame(self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)

            if self.cbf and self.cbf.check_for_bodies():
                if self.kinect.has_new_depth_frame():
                    self.df = self.kinect.get_last_depth_frame().reshape((424, 512))
                    timestamp = time.time()
                    joint_points = self.get_3d_joint_positions()
                    left_mask = self.gen_mask(joint_points, JointType_HandLeft, self.hand_cube_size)
                    right_mask = self.gen_mask(joint_points, JointType_HandRight, self.hand_cube_size)

                    crop_dimensions = self.segment(joint_points, JointType_SpineMid, self.body_cube_size)
                    self.preprocess_frame(joint_points)
                    self.df = self.crop_frame(self.df, *crop_dimensions)
                    left_mask = self.crop_frame(left_mask, *crop_dimensions)
                    right_mask = self.crop_frame(right_mask, *crop_dimensions)

                    self.df = cv2.resize(self.df, (self.sensor_width, self.sensor_height), interpolation=cv2.INTER_CUBIC)#, preserve_range=True)
                    left_mask = cv2.resize(left_mask, (self.sensor_width, self.sensor_height), interpolation=cv2.INTER_CUBIC)#, preserve_range=True)
                    right_mask = cv2.resize(right_mask, (self.sensor_width, self.sensor_height), interpolation=cv2.INTER_CUBIC)#, preserve_range=True)


                    self.frame = Preprocessed_Frame(self.df,
                                                    left_mask,
                                                    right_mask,
                                                    timestamp,
                                                    self.cbf.engaged)
                    self.event.set()




if __name__=='__main__':
    class_instance = CANet_Preprocessor(0.0, 10.0)
    class_instance.start()
    total_time = 0
    frame_count = 0
    start = time.time()

    while True:
        if class_instance.event.is_set():
            frame = class_instance.get_frame()
            cv2.imshow("frame", frame.depth_frame)
            cv2.imshow("left mask", np.multiply(frame.left_mask, frame.depth_frame))
            cv2.imshow("right mask", np.multiply(frame.right_mask, frame.depth_frame))

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

