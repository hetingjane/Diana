import Queue
import datetime
import struct
import time
import sys

from conf import streams
from conf.postures import right_hand_postures, left_hand_postures, body_postures
from fusion_thread import Fusion
from remote_thread import Remote
from automata.state_machine import StateMachine
from thread_sync import *

from support.constants import *


class App:

    def __init__(self, state_machines):
        # Start fusion thread
        self.fusion = Fusion()
        self.fusion.start()
        self.started = True

        # Start remote thread at port 9126
        self.remote = Remote("Brandeis", ('', FUSION_BRANDEIS_PORT), remote_events, remote_connected)
        self.remote.start()
        self.remote_started = True

        # Start remote gui n/w thread at port 9127
        self.gui = Remote("GUI", ('', FUSION_GUI_PORT), gui_events, gui_connected)
        self.gui.start()
        self.gui_started = True

        # Initialize the state manager
        self.state_machines = state_machines

        # For performance evaluation
        self.skipped = 0
        self.received = 0

    def _stop(self):
        # Stop the fusion thread
        self.fusion.stop()
        self.started = False

        # Stop the remote output thread
        self.remote.stop()
        self.remote_started = False

        # Stop the remote GUI thread
        self.gui.stop()
        self.gui_started = False

        # Clear sync queque in case resetting
        self._clear_synced_data()

        self.skipped = 0
        self.received = 0

    def _clear_synced_data(self):
        not_empty = True
        while not_empty:
            try:
                synced_data.get_nowait()
            except Queue.Empty:
                not_empty = False

    def _start(self):
        # Start the fusion thread
        self.fusion = Fusion()
        self.fusion.start()
        self.started = True

        # Start remote thread at port 9126
        self.remote = Remote(('', FUSION_BRANDEIS_PORT), "Brandeis", remote_events, remote_connected)
        self.remote.start()
        self.remote_started = True

        # Start remote gui n/w thread at port 9127
        self.gui = Remote(('', FUSION_GUI_PORT), "GUI", gui_events, gui_connected)
        self.gui.start()
        self.gui_started = True

    def _print_summary(self):
        """
        Prints a summary of skipped timestamps to keep up with the input rate
        :return: Nothing
        """
        if self.received > 0:
            print "Skipped percentage: " + str(self.skipped * 100.0 / self.received)

    def _exit(self):
        """
        Exits the application by printing a summary, stopping all the background network threads, and exiting
        :return:
        """
        self._print_summary()
        self._stop()
        sys.exit(0)

    def run(self):
        try:
            while True:
                self._update()
        except KeyboardInterrupt:
            self._exit()


    def _update(self):
        """
        Updates the latest synced data, and requests updating the latest gesture
        Also prints debugging messages related to sync queue
        :return:
        """
        try:
            self.latest_data = synced_data.get(False, 0.2)
            #print self.latest_data
            self._update_queues()
            self.received += 1
            if synced_data.qsize() <= 15:
                pass
                #print "Backlog queue size: " + str(synced_data.qsize())
            else:
                self.skipped += 1
                print "Timestamp " + str(self.latest_data[streams.get_stream_id("Body")][1]) + \
                      " skipped because backlog too large: " + str(synced_data.qsize())
        except Queue.Empty:
            pass

    def _prepare_events(self):
        # Check if engaged or not
        # Will be 0 or 1, can be directly ored
        engage_vec = self.latest_data[streams.get_stream_id("Body")][-1]

        # Get appropriate labels as integer values
        left_hand_label = self.latest_data[streams.get_stream_id("LH")][2]
        right_hand_label = self.latest_data[streams.get_stream_id("RH")][2]
        left_arm_label = self.latest_data[streams.get_stream_id("Body")][2]
        right_arm_label = self.latest_data[streams.get_stream_id("Body")][3]

        left_hand_gesture_received = left_hand_postures[left_hand_label]
        right_hand_gesture_received = right_hand_postures[right_hand_label]
        left_arm_movement_received = body_postures[left_arm_label]
        right_arm_movement_received = body_postures[right_arm_label]

        # Convert integer labels to one hot representations
        left_hand_vec = posture_to_vec[left_hand_gesture_received]
        right_hand_vec = posture_to_vec[right_hand_gesture_received]
        left_arm_vec = posture_to_vec[left_arm_movement_received]
        right_arm_vec = posture_to_vec[right_arm_movement_received]

        combined_vec = engage_vec | left_hand_vec | right_hand_vec | left_arm_vec | right_arm_vec

        # More than one output data is possible from multiple state machines
        all_events_to_send = []
        ts = datetime.datetime.fromtimestamp(time.time()).strftime("%M:%S:%f")[:-3]

        for state_machine in self.state_machines:

            # Input the combined label to the state machine
            changed = state_machine.input(combined_vec)

            if changed:
                # Get bytes from the string according to UTF-8 encoding
                new_state = state_machine.get_state()
                # Prepare ; delimited data consisting of <state>;<timestamp>
                data_to_send = new_state + ";" + ts
                print new_state.ljust(18) + ts + "\n\n"
                all_events_to_send.append( struct.pack("!i" + str(len(data_to_send)) + "s", len(data_to_send), data_to_send) )

        return all_events_to_send

    def _prepare_probs(self):
        body_probs = self.latest_data[streams.get_stream_id("Body")][-13:-1]
        lhand_probs = self.latest_data[streams.get_stream_id("LH")][-len(left_hand_postures):]
        rhand_probs = self.latest_data[streams.get_stream_id("RH")][-len(right_hand_postures):]

        all_probs = body_probs + lhand_probs + rhand_probs

        return struct.pack("!" + str(len(all_probs)) + "f", *all_probs)

    def _update_queues(self):

        raw_events_list = self._prepare_events()
        raw_probs = self._prepare_probs()
       

        if len(raw_events_list) > 0:
            # Include a check to see if the destination is connected or not
            for e in raw_events_list:
                if remote_connected.wait(0.0):
                    remote_events.put(e)

        if gui_connected.wait(0.0):
            ev_count = struct.pack("!i", len(raw_events_list))
            gui_events.put(ev_count + ''.join(raw_events_list) + raw_probs)


