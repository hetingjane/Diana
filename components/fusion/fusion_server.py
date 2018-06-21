import sys
import time
import argparse
import struct
import queue
import csv

import numpy as np

from components.fusion.automata import machines as machines
from components.fusion.fusion_thread import Fusion
from components.fusion.remote_thread import Remote
from components.fusion import thread_sync
from components.fusion.conf import streams
from components.fusion.conf import postures


class App:

    def __init__(self, state_machines, debug, capture):
        self._start()

        # Initialize the state manager
        self.state_machines = state_machines

        # For performance evaluation
        self.skipped = 0
        self.received = 0
        self.debug = debug

        if capture:
            print("Capture mode ON")

        self.capture_file = open('captured.csv', 'w') if capture else None
        self.capture_csv = csv.writer(self.capture_file) if capture else None
        if self.capture_csv is not None:
            self.capture_csv.writerow(['engaged', 'la', 'ra', 'lh', 'rh', 'head', 'body', 'speech'])

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
                thread_sync.synced_msgs.get_nowait()
            except queue.Empty:
                not_empty = False

    def _start(self):
        # Start fusion thread
        self.fusion = Fusion()
        self.fusion.start()
        self.started = True

        # Start server thread for Brandeis
        self.remote = Remote('Brandeis', 'brandeis', thread_sync.remote_events, thread_sync.remote_connected)
        self.remote.start()
        self.remote_started = True

        # Start server thread for GUI
        self.gui = Remote('GUI', 'gui', thread_sync.gui_events, thread_sync.gui_connected)
        self.gui.start()
        self.gui_started = True

    def _print_summary(self):
        """
        Prints a summary of skipped timestamps to keep up with the input rate
        :return: Nothing
        """
        if self.received > 0:
            print("Skipped percentage: " + str(self.skipped * 100.0 / self.received))

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
            self.capture_file.close()
            self._exit()

    def _update(self):
        """
        Updates the latest synced data, and requests updating the latest gesture
        Also prints debugging messages related to sync queue
        :return:
        """
        try:
            # Get synced data without blocking with timeout
            self.latest_s_msg = thread_sync.synced_msgs.get(False, 0.2)
            if self.debug:
                print("Latest synced message: {}\n".format(self.latest_s_msg))
            #
            self._update_queues()
            self.received += 1
            if thread_sync.synced_msgs.qsize() <= 15:
                if self.debug:
                    print("Backlog queue size exceeded limit: {}".format(thread_sync.synced_msgs.qsize()))
            else:
                self.skipped += 1
                print("Skipping because backlog too large: {}".format(thread_sync.synced_msgs.qsize()))
        except queue.Empty:
            pass

    def _get_probs(self):

        if streams.is_active("Body"):
            body_msg = self.latest_s_msg["Body"]
            engaged = body_msg.data.engaged
            larm_probs, rarm_probs = np.array(body_msg.data.p_l_arm), np.array(body_msg.data.p_r_arm)
            body_probs = np.array([body_msg.data.p_emblem, body_msg.data.p_motion, body_msg.data.p_neutral,
                               body_msg.data.p_oscillate, body_msg.data.p_still])
        else:
            engaged = True
            larm_probs, rarm_probs = np.zeros(6), np.zeros(6)
            body_probs = np.zeros(5)

        lhand_probs = np.array(self.latest_s_msg["LH"].data.probabilities) \
            if streams.is_active("LH") else np.zeros(len(postures.left_hand_postures))
        rhand_probs = np.array(self.latest_s_msg["RH"].data.probabilities) \
            if streams.is_active("RH") else np.zeros(len(postures.right_hand_postures))
        head_probs = np.array(self.latest_s_msg["Head"].data.probabilities) \
            if streams.is_active("Head") else np.zeros(len(postures.head_postures))

        return engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs, body_probs

    def _prepare_probs(self):
        engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs, body_probs = self._get_probs()
        all_probs = list(np.concatenate((larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs, body_probs), axis=0))
        return struct.pack("<" + str(len(all_probs)) + "f", *all_probs)

    def _get_poses(self):
        if streams.is_active("Body"):
            body_msg = self.latest_s_msg["Body"]
            idx_l_arm, idx_r_arm, idx_body = body_msg.data.idx_l_arm, body_msg.data.idx_r_arm, body_msg.data.idx_body
            idx_engaged = int(body_msg.data.engaged)
        else:
            idx_l_arm, idx_r_arm, idx_body = len(postures.left_arm_motions) - 1, len(
                postures.right_arm_motions) - 1, len(postures.body_postures) - 1
            idx_engaged = int(True)

        pose_l_arm = postures.left_arm_motions[idx_l_arm]
        pose_r_arm = postures.right_arm_motions[idx_r_arm]
        pose_body = postures.body_postures[idx_body]

        engaged = postures.engaged[idx_engaged]

        if streams.is_active("Head"):
            head_msg = self.latest_s_msg["Head"]
            idx_head = head_msg.data.idx_head
        else:
            idx_head = len(postures.head_postures) - 1

        pose_head = postures.head_postures[idx_head]

        lh_msg = self.latest_s_msg["LH"]
        lh_idx = lh_msg.data.idx_hand
        pose_lh = postures.left_hand_postures[lh_idx]

        rh_msg = self.latest_s_msg["RH"]
        rh_idx = rh_msg.data.idx_hand
        pose_rh = postures.right_hand_postures[rh_idx]

        poses = engaged, pose_l_arm, pose_r_arm, pose_lh, pose_rh, pose_head, pose_body
        return poses

    def _get_events(self):

        poses = self._get_poses()

        if streams.is_active("Body"):
            body_msg = self.latest_s_msg["Body"]
            lx, ly, var_l_x, var_l_y = body_msg.data.pos_l_x, body_msg.data.pos_l_y, body_msg.data.var_l_x, body_msg.data.var_l_y
            rx, ry, var_r_x, var_r_y = body_msg.data.pos_r_x, body_msg.data.pos_r_y, body_msg.data.var_r_x, body_msg.data.var_r_y
            engaged = body_msg.data.engaged
        else:
            lx, ly, var_l_x, var_l_y, rx, ry, var_r_x, var_r_y = (float("-inf"),)*8
            engaged = True

        word = self.latest_s_msg["Speech"].data.command if streams.is_active("Speech") else ""

        if self.capture_csv is not None:
            self.capture_csv.writerow(poses + (word,))

        inputs = poses + (word,) if len(word) > 0 else poses

        # More than one output data is possible from multiple state machines
        all_events_to_send = []

        ts = "{0:.3f}".format(time.time())
        lx, ly, var_l_x, var_l_y, rx, ry, var_r_x, var_r_y = ["{0:.2f}".format(x) for x in [lx, ly, var_l_x, var_l_y, rx, ry, var_r_x, var_r_y]]

        for state_machine in self.state_machines:
            # Input the combined label to the state machine
            changed = state_machine.input(*inputs)
            cur_state = state_machine.get_full_state()

            if state_machine is machines.left_point_continuous and state_machine.is_started():
                all_events_to_send.append("P;l,{},{},{},{};{}".format(lx, ly, var_l_x, var_l_y, ts))
            elif state_machine is machines.right_point_continuous and state_machine.is_started():
                all_events_to_send.append("P;r,{},{},{},{};{}".format(rx, ry, var_r_x, var_r_y, ts))

            elif changed:
                if state_machine is machines.left_point and state_machine.is_started():
                    cur_state += ",{},{}".format(lx, ly)
                elif state_machine is machines.right_point and state_machine.is_started():
                    cur_state += ",{},{}".format(rx, ry)

                all_events_to_send.insert(0, "G;" + cur_state + ";" + ts)

        if engaged and len(word) > 0 and word not in postures.words:
            command = ''.join(word.split()[1:]).upper()
            all_events_to_send.append("S;{};{}".format(command, ts))

        if not engaged:
            self._clear_synced_data()

        return all_events_to_send

    def _prepare_events(self):
        raw_events_to_send = []
        all_events_to_send = self._get_events()

        for e in all_events_to_send:
            ev_type, ev, timestamp = e.split(';')
            if ev_type != 'P':
                print(ev_type.ljust(5) + ev.ljust(30) + timestamp + "\n\n")
            raw_events_to_send.append(struct.pack("<i" + str(len(e)) + "s", len(e), e.encode('ascii')))

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
            if thread_sync.remote_connected.wait(0.0):
                thread_sync.remote_events.put(e)
        if len(raw_events_list) > 0:
            #print(len(raw_events_list))
            #print(raw_events_list)
            pass

        if thread_sync.gui_connected.wait(0.0):
            ev_count = struct.pack("<i", len(raw_events_list))
            new_ev = ev_count + ''.join(raw_events_list) + raw_probs
            thread_sync.gui_events.put(new_ev)


brandeis_events = [machines.engage, machines.wave,
                   machines.posack, machines.negack, machines.nevermind,
                   machines.left_point, machines.right_point,
                   machines.left_point_continuous, machines.right_point_continuous,
                   machines.push_left, machines.push_right, machines.push_front, machines.push_back,
                   machines.grab]

csu_events = brandeis_events

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--mode', choices=['csu', 'brandeis'], default='brandeis', type=str,
                        help="the mode in which fusion server is run")
    parser.add_argument('-d', '--debug', dest='debug_mode', default=False, action='store_true', help='enable the debug mode')
    parser.add_argument('-c', '--capture', dest='capture_mode', default=False, action='store_true', help='captures incoming data')
    args = parser.parse_args()

    if args.mode == 'brandeis':
        print("Running in Brandeis mode")
        event_set = brandeis_events
    elif args.mode == 'csu':
        print("Running in CSU mode")
        event_set = csu_events
    else:
        event_set = None

    a = App(event_set, args.debug_mode, args.capture_mode)
    a.run()
