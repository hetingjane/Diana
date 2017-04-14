# NOT A GENERAL PURPOSE STATE MACHINE BECAUSE OF ENGAGE/DISENGAGE CHECKS ON INPUTS
class StateMachine:

    def __init__(self, states, transition_table, start_state):
        self.states = states
        self.transition_table = transition_table
        if start_state in self.states:
            self.start_state = start_state
        else:
            raise ValueError("Initial state does not match any of the valid states")
        self.cur_state = start_state

    def input(self, input_symbol):
        # Need prevention against stream of engage/disengage which is first bit in combined input symbol
        engage = input_symbol & 0x1
        if engage == 0:
            # If reset causes a change
            return self.reset()

        for state in self.transition_table[self.cur_state]:
            if self.transition_table[self.cur_state][state](input_symbol):
                self.cur_state = state
                return True
        return False

    def get_state(self):
        return self.cur_state

    def reset(self):
        changed = False
        if self.cur_state != self.start_state:
            self.cur_state = self.start_state
            changed = True
        return changed