def ensure_match(masks):
    """
    Returs a function that allows you to check if input symbol matches at least one of the masks
    For mask to match, all the bits in the mask must be set. It doesn't matter if input has more bits set than in mask
    :param masks: a list of masks you want to be matched
    :return: function that accepts an input symbol and returns True if any of the mask matches input symbol or False otherwise
    """
    def f(in_sym):
        for m in masks:
            if (in_sym & m) == m:
                return True
        return False
    return f


def ensure_mismatch(masks):
    """
    Returns a function that allows you to check if input symbol matches none of the masks
    :param masks: a list of masks you want to be mismatched
    :return: function that accepts an input symbol and returns True if none of the masks mathces input symbol or False otherwise
    """
    def f(in_sym):
        for m in masks:
            if (in_sym & m) == m:
                return False
        return True

    return f


def always_match(masks):
    return True


# Meta rule for ANDing
def and_rules(*rules):

    def f(in_sym):
        for rule in rules:
            if not rule(in_sym):
                return False
        return True

    return f


# Meta rule for ORing
def or_rules(*rules):

    def f(in_sym):
        for rule in rules:
            if rule(in_sym):
                return True
        return False

    return f


# Create vector form for each posture
engage_vec = [ 1 << 0 ]
left_hand_vecs = [1<<i for i in range(1, len(left_hand_postures) + 1)]
right_hand_vecs = [1<<i for i in range(len(left_hand_postures) + 1, len(left_hand_postures) + len(right_hand_postures) + 1)]
body_vecs = [1<<i for i in range(len(left_hand_postures) + len(right_hand_postures) + 1, len(left_hand_postures) + len(right_hand_postures) + len(body_postures) + 1)]

vecs = engage_vec + left_hand_vecs + right_hand_vecs + body_vecs
postures = ["engage"] + left_hand_postures + right_hand_postures + body_postures

vec_to_posture = dict(zip(vecs, postures))
posture_to_vec = dict(zip(postures, vecs))

