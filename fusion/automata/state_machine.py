# NOT A GENERAL PURPOSE STATE MACHINE BECAUSE OF ENGAGE/DISENGAGE CHECKS ON INPUTS
class BinaryStateMachine:

    def __init__(self, states, transition_table, start_state, change_threshold=5):
        if len(states) != 2:
            raise ValueError("Number of states does not equal 2")
        self.states = states
        self.transition_table = transition_table
        if start_state in self.states:
            self.start_state = start_state
        else:
            raise ValueError("Initial state does not match any of the valid states")
        self.cur_state = start_state
        self.change_threshold = change_threshold
        self.change_count = 0

    def input(self, input_symbol):
        # Need prevention against stream of engage/disengage which is first bit in combined input symbol
        engage = input_symbol & 0x1
        if engage == 0:
            # If reset causes a change
            return self.reset()
        cur_changed = False
        for state in self.transition_table[self.cur_state]:
            if self.transition_table[self.cur_state][state](input_symbol):
                cur_changed = True
                self.change_count += 1
                if self.change_count == self.change_threshold:
                    self.cur_state = state
                    self.change_count = 0
                    return True
                # First matched state should be the only one processed
                # Since the machine is binary, may not make a difference
                break

        # If we are here, then no state transition occured
        # either because threshold was not met, or actually
        # no transition condition was met

        # To avoid spikes in transitions
        # make change counter 0 if genuinely no transition was triggered
        if not cur_changed:
            self.change_count = 0

        return False

    def get_state(self):
        return self.cur_state

    def reset(self):
        self.change_count = 0
        changed = False
        if self.cur_state != self.start_state:
            self.cur_state = self.start_state
            changed = True
        return changed


