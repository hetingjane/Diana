import Queue
import datetime
import struct
import time
import sys

from conf import streams
from conf.postures import left_hand_postures, right_hand_postures, left_arm_motions, right_arm_motions, head_postures
from fusion_thread import Fusion
from remote_thread import Remote
from automata.state_machine import BinaryStateMachine
from thread_sync import *

from support.constants import *


class App:

    def __init__(self, state_machines, debug):
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
        self.debug = debug

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
        head_label = self.latest_data[streams.get_stream_id("Head")][2]

        left_hand_gesture_received = left_hand_postures[left_hand_label]
        right_hand_gesture_received = right_hand_postures[right_hand_label]
        left_arm_movement_received = left_arm_motions[left_arm_label]
        right_arm_movement_received = right_arm_motions[right_arm_label]
        head_gesture_received = head_postures[head_label]

        # Convert integer labels to one hot representations
        left_hand_vec = posture_to_vec[left_hand_gesture_received]
        right_hand_vec = posture_to_vec[right_hand_gesture_received]
        left_arm_vec = posture_to_vec[left_arm_movement_received]
        right_arm_vec = posture_to_vec[right_arm_movement_received]
        head_vec = posture_to_vec[head_gesture_received]

        combined_vec = engage_vec | left_hand_vec | right_hand_vec | left_arm_vec | right_arm_vec | head_vec

        # More than one output data is possible from multiple state machines
        all_events_to_send = []
        raw_events_to_send = []

        ts = datetime.datetime.fromtimestamp(time.time()).strftime("%M:%S:%f")[:-3]

        for state_machine in self.state_machines:

            # Input the combined label to the state machine
            changed = state_machine.input(combined_vec)

            if changed:
                # Get bytes from the string according to UTF-8 encoding
                new_state = state_machine.get_state()
                # Prepare ; delimited data consisting of <state>;<timestamp>
                # Sort events by stop then start
                if new_state.split(' ')[-1] == 'stop':
                    all_events_to_send = [new_state + ";" + ts] + all_events_to_send
                else:
                    all_events_to_send.append(new_state + ";" + ts)

        for e in all_events_to_send:
            state, timestamp = e.split(';')
            print state.ljust(30) + timestamp + "\n\n"
            raw_events_to_send.append(struct.pack("!i" + str(len(e)) + "s", len(e), e))

        return raw_events_to_send

    def _prepare_probs(self):
        body_probs = self.latest_data[streams.get_stream_id("Body")][-13:-1]
        lhand_probs = self.latest_data[streams.get_stream_id("LH")][-len(left_hand_postures):]
        rhand_probs = self.latest_data[streams.get_stream_id("RH")][-len(right_hand_postures):]
        head_probs = self.latest_data[streams.get_stream_id("Head")][-len(head_postures):]

        all_probs = body_probs + lhand_probs + rhand_probs + head_probs

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


def match_any(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    at least one of the postures matches with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """
    posture_vecs = [ posture_to_vec[posture] for posture in postures ]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) == v:
                return True
        return False

    return f


def match_all(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    all of the postures match with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """
    posture_vecs = [ posture_to_vec[posture] for posture in postures ]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) != v:
                return False
        return True

    return f


def mismatch_any(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    at least one of the postures does not match with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """

    posture_vecs = [ posture_to_vec[posture] for posture in postures ]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) != v:
                return True
        return False

    return f


def mismatch_all(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    none of the postures matches with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """
    posture_vecs = [ posture_to_vec[posture] for posture in postures ]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) == v:
                return False
        return True

    return f


# Meta rule for ANDing
def and_rules(*rules):
    """
    Returns a rule that computes the logical AND of the input rules with the input mask
    :param rules: One or more boolean rules
    :return: Rule that is logical AND of the input boolean rules
    """
    def f(in_sym):
        for rule in rules:
            if not rule(in_sym):
                return False
        return True

    return f