sm_ack = StateMachine(["posack start", "posack stop"], {
    "posack stop": {
        "posack start": ensure_match([
            posture_to_vec['rh thumbs up'],
            posture_to_vec['lh thumbs up']
        ])
    },
    "posack start": {
        "posack stop": ensure_mismatch([
            posture_to_vec['rh thumbs up'],
            posture_to_vec['lh thumbs up']
        ])
    }
}, "posack stop")

sm_engage = StateMachine(["engage start", "engage stop"], {
    "engage stop": {
        "engage start": ensure_match([
            posture_to_vec['engage']
        ])
    },
    "engage start": {
        "engage stop": ensure_mismatch([
            posture_to_vec['engage']
        ])
    }
}, "engage stop")

sm_point_left = StateMachine(["point left start", "point left stop"], {
    "point left stop": {
        "point left start": ensure_match([
            posture_to_vec['rh point left']
        ])
    },
    "point left start": {
        "point left stop": ensure_mismatch([
            posture_to_vec['rh point left']
        ])
    }
}, "point left stop")

sm_point_right = StateMachine(["point right start", "point right stop"], {
    "point right stop": {
        "point right start": ensure_match([
            posture_to_vec['lh point right']
        ])
    },
    "point right start": {
        "point right stop": ensure_mismatch([
            posture_to_vec['lh point right']
        ])
    }
}, "point right stop")

sm_point_front = StateMachine(["point front start", "point front stop"], {
    "point front stop": {
        "point front start": ensure_match([
            posture_to_vec['lh point front'],
            posture_to_vec['rh point front']
        ])
    },
    "point front start": {
        "point front stop": ensure_mismatch([
            posture_to_vec['lh point front'],
            posture_to_vec['rh point front']
        ])
    }
}, "point front stop")

sm_point_down = StateMachine(["point down start", "point down stop"], {
    "point down stop": {
        "point down start": ensure_match([
            posture_to_vec['lh point down'],
            posture_to_vec['rh point down']
        ])
    },
    "point down start": {
        "point down stop": ensure_mismatch([
            posture_to_vec['lh point down'],
            posture_to_vec['rh point down']
        ])
    }
}, "point down stop")

sm_nack = StateMachine(["negack start", "negack stop"], {
    "nack stop": {
        "negack start": ensure_match([
            posture_to_vec['rh thumbs down'],
            posture_to_vec['lh thumbs down'],
            posture_to_vec['rh stop'],
            posture_to_vec['lh stop']
        ]),
    },
    "negack start": {
        "negack stop": ensure_mismatch([
            posture_to_vec['rh thumbs down'],
            posture_to_vec['lh thumbs down'],
            posture_to_vec['rh stop'],
            posture_to_vec['lh stop']
        ])
    }
}, "negack stop")

sm_grab = StateMachine(["grab start", "grab stop"], {
    "grab stop": {
        "grab start": ensure_match([
            posture_to_vec['rh claw down'],
            posture_to_vec['lh claw down']
        ]),
    },
    "grab start": {
        "grab stop": ensure_mismatch([
            posture_to_vec['rh claw down'],
            posture_to_vec['lh claw down']
        ])
    }
}, "grab stop")

sm_grab_move_right = StateMachine(["grab move right start", "grab move right stop"], {
    "grab move right start": {
        "grab move right stop": and_rules(
            ensure_mismatch([
                posture_to_vec['rh claw down'],
                posture_to_vec['RA: move right']
            ]),
            ensure_mismatch([
                posture_to_vec['lh claw down'],
                posture_to_vec['LA: move right']
            ])
        )
    },
    "grab move right stop": {
        "grab move right start": or_rules(
            and_rules(
                ensure_match([
                    posture_to_vec['rh claw down'],
                ]),
                ensure_match([
                    posture_to_vec['RA: move right']
                ])),
            and_rules(
                ensure_match([
                    posture_to_vec['lh claw down'],
                ]),
                ensure_match([
                    posture_to_vec['LA: move right']
                ]))
            )
    }
}, "grab move right stop")

a = App([ sm_engage, sm_ack , sm_point_left, sm_point_right, sm_point_front, sm_point_down, sm_nack, sm_grab, sm_grab_move_right])

a.run()
