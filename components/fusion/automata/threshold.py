from .counter import Counter


class Threshold:
    """
    A threshold shared by one or more names
    The behavior of the threshold is as follows:
    If the input matches, the count is incremented.
        If the count matches threshold, it is triggered.
    It the input does not match, it is reset.
    """

    NO_MATCH = 0
    MATCHED = 1
    TRIGGERED = 2

    def __init__(self, names, threshold):
        assert len(names) > 0
        self._names = set(names)
        self._counter = Counter(0, min_val=0, max_val=threshold)

    def input(self, *in_names):
        """
        Triggers the threshold depending on values in the in_names which is the multi-modal input
        If none of the in_names matches the initializing names, it resets.
        If any of them matches, and the count reaches threshold, it is triggered as well as reset
        :param in_names: the input names
        :return: Any of these mutually exclusive outputs:
                 TRIGGERED, if triggered, therefore also reset
                 MATCHED, if not triggered, but there is a match with the input so not reset
                 NO_MATCH, if there is no match with the input i.e. it is reset
        """
        assert len(in_names) > 0

        has_matching_names = len([name for name in in_names if name in self._names]) > 0

        if has_matching_names:
            self._counter.inc()
            triggered = self._counter.at_max()
            if triggered:
                self.reset()
                return Threshold.TRIGGERED
            else:
                return Threshold.MATCHED
        else:
            self.reset()
            return Threshold.NO_MATCH

    def reset(self):
        self._counter.reset_to_min()

    def __repr__(self):
        return "Threshold: {}, {}".format(self._names, self._counter.val())


class ThresholdSpecification:
    def __init__(self):
        raise NotImplementedError("Usage: ThresholdSpecification.read()")

    @staticmethod
    def read(*spec):
        """

        :param spec: a list such that an element is of the form ( one or more names, threshold)
        :return:
        """
        thresholds = []
        for e in spec:
            threshold = e[-1]
            names = e[:-1]
            thresholds.append(Threshold(names, threshold))

        return thresholds


if __name__ == '__main__':
    t = Threshold(['rh tu', 'lh tu'], 2)
    assert t.input('body still') == Threshold.NO_MATCH
    assert t.input('rh tu') == Threshold.MATCHED
    assert t.input('lh tu') == Threshold.TRIGGERED
    assert t.input('rh tu') == Threshold.MATCHED
