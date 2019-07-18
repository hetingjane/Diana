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


class Closest_Body_Frame(object):
    def __init__(self, body_frame, engage_min, engage_max):
        self.body_frame = body_frame
        self.engage_min = engage_min
        self.engage_max = engage_max
        self.engaged = False
        
        tracked_bodies = {}
        for body in self.body_frame.bodies:
            if body.is_tracked and self.is_engaged(body):
                    tracked_bodies[self.distance_from_kinect(body)] = body
                    
        try:
            self.closest_body = tracked_bodies[min(tracked_bodies.keys())] 
            self.engaged = self.is_engaged(self.closest_body)
        except:
            self.closest_body = None
       
    def distance_from_kinect(self, body):
        pos = body.joints[JointType_SpineBase].Position
        return pos.x**2 + pos.y**2 + pos.z**2
    
    def is_engaged(self, body):
        if body is None:
            return False
        dist = self.distance_from_kinect(body)
        return self.engage_min < dist and self.engage_max > dist
                    
                
    

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
        
    
    def get_hand_positions(self, cbf):
        joint_points = self.kinect.body_joints_to_depth_space(cbf.closest_body.joints)
        
        return joint_points[JointType_HandRight], joint_points[JointType_HandLeft]
    
    def segment(self, hand_pos):
        x_start = 0
        y_start = 0
        x_end = self.cube_size
        y_end = self.cube_size
        mask = np.zeros((self.df.shape))
        depth_valid = True
        
        x = int(hand_pos.x)
        y = int(hand_pos.y)
        
        if x < 0 or x >= mask.shape[1] or y < 0 or y >= mask.shape[0]:
            return mask
        z = self.df[y,x]
        
        if z == 0:
            depth_valid = False
            
        if depth_valid:
            x_start = int((((x * z / self.fx) - (self.cube_size / 2.0)) / z) * self.fx)
            x_end = int((((x * z / self.fx) + (self.cube_size / 2.0)) / z) * self.fx)

            y_start = int((((y * z / self.fy) - (self.cube_size / 2.0)) / z) * self.fy)
            y_end = int((((y * z / self.fy) + (self.cube_size / 2.0)) / z) * self.fy)
            
        mask[max(y_start, 0):min(y_end, mask.shape[0]-1), max(x_start, 0):min(x_end, mask.shape[1]-1)] = 255
        
        return mask
                            
        
    def run(self):
        while True:
            if self.kinect.has_new_body_frame():
                self.cbf = Closest_Body_Frame( self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)
                
            if self.kinect.has_new_depth_frame():
                self.df = self.kinect.get_last_depth_frame()
                self.df = np.resize(self.df,(424, 512))
                
            if self.cbf and self.cbf.engaged:
                right_pos, left_pos = self.get_hand_positions(self.cbf)
                left_mask = self.segment(left_pos)
                right_mask = self.segment(right_pos)
                #self.df = 255*(self.df  - self.df.min())/(self.df.max() - self.df.min())
                
                cv2.imshow("frame", self.df)
                cv2.imshow("left mask", np.multiply(left_mask, self.df))#.astype(np.uint16))
                cv2.imshow("right mask", np.multiply(right_mask, self.df))#.astype(np.uint16))
                
                
                
                
                #cv2.imshow("frame", df)
                if cv2.waitKey(10) & 0xFF == ord('q'):
                    break
                
                
            
                
            
            
class_instance = CANet_Preprocessor(0.0, 10.0)
class_instance.run()