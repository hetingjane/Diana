import datetime
import sys
import time
import argparse

from automata.state_machine import BinaryStateMachine
from fusion.automata.rules import *
from fusion_thread import Fusion
from remote_thread import Remote
from support.endpoints import *
from support.postures import *
from thread_sync import *

class App:

    def __init__(self, state_machines, debug):
        self._start()

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
        # Start fusion thread
        self.fusion = Fusion()
        self.fusion.start()
        self.started = True

        # Start server thread for Brandeis
        self.remote = Remote('Brandeis', 'brandeis', remote_events, remote_connected)
        self.remote.start()
        self.remote_started = True

        # Start server thread for GUI
        self.gui = Remote('GUI', 'gui', gui_events, gui_connected)
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
            # Get synced data without blocking with timeout
            self.latest_data = synced_data.get(False, 0.2)
            if self.debug:
                print "Latest synced data: ", self.latest_data, "\n"
            #
            self._update_queues()
            self.received += 1
            if synced_data.qsize() <= 15:
                if self.debug:
                    print "Backlog queue size exceeded limit: " + str(synced_data.qsize())
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
        left_arm_label, right_arm_label, lx, ly, rx, ry = self.latest_data[streams.get_stream_id("Body")][2:8]

        head_label = self.latest_data[streams.get_stream_id("Head")][2]
        word = self.latest_data[streams.get_stream_id("Speech")][2]

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

            # Don't do anything with continuous point stops
            if "continuous point stop" in state_machine.get_state():
                continue
            if state_machine.get_state() == "left continuous point start":
                all_events_to_send.append("P;l,{0:.2f},{1:.2f};{2:s}".format(lx, ly, ts))
                continue
            if state_machine.get_state() == "right continuous point start":
                all_events_to_send.append("P;r,{0:.2f},{1:.2f};{2:s}".format(rx, ry, ts))
                continue

            if changed:
                # Get bytes from the string according to UTF-8 encoding
                new_state = state_machine.get_state()
                # Prepare ; delimited data consisting of <state>;<timestamp>
                # Sort events by stop then start
                if new_state.split(' ')[-1] == 'stop':
                    all_events_to_send = [ "G;" + new_state + ";" + ts] + all_events_to_send
                else:
                    if "left point" in new_state:
                        new_state += ",{0:.2f},{1:.2f}".format(lx, ly)
                    elif "right point" in new_state:
                        new_state += ",{0:.2f},{1:.2f}".format(rx, ry)
                    all_events_to_send.append("G;" + new_state + ";" + ts)

        if engage_vec == 1 and len(word) > 0:
            all_events_to_send.append("S;" + word + ";" + ts)

        for e in all_events_to_send:
            ev_type, ev, timestamp = e.split(';')
            print ev_type.ljust(5) + ev.ljust(30) + timestamp + "\n\n"
            raw_events_to_send.append(struct.pack("<i" + str(len(e)) + "s", len(e), e))

        return raw_events_to_send

    def _prepare_probs(self):
        body_probs = self.latest_data[streams.get_stream_id("Body")][-13:-1]
        lhand_probs = self.latest_data[streams.get_stream_id("LH")][-len(left_hand_postures):]
        rhand_probs = self.latest_data[streams.get_stream_id("RH")][-len(right_hand_postures):]
        head_probs = self.latest_data[streams.get_stream_id("Head")][-len(head_postures):]

        all_probs = body_probs + lhand_probs + rhand_probs + head_probs

        return struct.pack("<" + str(len(all_probs)) + "f", *all_probs)

    def _update_queues(self):
        """
        Update the queues for events to be sent.
        Currently, two queues are updated: Remote GUI and Remote Client (Brandeis)
        :return: None
        """

        raw_events_list = self._prepare_events()
        raw_probs = self._prepare_probs()

        # Include a check to see if the destination is connected or not
        for e in raw_events_list:
            if remote_connected.wait(0.0):
                remote_events.put(e)

        if gui_connected.wait(0.0):
            ev_count = struct.pack("<i", len(raw_events_list))
            gui_events.put(ev_count + ''.join(raw_events_list) + raw_probs)

