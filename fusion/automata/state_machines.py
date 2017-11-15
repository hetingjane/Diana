# NOT A GENERAL PURPOSE STATE MACHINE BECAUSE OF ENGAGE/DISENGAGE CHECKS ON INPUTS
from fusion.automata.rules import *


class BinaryStateMachine:

    def __init__(self, states, transition_table, start_state, threshold=5):
        if len(states) != 2:
            raise ValueError("Number of states does not equal 2")
        self.states = states
        self.transition_table = transition_table
        if start_state in self.states:
            self.start_state = start_state
        else:
            raise ValueError("Initial state does not match any of the valid states")
        self.cur_state = start_state
        self.threshold = threshold
        self.cur_val = 0

    def input(self, engaged, high_pose, low_pose=None):
        # Need prevention against stream of engage/disengage which is first bit in combined input symbol
        if not engaged:
            # If reset causes a change
            return self.reset()
        transitioned = False
        for state in self.transition_table[self.cur_state]:
            if self.transition_table[self.cur_state][state](high_pose):
                transitioned = True
                self.cur_val += 1
                if self.cur_val == self.threshold:
                    self.cur_state = state
                    self.cur_val = 0
                    return True
                # First matched state should be the only one processed
                # Since the machine is binary, may not make a difference
                break

        # If we are here, then no state transition occured
        # either because threshold was not met, or actually
        # no transition condition was met

        # To avoid spikes in transitions
        # make change counter 0 if no transition was triggered
        if not transitioned:
            self.cur_val = 0

        return False

    def get_state(self):
        return self.cur_state

    def is_started(self):
        return "start" in self.cur_state

    def is_stopped(self):
        return "stop" in self.cur_state

    def reset(self):
        self.cur_val = 0
        changed = False
        if self.cur_state != self.start_state:
            self.cur_state = self.start_state
            changed = True
        return changed


class TriStateMachine:

    _states_arr = ["stop", "low", "high"]
    _states = dict(zip(_states_arr, range(len(_states_arr))))

    def __init__(self, name, rule, thresholds=5):
        self.name = name
        self.start_state = TriStateMachine._states["stop"]
        self.cur_state = self.start_state
        self.rule = rule
        if isinstance(thresholds, int):
            thresholds = (thresholds,) * len(TriStateMachine._states_arr)
            self.thresholds = dict(zip(TriStateMachine._states_arr, thresholds))
        elif len(thresholds) == len(TriStateMachine._states_arr):
            self.thresholds = dict(zip(TriStateMachine._states_arr, thresholds))
        else:
            raise ValueError("thresholds must be an integer or a list of size " + str(len(TriStateMachine._states_arr)))
        # Counts for transition from each state
        self.cur_val = dict((s, 0) for s in TriStateMachine._states_arr)

    def input(self, engaged, high_pose, low_pose):
        # Reset if not engaged
        if not engaged:
            return self.reset()

        # Try high pose first, and possibly transition to high state
        if self.rule(high_pose):
            return self._try_transition_to("high")
        elif self.rule(low_pose):
            return self._try_transition_to("low")
        else:
            return self._try_transition_to("stop")

    def _is_cur_state(self, state):
        return self.cur_state == TriStateMachine._states[state]

    # WARNING: Check with a rule before attempting a transition
    def _try_transition_to(self, state):
        present = False
        for s in TriStateMachine._states_arr:
            if s != state:
                self.cur_val[s] = 0
            else:
                present = True
        if not present:
            raise KeyError("Attempted transition to absent state: " + state)

        transitioned = False
        if not self._is_cur_state(state):
            self.cur_val[state] += 1
            if self.cur_val[state] == self.thresholds[state]:
                self.cur_val[state] = 0
                self.cur_state = TriStateMachine._states[state]
                transitioned = True
        else:
            self.cur_val[state] = 0

        return transitioned

    def get_state(self):
        return self.name + " " + TriStateMachine._states_arr[self.cur_state]

    def is_stopped(self):
        return self._is_cur_state("stop")

    def is_low(self):
        return self._is_cur_state("low")

    def is_high(self):
        return self._is_cur_state("high")

    def is_started(self):
        return self.is_high() or self.is_low()

    def get_name(self):
        return self.name

    def reset(self):
        for k in self.cur_val.keys():
            self.cur_val[k] = 0
        transitioned = False
        if self.cur_state != self.start_state:
            self.cur_state = self.start_state
            transitioned = True
        return transitioned

