import threading
import numpy as np
import matplotlib.pyplot as plt
import pickle
import queue
import os
import time

from components.handRecognition.depth_client import is_gesture, is_bright


class OneShotWorker(threading.Thread):
    def __init__(self, hand_type, forest_status, event_vars, one_shot_queue,
                 global_lock, is_flipped, blacklist, is_test=False):
        threading.Thread.__init__(self)
        self.hand_type = hand_type
        self.forest_status = forest_status
        self.event_vars = event_vars
        self.one_shot_queue = one_shot_queue
        self.new_gesture_index = 32  # the first new gesture, increment when a new gesture is learned
        self.global_lock = global_lock
        self.is_test = is_test  # whether it is testing; should save reference images if is_test

        # Find out the corresponding kinect v2 joint index of the hand
        if self.hand_type == 'RH':
            self.palm_ind = 11
        elif self.hand_type == 'LH':
            self.palm_ind = 7
        # the start and end position of palm center coordinate in decoded body frame
        self.palm_coordinates_ind_start = self.palm_ind*9 + 7
        self.palm_coordinates_ind_end = self.palm_ind*9 + 10
        self.spine_base_ind = 0
        self.spine_base_Y_ind = self.spine_base_ind * 9 + 8
        self.spine_base_Z_ind = self.spine_base_ind * 9 + 9
        self.spine_mid_ind = 1
        self.spine_mid_Y_ind = self.spine_mid_ind * 9 + 8

        self.active_arm_threshold = 0.16

        self.forest = None  # random forest instance
        self.receiving_frames = False  # Only start to receive frames when a signal is sent from kinect server
        self.skip_frame = 0  # skip some frames to maximize variance in learning input, use this variable to keep track
        self.buffer_length = 30  # determines the number of frames in the buffer (3 seconds of gapped frames)
        self.moving_variance_threshold = 0.001  # used in self._can_start(), starts learning when the arm stops moving
        self.continous_no_gesture_frame_count = 0
        self.no_action_threshold = 120  # if there are no gestures for continous 5 seconds, do not learn for this hand
        self.ref_frames = []  # a buffer list of frames to learn
        self.palm_centers = []  # a buffer list of palm center coordinates

        self.is_flipped = is_flipped

    def run(self):
        while True:
            if self.event_vars.load_forest_event.is_set():
                self.load_forest()
            try:
                feature, skeleton_arr, start_learn, probs, frame = self.one_shot_queue.get(block=True, timeout=2)
                self.add_frame(feature, skeleton_arr, start_learn, probs, frame)
            except queue.Empty:
                pass

    # TODO keep running list of probabilities also
    # when ready to save new feature, check average vector's argmax. If this belongs to a blacklisted gesture and avg probability is above a threshold (0.7?), reject the gesture
    # also add other failures (timeout/low, revise current to "other", and think about how to handle "move")

    def add_frame(self, feature, skeleton_arr, start_learn, probs, depth_frame):
        """
        When learning starts, hand depth array and body skeleton array needs to used to extract necessary information
        and stored for further processing.
        :param skeleton_arr: body skeleton array from self.decode_frame_skeleton()
        :param start_learn: A signal from kinect to indicate whether the learning initiates
        :return: None
        """
        if start_learn:
            if self.receiving_frames:
                raise ValueError("Another learning signal is received before previous learning is finished!")
            self.receiving_frames = True
        if not self.receiving_frames:
            return
        if is_gesture(self.hand_type, skeleton_arr, 0, 0) and is_bright(depth_frame):
            print('is gesture and is bright')
            if not self.skip_frame:
                self.ref_frames.append(feature)
                self.palm_centers.append(skeleton_arr[self.palm_coordinates_ind_start:self.palm_coordinates_ind_end])

                # print(self.receiving_frames, len(self.palm_centers))
                if len(self.palm_centers) > self.buffer_length:
                    self.ref_frames.pop(0)
                    self.palm_centers.pop(0)

                    # Assumes the start time of learning is when the hand stops moving. This is determined by the palm
                    # center buffer variance, which needs to be smaller than certain threshold.
                    if self._palm_center_buffer_variance() < self.moving_variance_threshold:
                        # Start to learn, stop receiving frames
                        self.receiving_frames = False
                        # Get feature vectors
                        new_features = []
                        for i, ref_feature in enumerate(self.ref_frames):
                            new_features.append(ref_feature)
                        # learning finished, reset variables
                        self.ref_frames = []
                        self.palm_centers = []
                        # add reference features to forest
                        self.global_lock.acquire()
                        print('ADDING...')
                        self.forest_status.is_fresh = False
                        self.forest.add_new(new_features, [self.new_gesture_index] * len(new_features))
                        print('ADDING FINISHED...')
                        self.forest_status.is_ready = True
                        self.new_gesture_index += 1
                        self.event_vars.learn_complete_event.set()
                        self.global_lock.release()
                    else:
                        pass#print(self.hand_type, 'waiting for palm to stabilize')

        else:
            """
            In current frame, the hand is still next to body. Proceed without processing and reset buffer
            """
            #print(self.hand_type, "is next to body, resetting")
            self.continous_no_gesture_frame_count += 1
            print('no gesture count', self.continous_no_gesture_frame_count)
            if self.continous_no_gesture_frame_count > self.no_action_threshold:
                self.receiving_frames = False
                self.continous_no_gesture_frame_count = 0

                self.global_lock.acquire()
                self.forest_status.is_ready = True
                self.event_vars.learn_no_action_event.set()
                self.global_lock.release()

            self.ref_frames = []
            self.palm_centers = []

        self.skip_frame = (self.skip_frame + 1) % 3  # skip every 2 frames to maximize variance in learning input

    def _palm_center_buffer_variance(self):
        """
        This method calculates the variance of distances of palm centers between two consecutive frames.
        :return: The variance
        """
        if len(self.palm_centers) < self.buffer_length:
            return None
        distances = np.linalg.norm(np.array(self.palm_centers[:-1]) - np.array(self.palm_centers[1:]), axis=0)
        return np.std(distances)

    def _image_augmentation(self):
        """
        May use this for image augmentation more variance in reference images for one shot learning
        """
        pass

    def load_forest(self):
        self.global_lock.acquire()

        if self.is_flipped:
            load_hand_type = "RH"
        else:
            load_hand_type = self.hand_type
        load_path = './models/%s/forest.pickle' % load_hand_type
        print('Loading random forest checkpoint: %s' % load_path)

        with open(load_path, 'rb') as f:
            self.forest = pickle.load(f, encoding='latin1')
        self.forest_status.is_fresh = True  # whether the forest is a fresh copy
        self.forest_status.is_ready = True  # whether the forest is ready to be used for classification
        self.new_gesture_index = 32  # reset the index
        self.event_vars.load_forest_event.clear()

        self.global_lock.release()

        print('%s forest loaded!' % load_hand_type)
