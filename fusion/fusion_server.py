import sys
import time
import argparse

import numpy as np

from fusion.automata.bi_state_machines import *
from fusion.automata.state_machines import GrabStateMachine
from fusion.automata.tri_state_machines import *
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

    def _get_probs(self):
        body_probs = self.latest_data[streams.get_stream_id("Body")][-18:-1]
        engaged = self.latest_data[streams.get_stream_id("Body")][-1] == 1
        emblem, motion, neutral, oscillate, still = body_probs[:5]
        larm_probs, rarm_probs = np.array(body_probs[5:11]), np.array(body_probs[11:])

        lhand_probs = np.array(self.latest_data[streams.get_stream_id("LH")][-len(left_hand_postures):])
        rhand_probs = np.array(self.latest_data[streams.get_stream_id("RH")][-len(right_hand_postures):])
        head_probs = np.array(self.latest_data[streams.get_stream_id("Head")][-len(head_postures):])

        return engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs

    def _prepare_probs(self):
        engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs = self._get_probs()
        all_probs = list(np.concatenate((larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs), axis=0))
        return struct.pack("<" + str(len(all_probs)) + "f", *all_probs)

    def _get_pose_vectors(self, low_threshold=0.1, high_threshold=0.5):

        engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs = self._get_probs()

        larm_label, rarm_label, body_label = self.latest_data[streams.get_stream_id("Body")][2:5]

        hand_labels = np.array(range(len(lhand_probs)))
        high_lhand_labels = hand_labels[lhand_probs >= high_threshold]
        low_lhand_labels = hand_labels[np.logical_and(lhand_probs >= low_threshold, lhand_probs < high_threshold)]
        high_rhand_labels = hand_labels[rhand_probs >= high_threshold]
        low_rhand_labels = hand_labels[np.logical_and(rhand_probs >= low_threshold, rhand_probs < high_threshold)]

        head_labels = np.array(range(len(head_probs)))
        high_head_labels = head_labels[head_probs >= high_threshold]

        # High pose uses max probability arm labels, max probability body label
        # head labels with probabilities in [high_threshold, 1.0],
        # and hand labels with probabilities in [low_threshold, high_threshold)
        high_pose = 1 if engaged else 0
        high_pose |= posture_to_vec[left_arm_motions[larm_label]]
        high_pose |= posture_to_vec[right_arm_motions[rarm_label]]
        high_pose |= posture_to_vec[body_postures[body_label]]

        for l in high_lhand_labels:
            high_pose |= posture_to_vec[left_hand_postures[l]]
        for l in high_rhand_labels:
            high_pose |= posture_to_vec[right_hand_postures[l]]
        for l in high_head_labels:
            high_pose |= posture_to_vec[head_postures[l]]

        # Low pose uses max probability arm labels, and max probability body label
        # no head labels,
        # and hand labels with probabilities in [low_threshold, high_threshold)
        low_pose = 1 if engaged else 0
        low_pose |= posture_to_vec[left_arm_motions[larm_label]]
        low_pose |= posture_to_vec[right_arm_motions[rarm_label]]
        low_pose |= posture_to_vec[body_postures[body_label]]

        for l in low_lhand_labels:
            low_pose |= posture_to_vec[left_hand_postures[l]]
        for l in low_rhand_labels:
            low_pose |= posture_to_vec[right_hand_postures[l]]

        return engaged, high_pose, low_pose

    def _get_events(self):

        engaged, high_pose, low_pose = self._get_pose_vectors()

        lx, ly, rx, ry = self.latest_data[streams.get_stream_id("Body")][4:8]
        word = self.latest_data[streams.get_stream_id("Speech")][2]

        # More than one output data is possible from multiple state machines
        all_events_to_send = []

        ts = "{0:.3f}".format(time.time())

        for state_machine in self.state_machines:
            # Input the combined label to the state machine
            # State machine could be binary or tristate
            changed = state_machine.input(engaged, high_pose, low_pose)
            cur_state = state_machine.get_state()

            # If it is the binary state machine for continuous points
            # and is in start state, append pointer message contents to the sent message
            if state_machine is bsm_left_continuous_point:
                if state_machine.is_started():
                    all_events_to_send.append("P;l,{0:.2f},{1:.2f};{2:s}".format(lx, ly, ts))
            elif state_machine is bsm_right_continuous_point:
                if state_machine.is_started():
                    all_events_to_send.append("P;r,{0:.2f},{1:.2f};{2:s}".format(rx, ry, ts))
            # Else, check if current input caused a transition
            elif changed:
                # For the special case of binary state machines for left point vec and right point vec
                # append x,y coordinates to state
                if state_machine is tsm_left_point_vec or state_machine is bsm_left_point_vec:
                    if state_machine.is_started():
                        cur_state += ",{0:.2f},{1:.2f}".format(lx, ly)
                elif state_machine is tsm_right_point_vec or state_machine is bsm_right_point_vec:
                    if state_machine.is_started():
                        cur_state += ",{0:.2f},{1:.2f}".format(rx, ry)
                # Finally create a timestamped message
                all_events_to_send.insert(0, "G;" + cur_state + ";" + ts)

        if engaged and len(word) > 0:
            all_events_to_send.append("S;" + word + ";" + ts)

        if not engaged:
            self._clear_synced_data()

        return all_events_to_send

    def _prepare_events(self):
        raw_events_to_send = []
        all_events_to_send = self._get_events()

        for e in all_events_to_send:
            ev_type, ev, timestamp = e.split(';')
            print ev_type.ljust(5) + ev.ljust(30) + timestamp + "\n\n"
            raw_events_to_send.append(struct.pack("<i" + str(len(e)) + "s", len(e), e))

        return raw_events_to_send

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


gsm = GrabStateMachine()

brandeis_events = [bsm_engage, bsm_left_continuous_point, bsm_right_continuous_point,
                   tsm_count_five, tsm_count_four, tsm_count_three, tsm_count_two, tsm_count_one,
                   #tsm_grab, tsm_grab_move_back, tsm_grab_move_front, tsm_grab_move_left, tsm_grab_move_right,
                   #tsm_grab_move_up, tsm_gram_move_down,
                   gsm,
                   tsm_negack, tsm_posack,
                   tsm_push_back, tsm_push_front, tsm_push_left, tsm_push_right,
                   tsm_right_point_vec, tsm_left_point_vec]

csu_events = brandeis_events + []

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
