from collections import OrderedDict

from .rules import Rule, Not, All, And


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
        rule_result = Rule.IS_FALSE

        for state, rule in self._rules.items():
            if state != self._cur_state:
                rule_result = self._rules[state].match(*inputs)
                if rule_result == Rule.IS_TRUE:
                    self.reset_rule_for(self._cur_state)
                    self._cur_state = state
                    break
                elif rule_result == Rule.IS_FALSE:
                    self.reset_rule_for(self._cur_state)

        return rule_result == Rule.IS_TRUE

    def reset_rule_for(self, state):
        self._rules[state].reset()

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

    def __repr__(self):
        return str(self._rules)


class BinaryStateMachine(StateMachine):
    def __init__(self, prefix, rule):
        states_with_rules = {
            'high': rule,
            'stop': Not(rule)
        }
        StateMachine.__init__(self, prefix, ['high', 'stop'], states_with_rules, 'stop')

    def is_high(self):
        return self._cur_state == 'high' or 'high' in self._cur_state


class OldBinaryStateMachine(StateMachine):
    def __init__(self, prefix, rule):
        states_with_rules = {
            'start': rule,
            'stop': Not(rule)
        }
        StateMachine.__init__(self, prefix, ['start', 'stop'], states_with_rules, 'stop')

    def is_high(self):
        return self._cur_state == 'start' or 'start' in self._cur_state


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


class OldPoseStateMachine(OldBinaryStateMachine):
    def __init__(self, prefix, rule):
        OldBinaryStateMachine.__init__(self, prefix, rule)
        self._rules['start'] = And(
            self._rules['start'],
            All(('engaged', 1))
        )
        self._rules['stop'] = Not(
            self._rules['start']
        )
