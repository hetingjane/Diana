import threading
import numpy as np
import matplotlib.pyplot as plt
import pickle
import queue


class OneShotWorker(threading.Thread):
    def __init__(self, hand_type, hand_classfier, forest_status, event_vars, one_shot_queue,
                 global_lock, is_test=False):
        threading.Thread.__init__(self)
        self.hand_type = hand_type
        self.hand_classfier = hand_classfier
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
        self.spine_mid_ind = 1
        self.spine_mid_coordinates_ind_start = self.spine_mid_ind * 9 + 7
        self.spine_mid_coordinates_ind_end = self.spine_mid_ind * 9 + 10
        self.forest = None  # random forest instance
        self.receiving_frames = False  # Only start to receive frames when a signal is sent from kinect server
        self.skip_frame = 0  # skip some frames to maximize variance in learning input, use this variable to keep track
        self.pixel_intensity_threshold = 0.4  # used in self._is_gesture()
        self.buffer_length = 30  # determines the number of frames in the buffer (3 seconds of gapped frames)
        self.moving_variance_threshold = 0.001  # used in self._can_start(), starts learning when the arm stops moving
        self.continous_no_gesture_frame_count = 0
        self.no_action_threshold = 120  # if there are no gestures for continous 5 seconds, do not learn for this hand
        self.ref_frames = []  # a buffer list of frames to learn
        self.palm_centers = []  # a buffer list of palm center coordinates

    def run(self):
        while True:
            if self.event_vars.load_forest_event.is_set():
                self.load_forest()
            # hand_arr, skeleton_arr, start_learn = self.one_shot_queue.get()
            try:
                hand_arr, skeleton_arr, start_learn = self.one_shot_queue.get(block=True, timeout=2)
                self.add_frame(hand_arr, skeleton_arr, start_learn)
            except queue.Empty:
                pass

    def add_frame(self, hand_arr, skeleton_arr, start_learn):
        """
        When learning starts, hand depth array and body skeleton array needs to used to extract necessary information
        and stored for further processing.
        :param hand_arr: hand depth array from self.decode_frame_hand()
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
        if self._is_gesture(skeleton_arr):
            if not self.skip_frame:
                self.ref_frames.append(hand_arr)
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
                        for i, each_hand_arr in enumerate(self.ref_frames):
                            if self.hand_type == "RH":
                                new_features.append(self.hand_classfier.classify(each_hand_arr, flip=False)[0])
                            else:
                                new_features.append(self.hand_classfier.classify(each_hand_arr, flip=True)[0])
                            if self.is_test:
                                img_name = "/s/red/a/nobackup/vision/jason/DraperLab/Demo/reference_imgs/%d.png" % i
                                plt.imsave(img_name, np.squeeze(each_hand_arr))
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

    def _is_gesture(self, skeleton_arr):
        """
        The hand is performing a gesture only if it's above the spine mid. This is not perfect, but good enough.
        :param skeleton: kinect skeleton array
        :param hand: string with either "RH" or "LH"
        :return: A boolean about whether the hand is performing a gesture
        """
        spine_mid_y = skeleton_arr[self.spine_mid_coordinates_ind_start+1]
        hand_y = skeleton_arr[self.palm_coordinates_ind_start+1]
        return hand_y > spine_mid_y

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

        #load_path = 'models/%s/forest.pickle' % self.hand_type
        load_path = 'models/%s/forest.pickle' % self.hand_type
        print('Loading random forest checkpoint: %s' % load_path)
        f = open(load_path, 'rb')
        self.forest = pickle.load(f, encoding='latin1')
        f.close()

        self.forest_status.is_fresh = True  # whether the forest is a fresh copy
        self.forest_status.is_ready = True  # whether the forest is ready to be used for classification
        self.new_gesture_index = 32  # reset the index
        self.event_vars.load_forest_event.clear()

        self.global_lock.release()

        print('%s forest loaded!' % self.hand_type)
