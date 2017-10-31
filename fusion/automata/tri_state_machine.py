class TriStateMachine:
    _states = {
        "stop": 0,
        "low": 1,
        "high": 2
    }

    _states_arr = ["stop", "low", "high"]

    def __init__(self, name, rule, threshold):
        self.name = name
        self.start_state = TriStateMachine._states["stop"]
        self.cur_state = self.start_state
        self.rule = rule
        self.threshold = threshold
        self.cur_val = 0

    def input(self, engage, pose_high, pose_low):
        # Reset if not engaged
        if not engage:
            return self.reset()
        transitioned = False
        # Try high pose first, and possibly transition to high state
        if self.rule(pose_high):
            if not self.is_high():
                self.cur_val += 1
                if self.cur_val == self.threshold:
                    transitioned=True
                    self.cur_state = TriStateMachine._states["high"]
        # Else, low pose, and possibly transition to low state
        elif self.rule(pose_low):
            if not self.is_low():
                self.cur_val += 1
                if self.cur_val == self.threshold:
                    transitioned = True
                    self.cur_state = TriStateMachine._states["low"]
        # Else, go to stop state
        elif not self.is_stopped():
            self.cur_val += 1
            if self.cur_val == self.threshold:
                transitioned = True
                self.cur_state = TriStateMachine._states["stop"]
        return transitioned

    def is_stopped(self):
        return self.cur_state == TriStateMachine._states["stop"]

    def is_low(self):
        return self.cur_state == TriStateMachine._states["low"]

    def is_high(self):
        return self.cur_state == TriStateMachine._states["high"]

    def get_state(self):
        return self.name + " " + TriStateMachine._states_arr[self.cur_state]

    def reset(self):
        self.cur_val = 0
        transitioned = False
        if self.cur_state != self.start_state:
            self.cur_state = self.start_state
            transitioned = True
        return transitioned