class GrabStateMachine:

    _states_arr = ["grab stop", "grab still", "grab move up", "grab move down", "grab move left", "grab move right",
               "grab move front", "grab move back"]
    _states = dict(zip(_states_arr, range(len(_states_arr))))

    def __init__(self, thresholds=(8, 8, 2, 2, 2, 2, 2, 2)):
        self.start_state = GrabStateMachine._states['grab stop']
        self.cur_state = self.start_state
        if isinstance(thresholds, int):
            thresholds = (thresholds,) * len(GrabStateMachine._states_arr)
            self.thresholds = dict(zip(GrabStateMachine._states_arr, thresholds))
        elif len(thresholds) == len(GrabStateMachine._states_arr):
            self.thresholds = dict(zip(GrabStateMachine._states_arr, thresholds))
        else:
            raise ValueError("thresholds must be an integer or a list of size " + str(len(GrabStateMachine._states_arr)))

        # Counts for transition from each state
        self.cur_val = dict((s, 0) for s in GrabStateMachine._states_arr)

        self._grab_still_rule = or_rules(
            match_all('rh claw down', 'RA: still'),
            match_all('lh claw down', 'LA: still')
        )

        self._grab_move_front_rule = or_rules(
            match_all('rh claw down', 'RA: move front'),
            match_all('lh claw down', 'LA: move front')
        )

        self._grab_move_back_rule = or_rules(
            match_all('rh claw down', 'RA: move back'),
            match_all('lh claw down', 'LA: move back')
        )

        self._grab_move_left_rule = or_rules(
            match_all('rh claw down', 'RA: move left'),
            match_all('lh claw down', 'LA: move left')
        )

        self._grab_move_right_rule = or_rules(
            match_all('rh claw down', 'RA: move right'),
            match_all('lh claw down', 'LA: move right')
        )

        self._grab_move_up_rule = or_rules(
            match_all('rh claw down', 'RA: move up'),
            match_all('lh claw down', 'LA: move up')
        )

        self._grab_move_down_rule = or_rules(
            match_all('rh claw down', 'RA: move down'),
            match_all('lh claw down', 'LA: move down')
        )

        self._grab_move_rule = or_rules(
            and_rules(
                match_all('lh claw down'),
                match_any(
                    'LA: move left', 'LA: move right', 'LA: move front', 'LA: move back', 'LA: move up', 'LA: move down'
                )
            ),
            and_rules(
                match_all('rh claw down'),
                match_any(
                    'RA: move left', 'RA: move right', 'RA: move front', 'RA: move back', 'RA: move up', 'RA: move down'
                )
            )
        )

    def input(self, engaged, high_pose, low_pose=None):
        # Need prevention against disengage
        if not engaged:
            # If reset causes a change
            return self.reset()

        # If current state is stop
        if self._is_cur_state('grab stop'):
            # If grab is possible then move to grab
            if self._is_pose_grab_still(high_pose):
                return self._try_transition_to('grab still')
        # Else if current state is grab
        elif self._is_cur_state('grab still'):
            # If grab move is possible in any direction then move to that direction
            if self._is_pose_grab_move(high_pose):
                return self._try_transition_to_moves(high_pose)
            elif not self._is_pose_grab_still(high_pose):
                return self._try_transition_to('grab stop')
        # Else one of the grab move states
        else:
            if self._is_pose_grab_still(high_pose):
                return self._try_transition_to('grab still')
            elif self._is_pose_grab_move(high_pose):
                return self._try_transition_to_moves(high_pose)
            else:
                return self._try_transition_to('grab stop')

    def get_state(self):
        s = GrabStateMachine._states_arr[self.cur_state]
        if s == 'grab still':
            return 'grab high'
        elif s == 'grab stop':
            return s
        else:
            return s + ' high'

    def _is_cur_state(self, state):
        return self.cur_state == GrabStateMachine._states[state]

    def _is_pose_grab_still(self, pose):
        return self._grab_still_rule(pose)

    def _is_pose_grab_move(self, pose):
        return self._grab_move_rule(pose)

    def _is_pose_grab_front(self, pose):
        return self._grab_move_front_rule(pose)

    def _is_pose_grab_back(self, pose):
        return self._grab_move_back_rule(pose)

    def _is_pose_grab_left(self, pose):
        return self._grab_move_left_rule(pose)

    def _is_pose_grab_right(self, pose):
        return self._grab_move_right_rule(pose)

    def _is_pose_grab_up(self, pose):
        return self._grab_move_up_rule(pose)

    def _is_pose_grab_down(self, pose):
        return self._grab_move_down_rule(pose)

    # WARNING: Check with a rule before attempting a transition
    def _try_transition_to(self, state):
        present = False
        for s in GrabStateMachine._states_arr:
            if s != state:
                self.cur_val[s] = 0
            else:
                present = True
        if not present:
            raise KeyError("Attempted transition to absent state: " + state)

        transitioned = False
        if not self._is_cur_state(state):
            self.cur_val[state] += 1
            if self.cur_val[state] == self.thresholds[state]:
                self.cur_val[state] = 0
                self.cur_state = GrabStateMachine._states[state]
                transitioned = True
        else:
            self.cur_val[state] = 0

        return transitioned

    def _try_transition_to_moves(self, pose):
        if self._is_pose_grab_front(pose):
            return self._try_transition_to('grab move front')
        elif self._is_pose_grab_back(pose):
            return self._try_transition_to('grab move back')
        elif self._is_pose_grab_left(pose):
            return self._try_transition_to('grab move left')
        elif self._is_pose_grab_right(pose):
            return self._try_transition_to('grab move right')
        elif self._is_pose_grab_up(pose):
            return self._try_transition_to('grab move up')
        elif self._is_pose_grab_down(pose):
            return self._try_transition_to('grab move down')
        else:
            return False

    def reset(self):
        for k in self.cur_val.keys():
            self.cur_val[k] = 0
        transitioned = False
        if self.cur_state != self.start_state:
            self.cur_state = self.start_state
            transitioned = True
        return transitioned






