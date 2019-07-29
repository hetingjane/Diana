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
        self.frame = frame
        self.left_mask = left_mask
        self.right_mask = right_mask
        self.timestamp = timestamp
        self.engagement = engagement

    def masks_valid(self):
        return self.left_mask_valid and self.right_mask_valid

    def left_mask_valid(self):
        if left_mask:
            return True
        return False

    def right_mask_valid(self):
        if right_mask:
            return True
        return False

class CANet_Preprocessor(object):
    def __init__(self, engage_min, engage_max):
        self.kinect = PyKinectRuntime.PyKinectRuntime(PyKinectV2.FrameSourceTypes_Depth | PyKinectV2.FrameSourceTypes_Body)
        self.engage_min = engage_min
        self.engage_max = engage_max
        self.cbf = None
        self.df = None
        self.fx = 288.03
        self.fy = 287.07
        self.cube_size = 396
        self.fallback_size = 200
        self.frames = []

    def get_hand_positions(self):
        joint_points = self.kinect.body_joints_to_depth_space(self.cbf.closest_body.joints)
        return joint_points[JointType_HandRight], joint_points[JointType_HandLeft]

    def new_depth_frame_arrived(self):
        return self.kinect.has_new_depth_frame()

    def new_body_frame_arrived(self):
        return self.kinect.has_new_body_frame()

    def body_engaged(self):
        if self.cbf:
            return self.cbf.engaged
        return False
    
    def segment(self, hand_pos):
        x_start = 0
        y_start = 0
        x_end = self.fallback_size
        y_end = self.fallback_size
        mask = np.zeros(self.df.shape)
        depth_valid = True
        
        x = int(hand_pos.x)
        y = int(hand_pos.y)
        
        if x < 0 or x >= mask.shape[1] or y < 0 or y >= mask.shape[0]:
            return mask
        z = self.df[y, x]
        
        if z == 0:
            depth_valid = False
            
        if depth_valid:
            x_start = int(x - (self.cube_size*self.fx)/(2*z))
            x_end = int(x + (self.cube_size*self.fx)/(2*z))

            y_start = int(y - (self.cube_size*self.fy)/(2*z))
            y_end = int(y + (self.cube_size*self.fy)/(2*z))
            
        mask[max(y_start, 0):min(y_end, mask.shape[0]-1), max(x_start, 0):min(x_end, mask.shape[1]-1)] = 255
        
        return mask

    def get_frames(self):
        if self.kinect.has_new_body_frame():
            self.cbf = Closest_Body_Frame(self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)

        if self.cbf and self.cbf.check_for_bodies():
            if self.kinect.has_new_depth_frame():
                self.df = self.kinect.get_last_depth_frame()
                timestamp = time.time()
                self.df = np.resize(self.df, (424, 512))
                right_pos, left_pos = self.get_hand_positions()
                left_mask = self.segment(left_pos)
                right_mask = self.segment(right_pos)

                return Preprocessed_Frame(self.df, left_mask, right_mask, timestamp, self.cbf.engaged)