# Meta rule for ORing
def or_rules(*rules):
    """
    Returns a rule that computes the logical OR of the input rules with the input mask
    :param rules: One or more boolean rules
    :return: Rule that is logical OR of the input boolean rules
    """
    def f(in_sym):
        for rule in rules:
            if rule(in_sym):
                return True
        return False

    return f


# Create vector form for each posture
engage_vec = [ 1 << 0 ]
vecs = engage_vec + [1 << i for i in range(1, len(left_hand_postures + right_hand_postures + left_arm_motions + right_arm_motions + head_postures) + 1)]
postures = ["engage"] + left_hand_postures + right_hand_postures + left_arm_motions + right_arm_motions + head_postures

vec_to_posture = dict(zip(vecs, postures))
posture_to_vec = dict(zip(postures, vecs))

sm_ack = BinaryStateMachine(["posack start", "posack stop"], {
    "posack stop": {
        "posack start": match_any('rh thumbs up', 'lh thumbs up')
    },
    "posack start": {
        "posack stop": mismatch_all('rh thumbs up', 'lh thumbs up')
    }
}, "posack stop")


sm_engage = BinaryStateMachine(["engage start", "engage stop"], {
    "engage stop": {
        "engage start": match_any('engage')
    },
    "engage start": {
        "engage stop": mismatch_all('engage')
    }
}, "engage stop", 1)


sm_point_left = BinaryStateMachine(["point left start", "point left stop"], {
    "point left stop": {
        "point left start": match_any('rh point left')
    },
    "point left start": {
        "point left stop": mismatch_all('rh point left')
    }
}, "point left stop")

sm_point_left_front = BinaryStateMachine(["point left front start", "point left front stop"], {
    "point left front stop": {
        "point left front start": match_all('rh point front', 'RA: move left front')
    },
    "point left front start": {
        "point left front stop": mismatch_all('rh point front')
    }
}, "point left front stop")

sm_point_right_front = BinaryStateMachine(["point right front start", "point right front stop"], {
    "point right front stop": {
        "point right front start": match_all('lh point front', 'LA: move right front')
    },
    "point right front start": {
        "point right front stop": mismatch_all('lh point front')
    }
}, "point right front stop")


sm_point_right = BinaryStateMachine(["point right start", "point right stop"], {
    "point right stop": {
        "point right start": match_any('lh point right')
    },
    "point right start": {
        "point right stop": mismatch_all('lh point right')
    }
}, "point right stop")


sm_point_front = BinaryStateMachine(["point front start", "point front stop"], {
    "point front stop": {
        "point front start": match_any('lh point front', 'rh point front')
    },
    "point front start": {
        "point front stop": mismatch_all('lh point front', 'rh point front')
    }
}, "point front stop")


sm_point_down = BinaryStateMachine(["point down start", "point down stop"], {
    "point down stop": {
        "point down start": match_any('lh point down', 'rh point down')
    },
    "point down start": {
        "point down stop": mismatch_all('lh point down', 'rh point down')
    }
}, "point down stop")


sm_nack = BinaryStateMachine(["negack start", "negack stop"], {
    "negack stop": {
        "negack start": match_any('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop')
    },
    "negack start": {
        "negack stop": mismatch_all('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop')
    }
}, "negack stop")


sm_grab = BinaryStateMachine(["grab start", "grab stop"], {
    "grab stop": {
        "grab start": match_any('rh claw down', 'lh claw down')
    },
    "grab start": {
        "grab stop": mismatch_all('rh claw down', 'lh claw down')
    }
}, "grab stop")


sm_grab_move_right = BinaryStateMachine(["grab move right start", "grab move right stop"], {
    "grab move right start": {
        "grab move right stop": and_rules(
            mismatch_any('rh claw down', 'RA: move right'),
            mismatch_any('lh claw down', 'LA: move right')
        )
    },
    "grab move right stop": {
        "grab move right start": or_rules(
            match_all('rh claw down', 'RA: move right'),
            match_all('lh claw down', 'LA: move right')
        )
    }
}, "grab move right stop")


