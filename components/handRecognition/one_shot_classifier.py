import threading
import queue
import sys

from base_classifier import BaseClassifier
from realtime_hand_recognition import RealTimeHandRecognitionOneShot
import RandomForest
from RandomForest.threaded_one_shot import OneShotWorker

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

    def __init__(self):
        BaseClassifier.__init__(self)

        self.global_lock = threading.Lock()
        self.forest_status = ForestStatus()
        self.one_shot_queue = queue.Queue()  # The one-shot learning code reads from the queue to process.
        self.event_vars = EventVars()  # event variables used for communication between threads
        self.new_gesture_index = 32  # refers to 'new gesture 1', increments after every new gesture is learned
        self.taught_gesture_index = 36  # refers to 'taught gesture 1', increments after every new gesture is learned

        self.one_shot_worker = OneShotWorker("RH", self.hand_recognition, self.forest_status, self.event_vars,
                                             self.one_shot_queue, self.global_lock, is_test=False)
        self.one_shot_worker.start()
        self.event_vars.load_forest_event.set()
        self.learning = False  # whether the system is learning gesture

    def _process(self, timestamp, width, height, posx, posy, depth_data, writer_data_hand, engaged, frame_pieces, hand, gestures):

        if not engaged:
            if not self.forest_status.is_fresh:
                self.global_lock.acquire()
                self.taught_gesture_index = 36
                self.forest_status.is_ready = False
                self.forest_status.is_fresh = True
                self.event_vars.load_forest_event.set()
                self.global_lock.release()

        if writer_data_hand == b'learn':
            self.global_lock.acquire()
            self.forest_status.is_ready = False
            self.global_lock.release()
            self.learning = True  # start learning mode

        if posx == -1 and posy == -1:
            max_index = len(self.probs) - 1  # max_index refers to 'blind'
            self.probs[max_index] = 1
        else:
            hand_arr = self._preprocess_hand_arr(depth_data, posx, posy, height, width)

            self.one_shot_queue.put((hand_arr, frame_pieces, writer_data_hand == b'learn'))

            if hand == "RH":
                feature = self.hand_recognition.classify(hand_arr, flip=False)
            else:
                feature = self.hand_recognition.classify(hand_arr, flip=True)
            max_index, dist = self._find_label(feature)
            self.probs[max_index] = dist

        print('{:<20}'.format(gestures[max_index]), '{:.3}'.format(float(self.probs[max_index])), end='\t')

        return max_index

    def _get_hand_recognition(self):
        return RealTimeHandRecognitionOneShot("RH", self.num_gestures)

    def _find_label(self, feature):
        """
        The sequence of the 4 if statements are extremely important
        :param feature: Only one feature is accepted here
        :return:
        """
        max_index, dist = len(self.probs) - 1, 1  # default is blind
        if self.learning:
            max_index = 35  # refers to posture 'learning'
            dist = 1
        if self.event_vars.learn_no_action_event.is_set():
            # learning failed, forest should be ready so find the label again
            self.learning = False
            self.event_vars.learn_no_action_event.clear()
        if self.forest_status.is_ready:
            max_index, dist = self.one_shot_worker.forest.find_nn(feature)
            max_index = max_index[0]
            # feature vector has a dimension of 1024, so dist[0]/1023/2 is the probability
            dist = (0.5 - dist[0] / 2046.0)
        if self.event_vars.learn_complete_event.is_set():
            # the learning process successfully completes
            self.learning = False
            max_index = self.taught_gesture_index  # refers to posture 'learned'
            print([max_index, gestures[max_index]] * 100)
            self.taught_gesture_index += 1
            dist = 1
            self.event_vars.learn_complete_event.clear()

        return max_index, dist