sm_ack = BinaryStateMachine(["posack start", "posack stop"], {
    "posack stop": {
        "posack start": match_any('rh thumbs up', 'lh thumbs up', 'head nod')
    },
    "posack start": {
        "posack stop": mismatch_all('rh thumbs up', 'lh thumbs up', 'head nod')
    }
}, "posack stop")


sm_nack = BinaryStateMachine(["negack start", "negack stop"], {
    "negack stop": {
        "negack start": match_any('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop', 'head shake')
    },
    "negack start": {
        "negack stop": mismatch_all('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop', 'head shake')
    }
}, "negack stop")


sm_engage = BinaryStateMachine(["engage start", "engage stop"], {
    "engage stop": {
        "engage start": match_any('engage')
    },
    "engage start": {
        "engage stop": mismatch_all('engage')
    }
}, "engage stop", 1)

sm_left_point_vec = BinaryStateMachine(["left point start", "left point stop"], {
    "left point stop": {
        "left point start": and_rules(
            match_all('LA: still'),
            match_any('lh point down', 'lh point right', 'lh point front')
        )
    },
    "left point start": {
        "left point stop": or_rules(
            mismatch_any('LA: still'),
            mismatch_all('lh point down', 'lh point right', 'lh point front')
        )
    }
}, "left point stop")

sm_right_point_vec = BinaryStateMachine(["right point start", "right point stop"], {
    "right point stop": {
        "right point start": and_rules(
            match_all('RA: still'),
            match_any('rh point down', 'rh point left', 'rh point front')
        )
    },
    "right point start": {
        "right point stop": or_rules(
            mismatch_any('RA: still'),
            mismatch_all('rh point down', 'rh point left', 'rh point front')
        )
    }
}, "right point stop")

sm_left_continuous_point = BinaryStateMachine(["left continuous point start", "left continuous point stop"], {
    "left continuous point stop": {
        "left continuous point start": match_any('lh point down', 'lh point right', 'lh point front')
    },
    "left continuous point start": {
        "left continuous point stop": mismatch_all('lh point down', 'lh point right', 'lh point front')
    }
}, "left continuous point stop", 3)

sm_right_continuous_point = BinaryStateMachine(["right continuous point start", "right continuous point stop"], {
    "right continuous point stop": {
        "right continuous point start": match_any('rh point down', 'rh point left', 'rh point front')
    },
    "right continuous point start": {
        "right continuous point stop": mismatch_all('rh point down', 'rh point left', 'rh point front')
    }
}, "right continuous point stop", 3)

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

brandeis_events = [ sm_engage, sm_ack, sm_nack, sm_grab,
                    sm_left_point_vec, sm_right_point_vec,
                    sm_left_continuous_point, sm_right_continuous_point,
                    sm_grab_move_right, sm_grab_move_left, sm_grab_move_up, sm_grab_move_down,
                    sm_grab_move_front, sm_grab_move_back,
                    sm_push_left, sm_push_right, sm_push_back, sm_push_front ]

csu_events = [ sm_engage, sm_ack, sm_nack, sm_grab,
               sm_left_point_vec, sm_right_point_vec,
               sm_left_continuous_point, sm_right_continuous_point,
               sm_grab_move_right, sm_grab_move_left, sm_grab_move_up, sm_grab_move_down,
               sm_grab_move_front, sm_grab_move_back,
               sm_grab_move_right_front, sm_grab_move_left_front,
               sm_grab_move_left_back, sm_grab_move_right_back,
               sm_push_left, sm_push_right, sm_push_back, sm_push_front,

               sm_count_one, sm_count_two, sm_count_three, sm_count_four, sm_count_five,
               sm_arms_together_X, sm_arms_apart_X, sm_arms_together_Y, sm_arms_apart_Y ]

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
