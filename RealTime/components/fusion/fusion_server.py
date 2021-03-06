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
            self.capture_csv.writerow(['engaged', 'attentive', 'la', 'ra', 'lh', 'rh', 'head', 'speech'])

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
            print("Skipped {:.2f}%".format(self.skipped * 100.0 / self.received))

    def _exit(self):
        """
        Exits the application by printing a summary, stopping all the background network threads, and exiting
        :return:
        """
        self._print_summary()
        self._stop()
        if self.capture_file is not None:
            self.capture_file.close()
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
        # Get synced data without blocking with timeout
        self.latest_s_msg = thread_sync.synced_msgs.get(True)
        if self.debug:
            print("Latest synced message: {}".format(self.latest_s_msg), end='\n\n')

        self._update_queues()
        self.received += 1
        if thread_sync.synced_msgs.qsize() <= 15:
            if self.debug:
                print("Backlog queue size exceeded limit: {}".format(thread_sync.synced_msgs.qsize()))
        else:
            self.skipped += 1
            print("Skipping because backlog too large: {}".format(thread_sync.synced_msgs.qsize()))

    def _get_probs(self):

        if streams.is_active("Body"):
            body_msg = self.latest_s_msg["Body"]
            engaged = body_msg.data.engaged
            larm_probs, rarm_probs = np.array(body_msg.data.p_l_arm), np.array(body_msg.data.p_r_arm)
        else:
            engaged = True
            larm_probs, rarm_probs = np.zeros(8), np.zeros(8)

        lhand_probs = np.array(self.latest_s_msg["LH"].data.probabilities) \
            if streams.is_active("LH") else np.zeros(len(postures.left_hand_postures))
        rhand_probs = np.array(self.latest_s_msg["RH"].data.probabilities) \
            if streams.is_active("RH") else np.zeros(len(postures.right_hand_postures))
        head_probs = np.array(self.latest_s_msg["Head"].data.probabilities) \
            if streams.is_active("Head") else np.zeros(len(postures.head_postures))

        emotion_probs = np.array(self.latest_s_msg["Emotion"].data.probabilities) \
            if streams.is_active("Emotion") else np.ones(1) # Assume full attention if no attention stream

        return engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs, emotion_probs

    def _prepare_probs(self):
        engaged, larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs, emotion_probs = self._get_probs()
        all_probs = list(np.concatenate((larm_probs, rarm_probs, lhand_probs, rhand_probs, head_probs, emotion_probs), axis=0))
        return struct.pack("<" + str(len(all_probs)) + "f", *all_probs)

    def _get_inputs(self):
        if streams.is_active("Body"):
            body_msg = self.latest_s_msg["Body"]
            idx_l_arm, idx_r_arm = body_msg.data.idx_l_arm, body_msg.data.idx_r_arm
            idx_engaged = int(body_msg.data.engaged)
        else:
            idx_l_arm, idx_r_arm, idx_engaged = -1, -1, -1

        pose_l_arm = postures.left_arm_motions[idx_l_arm]
        pose_r_arm = postures.right_arm_motions[idx_r_arm]

        engaged = postures.engaged[idx_engaged]

        if streams.is_active("Emotion"):
            emo_msg = self.latest_s_msg["Emotion"]
            idx_attentive = int(emo_msg.data.attentive)
        else:
            idx_attentive = -1

        attentive = postures.attentive[idx_attentive]

        if streams.is_active("Head"):
            head_msg = self.latest_s_msg["Head"]
            idx_head = head_msg.data.idx_head
        else:
            idx_head = -1

        pose_head = postures.head_postures[idx_head]

        lh_msg = self.latest_s_msg["LH"]
        lh_idx = lh_msg.data.idx_hand
        pose_lh = postures.left_hand_postures[lh_idx]

        rh_msg = self.latest_s_msg["RH"]
        rh_idx = rh_msg.data.idx_hand
        pose_rh = postures.right_hand_postures[rh_idx]

        inputs = engaged, attentive, pose_l_arm, pose_r_arm, pose_lh, pose_rh, pose_head
        return inputs

    def _get_events(self):

        inputs = self._get_inputs()

        if streams.is_active("Speech"):
            command = self.latest_s_msg["Speech"].data.command # command in new language model won't work with SMs
        else:
            command = ""

        if len(command) > 0:
            inputs += (command,)

        if self.capture_csv is not None:
            self.capture_csv.writerow(inputs + (command,))

        if streams.is_active("Body"):
            body_msg = self.latest_s_msg["Body"]
            lx, ly, var_l_x, var_l_y = body_msg.data.pos_l_x, body_msg.data.pos_l_y, body_msg.data.var_l_x, body_msg.data.var_l_y
            rx, ry, var_r_x, var_r_y = body_msg.data.pos_r_x, body_msg.data.pos_r_y, body_msg.data.var_r_x, body_msg.data.var_r_y
            engaged = body_msg.data.engaged
        else:
            lx, ly, var_l_x, var_l_y, rx, ry, var_r_x, var_r_y = (float("-inf"),)*8
            engaged = True

        # More than one output data is possible from multiple state machines
        all_events_to_send = []

        ts = "{0:.3f}".format(time.time())
        lx, ly, var_l_x, var_l_y, rx, ry, var_r_x, var_r_y = ["{0:.2f}".format(x) for x in [lx, ly, var_l_x, var_l_y, rx, ry, var_r_x, var_r_y]]

        for state_machine in self.state_machines:
            # Input the combined label to the state machine
            changed = state_machine.input(*inputs)
            cur_state = state_machine.get_full_state()
            if 'probable' in cur_state:
                continue

            # Intercept continuous point state machines; their events are different
            if state_machine is machines.left_point_continuous:
                # Only start states generate events, no stop event is sent, implied by absence
                if state_machine.is_started():
                    all_events_to_send.append("P;l,{},{},{},{};{}".format(lx, ly, var_l_x, var_l_y, ts))
            elif state_machine is machines.right_point_continuous:
                if state_machine.is_started():
                    all_events_to_send.append("P;r,{},{},{},{};{}".format(rx, ry, var_r_x, var_r_y, ts))

            elif changed:
                if state_machine is machines.left_point and state_machine.is_started():
                    cur_state += ",{},{}".format(lx, ly)
                elif state_machine is machines.right_point and state_machine.is_started():
                    cur_state += ",{},{}".format(rx, ry)

                all_events_to_send.insert(0, "G;" + cur_state + ";" + ts)

        if engaged and len(command) > 0:
            all_events_to_send.append("S;{};{}".format(command, ts))

        if not engaged:
            self._clear_synced_data()

        return all_events_to_send

    def _prepare_events(self):
        raw_events_to_send = []
        all_events_to_send = self._get_events()

        for e in all_events_to_send:
            ev = e.split(';')
            if ev[0] != 'P':
                print('{:5}{:<40}{:>}'.format(*ev))
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
            if thread_sync.remote_connected.is_set():
                thread_sync.remote_events.put(e)

        if thread_sync.gui_connected.is_set():
            ev_count = struct.pack("<i", len(raw_events_list))
            new_ev = ev_count + b''.join(raw_events_list) + raw_probs
            thread_sync.gui_events.put(new_ev)


def create_one_shot_learning_events(n):
    assert n >= 1

    from .automata.statemachines import PoseStateMachine
    from .automata.rules import All

    one_shot_learning_events = []

    for i in range(n):
        gesture_name = ' '.join(['rh', 'gesture', str(i + 1)])
        assert gesture_name in postures.right_hand_postures
        one_shot_learning_events.append(PoseStateMachine(gesture_name, All((gesture_name, 5))))

    for i in range(n, 2 * n):
        gesture_name = ' '.join(['lh', 'gesture', str(i + 1)])
        assert gesture_name in postures.left_hand_postures
        one_shot_learning_events.append(PoseStateMachine(gesture_name, All((gesture_name, 5))))
    return one_shot_learning_events


brandeis_events = [machines.engage, machines.wave, machines.attentive,
                   machines.posack, machines.negack, machines.nevermind,
                   machines.left_point, machines.right_point,
                   machines.left_point_continuous, machines.right_point_continuous,
                   machines.push_front, machines.servo_back,
                   machines.push_servo_left, machines.push_servo_right,
                   machines.grab,
                   machines.teaching]

brandeis_events += create_one_shot_learning_events(3)

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
