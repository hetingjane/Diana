from .rules import Rule, All, And


class StateMachine:

    def __init__(self, prefix, states, states_with_rules, initial_state):
        self._prefix = prefix

        assert set(states_with_rules.keys()) == set(states), "{} != {}".format(set(states_with_rules.keys()), set(states))
        self._rules = states_with_rules

        assert initial_state in self._rules, "Initial state {} not found in {}".format(initial_state, self._rules)
        self._initial_state = initial_state

        self._cur_state = initial_state

    def input(self, *inputs):
        assert len(inputs) > 0, "At least one input must be provided"
        rule_result = Rule.IS_FALSE

        for state, rule in self._rules[self._cur_state].items():
            rule_result = rule.match(*inputs)
            if rule_result == Rule.IS_TRUE:
                self.reset_rules(self._cur_state)
                self._cur_state = state
                break
            elif rule_result == Rule.IS_FALSE:
                rule.reset()

        return rule_result == Rule.IS_TRUE

    def reset_rule(self, from_state, to_state):
        self._rules[from_state][to_state].reset()

    def reset_rules(self, from_state):
        for state in self._rules[from_state]:
            self.reset_rule(from_state, state)

    def reset(self):
        # Reset rules in the current state
        self.reset_rules(self._cur_state)

        # Transition to initial state
        changed = self._cur_state != self._initial_state
        self._cur_state = self._initial_state
        return changed

    def get_state(self):
        return self._cur_state

    def get_full_state(self):
        return self._prefix + (" " if len(self._prefix) > 0 else "") + self._cur_state

    def in_initial_state(self):
        return self._cur_state == self._initial_state

    def get_transitions(self):
        return [(from_state, to_state) for from_state in self._rules for to_state in self._rules[from_state]]

    def get_rule(self, from_state, to_state):
        return self._rules[from_state][to_state]

    def set_rule(self, from_state, to_state, new_rule):
        self._rules[from_state][to_state] = new_rule

    def __repr__(self):
        return '{state}: {transitions}'.format(state=self.get_full_state(), transitions=self._rules[self._cur_state])


class BinaryStateMachine(StateMachine):
    def __init__(self, prefix, rule):
        states_with_rules = {
            'stop': {
                'start': rule
            },
            'start': {
                'stop': rule.inverted()
            }
        }
        StateMachine.__init__(self, prefix, ['start', 'stop'], states_with_rules, 'stop')

    def is_started(self):
        return self._cur_state == 'start' or 'start' in self._cur_state


class PoseStateMachine(BinaryStateMachine):
    def __init__(self, prefix, rule):
        BinaryStateMachine.__init__(self, prefix, rule)
        self._rules['stop']['start'] = And(
            self._rules['stop']['start'],
            All(('engaged', 1))
        )
        self._rules['start']['stop'] = self._rules['stop']['start'].inverted()