sm_grab_move_left = BinaryStateMachine(["grab move left start", "grab move left stop"], {
    "grab move left start": {
        "grab move left stop": and_rules(
            mismatch_any('rh claw down', 'RA: move left'),
            mismatch_any('lh claw down', 'LA: move left')
        )
    },
    "grab move left stop": {
        "grab move left start": or_rules(
            match_all('rh claw down', 'RA: move left'),
            match_all('lh claw down', 'LA: move left')
        )
    }
}, "grab move left stop")


sm_grab_move_up = BinaryStateMachine(["grab move up start", "grab move up stop"], {
    "grab move up start": {
        "grab move up stop": and_rules(
            mismatch_any('rh claw down', 'RA: move up'),
            mismatch_any('lh claw down', 'LA: move up')
        )
    },
    "grab move up stop": {
        "grab move up start": or_rules(
            match_all('rh claw down', 'RA: move up'),
            match_all('lh claw down', 'LA: move up')
        )
    }
}, "grab move up stop")


sm_grab_move_down = BinaryStateMachine(["grab move down start", "grab move down stop"], {
    "grab move down start": {
        "grab move down stop": and_rules(
            mismatch_any('rh claw down', 'RA: move down'),
            mismatch_any('lh claw down', 'LA: move down')
        )
    },
    "grab move down stop": {
        "grab move down start": or_rules(
            match_all('rh claw down', 'RA: move down'),
            match_all('lh claw down', 'LA: move down')
        )
    }
}, "grab move down stop")


sm_grab_move_front = BinaryStateMachine(["grab move front start", "grab move front stop"], {
    "grab move front start": {
        "grab move front stop": and_rules(
            mismatch_any('rh claw down', 'RA: move front'),
            mismatch_any('lh claw down', 'LA: move front')
        )
    },
    "grab move front stop": {
        "grab move front start": or_rules(
            match_all('rh claw down', 'RA: move front'),
            match_all('lh claw down', 'LA: move front')
        )
    }
}, "grab move front stop")


sm_grab_move_back = BinaryStateMachine(["grab move back start", "grab move back stop"], {
    "grab move back start": {
        "grab move back stop": and_rules(
            mismatch_any('rh claw down', 'RA: move back'),
            mismatch_any('lh claw down', 'LA: move back')
        )
    },
    "grab move back stop": {
        "grab move back start": or_rules(
            match_all('rh claw down', 'RA: move back'),
            match_all('lh claw down', 'LA: move back')
        )
    }
}, "grab move back stop")


sm_grab_move_right_front = BinaryStateMachine(["grab move right front start", "grab move right front stop"], {
    "grab move right front start": {
        "grab move right front stop": and_rules(
            mismatch_any('rh claw down', 'RA: move right front'),
            mismatch_any('lh claw down', 'LA: move right front')
        )
    },
    "grab move right front stop": {
        "grab move right front start": or_rules(
            match_all('rh claw down', 'RA: move right front'),
            match_all('lh claw down', 'LA: move right front')
        )
    }
}, "grab move right front stop")


sm_grab_move_left_front = BinaryStateMachine(["grab move left front start", "grab move left front stop"], {
    "grab move left front start": {
        "grab move left front stop": and_rules(
            mismatch_any('rh claw down', 'RA: move left front'),
            mismatch_any('lh claw down', 'LA: move left front')
        )
    },
    "grab move left front stop": {
        "grab move left front start": or_rules(
            match_all('rh claw down', 'RA: move left front'),
            match_all('lh claw down', 'LA: move left front')
        )
    }
}, "grab move left front stop")


