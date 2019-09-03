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
import queue
from skimage.transform import resize


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
        if left_mask:
            return True
        return False

    def right_mask_valid(self):
        if right_mask:
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
        self.cube_size = 396
        self.cube_sizeZ = 3000000
        self.fallback_size = 168
        self.fallback_value = 255
        self.depth_valid = False
        self.event = threading.Event()
        self.frame = None

    def get_joint_positions(self):
        return self.kinect.body_joints_to_depth_space(self.cbf.closest_body.joints)#JointType_HandRight

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

    def crop_frame(self, frame, joint_to_crop_around):
        x = int(joint_to_crop_around.x)
        y = int(joint_to_crop_around.y)
        z = self.df[y,x]
        x_start = int(x - (1500*self.fx)/(2*z))
        x_end = int(joint_to_crop_around.x + (1500*self.fx)/(2*z))
        y_start = int(joint_to_crop_around.y - (1500*self.fy)/(2*z))
        y_end = int(joint_to_crop_around.y + (1500*self.fy)/(2*z))
        return frame[max(0, y_start):min(frame.shape[0]-1, y_end), max(0, x_start):min(frame.shape[1]-1, x_end)]

    def segment(self, hand_pos):
        x_start = 0
        y_start = 0
        x_end = self.fallback_size
        y_end = self.fallback_size
        mask = np.zeros(self.df.shape)
        self.depth_valid = True
        
        x = int(hand_pos.x)
        y = int(hand_pos.y)
        
        if x < 0 or x >= mask.shape[1] or y < 0 or y >= mask.shape[0]:
            return mask
        z = self.df[y, x]
        
        if z == 0:
            self.depth_valid = False
            
        if self.depth_valid:
            x_start = int(x - (self.cube_size*self.fx)/(2*z))
            x_end = int(x + (self.cube_size*self.fx)/(2*z))

            y_start = int(y - (self.cube_size*self.fy)/(2*z))
            y_end = int(y + (self.cube_size*self.fy)/(2*z))
            
        mask[max(y_start, 0):min(y_end, mask.shape[0]-1), max(x_start, 0):min(x_end, mask.shape[1]-1)] = 1
        
        return mask

    def threshold(self, joint_points):
        x = int(joint_points[JointType_SpineBase].x)
        y = int(joint_points[JointType_SpineBase].y)
        try:
            pos_z = self.df[y,x]
        except:
            return

        z_start = pos_z - self.cube_sizeZ / 2.0
        z_end   = pos_z + self.cube_sizeZ / 2.0

        if not self.depth_valid:
            self.df[:,:] = self.fallback_value
            return
        self.df[self.df == 0] = z_end
        self.df[self.df > z_end] = z_end
        self.df[self.df <= z_start] = z_start

    def run(self):
        while True:
            if self.kinect.has_new_body_frame():
                self.cbf = Closest_Body_Frame(self.kinect.get_last_body_frame(), self.engage_min, self.engage_max)

            if self.cbf and self.cbf.check_for_bodies():
                if self.kinect.has_new_depth_frame():
                    self.df = self.kinect.get_last_depth_frame()
                    timestamp = time.time()
                    self.df = np.reshape(self.df, (424, 512))
                    joint_points = self.get_joint_positions()
                    self.threshold(joint_points)
                    left_mask = self.segment(joint_points[JointType_HandLeft])
                    right_mask = self.segment(joint_points[JointType_HandRight])

                    cropped_df = self.crop_frame(self.df, joint_points[JointType_SpineMid])
                    cropped_left_mask = self.crop_frame(left_mask, joint_points[JointType_SpineMid])
                    cropped_right_mask = self.crop_frame(right_mask, joint_points[JointType_SpineMid])

                    #self.frame = Preprocessed_Frame(self.df,
                    #                                left_mask,
                    #                                right_mask,
                    #                                timestamp,
                    #                                self.cbf.engaged)

                    self.frame = Preprocessed_Frame(resize(cropped_df, (424, 512), preserve_range=True),
                                                    resize(cropped_left_mask, (424, 512), preserve_range=True),
                                                    resize(cropped_right_mask, (424,512), preserve_range=True),
                                                    timestamp,
                                                    self.cbf.engaged)
                    self.event.set()


def preprocess_image(image):
    image = np.clip(image, 400, 1800)
    image = np.multiply(image, 1.0)
    image -= np.min(image)
    image /= np.max(image+0.00001)
    image = np.multiply(image, 255)
    return image.astype("uint8")


if __name__=='__main__':
    class_instance = CANet_Preprocessor(0.0, 10.0)
    class_instance.start()
    total_time = 0
    frame_count = 0
    start = time.time()

    while True:
        if class_instance.event.is_set():
            frame = class_instance.get_frame()
            frame.depth_frame = preprocess_image(frame.depth_frame)
            cv2.imshow("frame", frame.depth_frame)
            cv2.imshow("left mask", np.multiply(frame.left_mask, frame.depth_frame))
            cv2.imshow("right mask", np.multiply(frame.right_mask, frame.depth_frame))

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

