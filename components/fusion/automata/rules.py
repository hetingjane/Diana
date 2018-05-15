from abc import ABCMeta, abstractmethod
from copy import deepcopy

from .threshold import Threshold, ThresholdSpecification


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
        return 'Rule contains: ' + ' | '.join(map(str, self._thresholds))


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
        for threshold in self._thresholds:
            result = threshold.input(*inputs)
            if result != Threshold.TRIGGERED:
                self.reset()
                return False
        return True


class MetaRule(Rule):
    def __init__(self, *rules):
        assert len(rules) > 1
        self._rules = deepcopy(rules)

    def reset(self):
        for rule in self._rules:
            rule.reset()


class And(MetaRule):
    def match(self, *inputs):
        for rule in self._rules:
            if not rule.match(inputs):
                self.reset()
                return False
        return True


class Or(MetaRule):
    def match(self, *inputs):
        for rule in self._rules:
            if rule.match(inputs):
                self.reset()
                return True
        return False


class Not(MetaRule):
    def __init__(self, rule):
        MetaRule.__init__(self, [rule])

    def match(self, *inputs):
        return not self._rules[0].match(inputs)


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
