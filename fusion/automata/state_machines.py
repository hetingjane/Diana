# NOT A GENERAL PURPOSE STATE MACHINE BECAUSE OF ENGAGE/DISENGAGE CHECKS ON INPUTS
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
        changed = False
        for state in self.transition_table[self.cur_state]:
            if self.transition_table[self.cur_state][state](high_pose):
                changed = True
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
        if not changed:
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
    _states = {
        "stop": 0,
        "low": 1,
        "high": 2
    }

    _states_arr = ["stop", "low", "high"]

    def __init__(self, name, rule, threshold=5):
        self.name = name
        self.start_state = TriStateMachine._states["stop"]
        self.cur_state = self.start_state
        self.rule = rule
        self.threshold = threshold
        # Counts for transition from each state
        self.cur_val = dict((s, 0) for s in TriStateMachine._states_arr)

    def input(self, engaged, high_pose, low_pose):
        # Reset if not engaged
        if not engaged:
            return self.reset()
        transitioned = False
        # Try high pose first, and possibly transition to high state
        if self.rule(high_pose):
            self.cur_val["low"] = 0
            self.cur_val["stop"] = 0
            if not self.is_high():
                self.cur_val["high"] += 1
                if self.cur_val["high"] == self.threshold:
                    self.cur_val["high"] = 0
                    self.cur_state = TriStateMachine._states["high"]
                    transitioned = True

        # Else, low pose, and possibly transition to low state
        elif self.rule(low_pose):
            self.cur_val["high"] = 0
            self.cur_val["stop"] = 0
            if not self.is_low():
                self.cur_val["low"] += 1
                if self.cur_val["low"] == self.threshold:
                    self.cur_val["low"] = 0
                    self.cur_state = TriStateMachine._states["low"]
                    transitioned = True

        # Else, go to stop state
        else:
            self.cur_val["high"] = 0
            self.cur_val["low"] = 0
            if not self.is_stopped():
                self.cur_val["stop"] += 1
                if self.cur_val["stop"] == self.threshold:
                    self.cur_val["stop"] = 0
                    self.cur_state = TriStateMachine._states["stop"]
                    transitioned = True

        return transitioned

    def get_state(self):
        return self.name + " " + TriStateMachine._states_arr[self.cur_state]

    def is_stopped(self):
        return self.cur_state == TriStateMachine._states["stop"]

    def is_low(self):
        return self.cur_state == TriStateMachine._states["low"]

    def is_high(self):
        return self.cur_state == TriStateMachine._states["high"]

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
