import threading
import queue
import sys

from .base_classifier import BaseClassifier
from .realtime_hand_recognition import RealTimeHandRecognitionOneShot
from . import RandomForest
from .RandomForest.threaded_one_shot import OneShotWorker

sys.modules['RandomForest'] = RandomForest  # to load the pickle file


class ForestStatus:
    __slots__ = ('is_fresh', 'is_ready')

    def __init__(self):
        self.is_fresh = True  # whether the forest is a fresh copy
        self.is_ready = False  # whether the forest is ready to be used for classification


class EventVars:
    __slots__ = ('load_forest_event', 'learn_no_action_event', 'learn_initialize_event', 'learn_complete_event')

    def __init__(self):
        self.load_forest_event = threading.Event()  # signals whether to reload a fresh copy of forest
        self.learn_no_action_event = threading.Event()  # whether learning process exits due to no valid gestures
        self.learn_initialize_event = threading.Event()  # whether learning process is initiated
        self.learn_complete_event = threading.Event()  # whether learning process is finished


class OneShotClassifier(BaseClassifier):

    def __init__(self, hand, stream_id):
        BaseClassifier.__init__(self, hand, stream_id)

        self.global_lock = threading.Lock()
        self.forest_status = ForestStatus()
        self.one_shot_queue = queue.Queue()  # The one-shot learning code reads from the queue to process.
        self.event_vars = EventVars()  # event variables used for communication between threads
        self.new_gesture_index = self.num_gestures + 1  # refers to 'grab cup'

        self.one_shot_worker = OneShotWorker(self.hand, self.hand_recognition, self.forest_status, self.event_vars,
                                             self.one_shot_queue, self.new_gesture_index, self.global_lock,
                                             is_test=False)
        self.one_shot_worker.start()
        self.event_vars.load_forest_event.set()
        self.learn_status = False  # whether to learn, record learning status received from kinect server

    def _process(self, timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces):
        result_probs = [0 for i in range(self.num_gestures + 2)]

        if not engaged:
            if not self.forest_status.is_fresh:
                self.global_lock.acquire()
                self.forest_status.is_ready = False
                self.forest_status.is_fresh = True
                self.event_vars.load_forest_event.set()
                self.global_lock.release()

        if writer_data_hand == b'learn':
            self.global_lock.acquire()
            self.forest_status.is_ready = False
            self.global_lock.release()
            self.learn_status = True
        else:
            self.learn_status = False

        if posx == -1 and posy == -1:
            max_index = len(result_probs) - 2  # max_index refers to 'blind'
            result_probs[max_index] = 1
        else:
            hand_arr = self._preprocess_hand_arr(depth_data, posx, posy, height, width)

            self.one_shot_queue.put((hand_arr, frame_pieces, self.learn_status))

            feature = self.hand_recognition.classify(hand_arr)
            max_index, dist = self._find_label(feature)
            if max_index is not None:
                # feature vector has a dimension of 1024, so dist[0]/1023/2 is the probability
                result_probs[max_index] = (0.5 - dist / 2046.0)

        if max_index is not None:
            print(timestamp, self.gestures[max_index], result_probs[max_index])
        else:
            # set the max_index to blind when the forest is not ready, although should not be used
            max_index = len(result_probs) - 2
            print('Forest Not Ready...')

        return max_index, result_probs

    def _get_hand_recognition(self):
        return RealTimeHandRecognitionOneShot(self.hand, self.num_gestures)

    def _find_label(self, feature):
        """
        :param feature: Only one feature is accepted here
        :return:
        """
        label_index, dist = None, None
        if self.forest_status.is_ready:
            label_index, dist = self.one_shot_worker.forest.find_nn(feature)
            label_index = label_index[0]
            dist = dist[0]
        return label_index, dist

    def _get_status_to_fusion(self):
        status_to_fusion = 0  # a status integer sent to fusion
        if self.event_vars.learn_no_action_event.is_set():
            # if the hand does not perform any valid gesture, exit without learning
            status_to_fusion = 3
            self.event_vars.learn_no_action_event.clear()
        if self.event_vars.learn_initialize_event.is_set():
            # learning process is initialized but has not finished
            status_to_fusion = 1
            self.event_vars.learn_initialize_event.clear()
        if self.event_vars.learn_complete_event.is_set():
            # if the learning process successfully completes
            status_to_fusion = 2
            self.event_vars.learn_complete_event.clear()

        return status_to_fusion

