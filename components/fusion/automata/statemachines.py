from .rules import Not, All, And


class StateMachine:

    def __init__(self, prefix, states, states_with_rules, initial_state):
        self._prefix = prefix

        assert set(states_with_rules.keys()) == set(states)
        self._rules = states_with_rules

        assert initial_state in self._rules
        self._initial_state = initial_state

        self._cur_state = initial_state

    def input(self, *inputs):
        assert len(inputs) > 0
        changed = False

        for state, rule in self._rules.items():
            if state != self._cur_state:
                changed = self._rules[state].match(*inputs)
                if changed:
                    self._cur_state = state
                    break

        return changed

    def reset(self):
        changed = self._cur_state != self._initial_state
        self._cur_state = self._initial_state
        return changed

    def get_state(self):
        return self._cur_state

    def get_full_state(self):
        return "{} {}".format(self._prefix, self._cur_state)

    def in_initial_state(self):
        return self._cur_state == self._initial_state


class BinaryStateMachine(StateMachine):
    def __init__(self, prefix, rule):
        states_with_rules = {
            'high': rule,
            'stop': Not(rule)
        }
        StateMachine.__init__(self, prefix, ['high', 'stop'], states_with_rules, 'stop')


class PoseStateMachine(BinaryStateMachine):
    def __init__(self, prefix, rule):
        BinaryStateMachine.__init__(self, prefix, rule)
        self._rules['high'] = And(
            self._rules['high'],
            All(('engaged', 1))
        )
        self._rules['stop'] = Not(
            self._rules['high']
        )
