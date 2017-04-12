import Queue
import datetime
import struct
import time
import sys

from conf import streams
from conf.postures import right_hand_postures, left_hand_postures, body_postures
from fusion_thread import Fusion
from output_thread import Remote
from automata.state_machine import StateMachine
from thread_sync import synced_data, output_data, remote_connected


class App:

    def __init__(self):
        # Start fusion thread
        self.fusion = Fusion()
        self.fusion.start()
        self.started = True

        # Start remote thread at port 9126
        self.remote = Remote(('', 9126))
        self.remote.start()
        self.remote_started = True

        # Initialize the state manager
        #self.state_manager = state_manager

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

        # Start the remote output thread
        self.remote = Remote(('', 9126))
        self.remote.start()
        self.remote_started = True

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
                pass
        except KeyboardInterrupt:
            response = input("Do you want to reset? (y/n): ")
            if response == ord('y') or response == ord('Y'):
                print "Resetting..."
                self.run()
            else:
                self._exit()
                sys.exit(0)


a = App()
a.run()