sm_grab_move_left_back = BinaryStateMachine(["grab move left back start", "grab move left back stop"], {
    "grab move left back start": {
        "grab move left back stop": and_rules(
            mismatch_any('rh claw down', 'RA: move left back'),
            mismatch_any('lh claw down', 'LA: move left back')
        )
    },
    "grab move left back stop": {
        "grab move left back start": or_rules(
            match_all('rh claw down', 'RA: move left back'),
            match_all('lh claw down', 'LA: move left back')
        )
    }
}, "grab move left back stop")


sm_grab_move_right_back = BinaryStateMachine(["grab move right back start", "grab move right back stop"], {
    "grab move right back start": {
        "grab move right back stop": and_rules(
            mismatch_any('rh claw down', 'RA: move right back'),
            mismatch_any('lh claw down', 'LA: move right back')
        )
    },
    "grab move right back stop": {
        "grab move right back start": or_rules(
            match_all('rh claw down', 'RA: move right back'),
            match_all('lh claw down', 'LA: move right back')
        )
    }
}, "grab move right back stop")


sm_push_left = BinaryStateMachine(["push left start", "push left stop"], {
    "push left start": {
        "push left stop": or_rules(
            mismatch_all('rh closed left', 'rh open left'),
            mismatch_any('RA: move left')
        )
    },
    "push left stop": {
        "push left start": and_rules(
            match_any('rh closed left', 'rh open left'),
            match_all('RA: move left')
        )
    }
}, "push left stop")


sm_push_right = BinaryStateMachine(["push right start", "push right stop"], {
    "push right start": {
        "push right stop": or_rules(
            mismatch_all('lh closed right', 'lh open right'),
            mismatch_any('LA: move right')
        )
    },
    "push right stop": {
        "push right start": and_rules(
            match_any('lh closed right', 'lh open right'),
            match_all('LA: move right')
        )
    }
}, "push right stop")


sm_push_front = BinaryStateMachine(["push front start", "push front stop"], {
    "push front start": {
        "push front stop": and_rules(
            mismatch_any('rh closed front', 'RA: move front'),
            mismatch_any('lh closed front', 'LA: move front')
        )
    },
    "push front stop": {
        "push front start": or_rules(
            match_all('rh closed front', 'RA: move front'),
            match_all('lh closed front', 'LA: move front')
        )
    }
}, "push front stop")


sm_push_back = BinaryStateMachine(["push back start", "push back stop"], {
    "push back start": {
        "push back stop": and_rules(
            or_rules(
                mismatch_all('rh open back', 'rh closed back'),
                mismatch_any('RA: move back')
            ),
            or_rules(
                mismatch_all('lh open back', 'lh closed back'),
                mismatch_any('LA: move back')
            ),
            mismatch_all('rh beckon', 'lh beckon')
        )
    },
    "push back stop": {
        "push back start": or_rules(
            and_rules(
                match_any('rh open back', 'rh closed back'),
                match_all('RA: move back')
            ),
            and_rules(
                match_any('lh open back', 'lh closed back'),
                match_all('LA: move back')
            ),
            match_any('rh beckon', 'lh beckon')
        )
    }
}, "push back stop")


sm_count_one = BinaryStateMachine(["count one start", "count one stop"], {
    "count one stop": {
        "count one start": match_any('rh one front', 'lh one front')
    },
    "count one start": {
        "count one stop": mismatch_all('rh one front', 'lh one front')
    }
}, "count one stop")


sm_count_two = BinaryStateMachine(["count two start", "count two stop"], {
    "count two stop": {
        "count two start": match_any('rh two front', 'rh two back', 'lh two front', 'lh two back')
    },
    "count two start": {
        "count two stop": mismatch_all('rh two front', 'rh two back', 'lh two front', 'lh two back')
    }
}, "count two stop")


sm_count_three = BinaryStateMachine(["count three start", "count three stop"], {
    "count three stop": {
        "count three start": match_any('rh three front', 'rh three back', 'lh three front', 'lh three back')
    },
    "count three start": {
        "count three stop": mismatch_all('rh three front', 'rh three back', 'lh three front', 'lh three back')
    }
}, "count three stop")


