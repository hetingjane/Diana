class StateMachine:

    def __init__(self, states, transition_table, start_state):
        self.states = states
        self.transition_table = transition_table
        if start_state in self.states:
            self.cur_state = start_state
        else:
            raise ValueError("Initial state does not match any of the valid states")

    def input(self, input_symbol):
        for state in self.transition_table[self.cur_state]:
            if self.transition_table[self.cur_state][state](input_symbol):
                self.cur_state = state
                return True
        return False

    def get_state(self):
        return self.cur_state

