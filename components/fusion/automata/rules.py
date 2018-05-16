from abc import ABCMeta, abstractmethod
from copy import deepcopy

from components.fusion.automata.threshold import Threshold, ThresholdSpecification


class Rule:

    __metaclass__ = ABCMeta

    def __init__(self, *spec):
        self._thresholds = ThresholdSpecification.read(*spec)

    @abstractmethod
    def match(self, *inputs):
        pass

    def reset(self):
        for threshold in self._thresholds:
            threshold.reset()

    def __repr__(self):
        return '{}({})'.format(self.__class__.__name__, ' | '.join(map(str, self._thresholds)))


class Any(Rule):
    def match(self, *inputs):
        assert len(inputs) > 0
        for threshold in self._thresholds:
            result = threshold.input(*inputs)
            if result == Threshold.TRIGGERED:
                self.reset()
                return True
        return False


class All(Rule):
    def match(self, *inputs):
        assert len(inputs) > 0
        all_triggered = True
        for threshold in self._thresholds:
            result = threshold.input(*inputs)
            all_triggered = all_triggered and (result == Threshold.TRIGGERED)

        if all_triggered:
            self.reset()

        return all_triggered


class MetaRule(Rule):
    def __init__(self, *rules):
        self._rules = deepcopy(rules)

    def reset(self):
        for rule in self._rules:
            rule.reset()

    def __repr__(self):
        return '{} ({})'.format(self.__class__.__name__, ' | '.join(map(str, self._rules)))


class And(MetaRule):
    def __init__(self, *rules):
        assert len(rules) > 1
        MetaRule.__init__(self, *rules)

    def match(self, *inputs):
        for rule in self._rules:
            if not rule.match(*inputs):
                self.reset()
                return False
        return True


class Or(MetaRule):
    def __init__(self, *rules):
        assert len(rules) > 1
        MetaRule.__init__(self, *rules)

    def match(self, *inputs):
        for rule in self._rules:
            if rule.match(*inputs):
                self.reset()
                return True
        return False


class Not(MetaRule):
    def __init__(self, rule):
        MetaRule.__init__(self, rule)

    def match(self, *inputs):
        return not self._rules[0].match(*inputs)


if __name__ == '__main__':
    posack = Any(('rh tu', 'lh tu', 4), ('s yes', 1))
    signal = [('rh tu', 'body still'), ('rh tu', 'body still'), ('lh tu', 'body still'), ('lh tu', 'body move front'),
              ('lh tu', 'body still', 's yes')]
    res = None
    for i in signal[:-1]:
        res = posack.match(*i)
    assert res is True
    assert posack.match('body still') is False
    assert posack.match(*signal[-1]) is True

    point = Any(('rh pf', 'lh pf', 2))
    not_point = Not(point)

    signal = [('rh pf', 'body still'), ('lh pf', 'body still')]
    point_res = None
    not_point_res = None
    for i in signal:
        point_res = point.match(*i)
        not_point_res = not_point.match(*i)

    assert point_res is True, point_res
    assert not_point_res is False, not_point_res

    disengage = All(('disengaged', 2))
    res_disengaged = disengage.match('disengaged')
    print(disengage)
    assert res_disengaged is False, res_disengaged

    res_disengaged = disengage.match('disengaged')
    assert res_disengaged is True, res_disengaged