sm_count_four = BinaryStateMachine(["count four start", "count four stop"], {
    "count four stop": {
        "count four start": match_any('rh four front', 'lh four front')
    },
    "count four start": {
        "count four stop": mismatch_all('rh four front', 'lh four front')
    }
}, "count four stop")


sm_count_five = BinaryStateMachine(["count five start", "count five stop"], {
    "count five stop": {
        "count five start": match_any('rh five front', 'lh five front')
    },
    "count five start": {
        "count five stop": mismatch_all('rh five front', 'lh five front')
    }
}, "count five stop")


sm_arms_apart_X = BinaryStateMachine(["arms apart X start", "arms apart X stop"], {
    "arms apart X stop": {
        "arms apart X start": match_all('LA: apart X', 'RA: apart X')
    },
    "arms apart X start": {
        "arms apart X stop": mismatch_any('LA: apart X', 'RA: apart X')
    }
}, "arms apart X stop")


sm_arms_together_X = BinaryStateMachine(["arms together X start", "arms together X stop"], {
    "arms together X stop": {
        "arms together X start": match_all('LA: together X', 'RA: together X')
    },
    "arms together X start": {
        "arms together X stop": mismatch_any('LA: together X', 'RA: together X')
    }
}, "arms together X stop")


sm_arms_apart_Y = BinaryStateMachine(["arms apart Y start", "arms apart Y stop"], {
    "arms apart Y stop": {
        "arms apart Y start": match_all('LA: apart Y', 'RA: apart Y')
    },
    "arms apart Y start": {
        "arms apart Y stop": mismatch_any('LA: apart Y', 'RA: apart Y')
    }
}, "arms apart Y stop")


sm_arms_together_Y = BinaryStateMachine(["arms together Y start", "arms together Y stop"], {
    "arms together Y stop": {
        "arms together Y start": match_all('LA: together Y', 'RA: together Y')
    },
    "arms together Y start": {
        "arms together Y stop": mismatch_any('LA: together Y', 'RA: together Y')
    }
}, "arms together Y stop")

brandeis_events = [ sm_engage, sm_ack, sm_point_left, sm_point_right, sm_point_front, sm_point_down, sm_nack, sm_grab,
                    sm_grab_move_right, sm_grab_move_left, sm_grab_move_up, sm_grab_move_down,
                    sm_grab_move_front, sm_grab_move_back,
                    sm_push_left, sm_push_right, sm_push_back, sm_push_front ]

csu_events = [ sm_engage, sm_ack, sm_point_left, sm_point_right, sm_point_front, sm_point_down, sm_nack, sm_grab,
               sm_grab_move_right, sm_grab_move_left, sm_grab_move_up, sm_grab_move_down,
               sm_grab_move_front, sm_grab_move_back,
               sm_grab_move_right_front, sm_grab_move_left_front,
               sm_grab_move_left_back, sm_grab_move_right_back,
               sm_push_left, sm_push_right, sm_push_back, sm_push_front,

               sm_count_one, sm_count_two, sm_count_three, sm_count_four, sm_count_five,
               sm_arms_together_X, sm_arms_apart_X, sm_arms_together_Y, sm_arms_apart_Y ]

import argparse
if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--mode', choices=['csu', 'brandeis'], default='brandeis', type=str,
                        help="the mode in which fusion server is run")
    parser.add_argument('-D', '--debug', dest='debug_mode', default=False, action='store_true', help='enable the debug mode')
    args = parser.parse_args()
    if args.mode == 'brandeis':
        print "Running in Brandeis mode"
        event_set = brandeis_events
    elif args.mode == 'csu':
        print "Running in CSU mode"
        event_set = csu_events

    a = App(event_set, args.debug_mode)
    a.run()
