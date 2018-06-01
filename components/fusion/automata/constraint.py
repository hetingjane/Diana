from components.fusion.automata.counter import Counter


class Constraint:
    """
    A constraint to match one or more names that share a common threshold
    The behavior of the constraint is as follows:
    If the input matches:
        If the threshold is reached, the constraint is SATISFIED
        Otherwise, the constraint MATCHED_NAMES
    It the input does not match, the constraint is NOT_SATISFIED and is reset.
    A good analogy is that of a cup with a filter.
    If something passes through the filter, the cup gets more filled.
    If something does not pass through the filter, the cup gets emptied.
    If the cup is filled, and something passes through the filter, the cup overflows.
    """

    NOT_SATISFIED = 0
    MATCHED_NAMES = 1
    SATISFIED = 2

    def __init__(self, names, threshold):
        assert len(names) > 0
        self._names = set(names)
        self._counter = Counter(0, min_val=0, max_val=threshold)

    def _has_matching_names(self, *in_names):
        """
        Check if the input names match with the constraint labels
        :param in_names: zero or more input names (or labels)
        :return: True if at least one of the input names is in the constraint labels, else False
        """
        for name in in_names:
            if name in self._names:
                return True
        return False

    def input(self, *in_names):
        """
        Depending on the multi modal input:
        If input matches the constraint labels, the counter for the threshold is incremented i.e. MATCHED_NAMES.
        Then, if the threshold is reached, the constraint is SATISFIED.
        If the input does not match the constraint labels, the counter for the threshold is reset and
        the constraint is NOT_SATISFIED.
        :param in_names: the input labels (or names)
        :return: Any of these mutually exclusive outputs: SATISFIED, MATCHED_NAMES, NOT_SATISFIED
        """
        assert len(in_names) > 0

        if self._has_matching_names(*in_names):
            self._counter.inc()
            satisfied = self._counter.at_max()
            return Constraint.SATISFIED if satisfied else Constraint.MATCHED_NAMES
        else:
            self.reset()
            return Constraint.NOT_SATISFIED

    def reset(self):
        """
        Resets the constraint (in effect, the counter for the threshold for matching names)
        """
        self._counter.reset_to_min()

    def __repr__(self):
        return "[{}; {}, {}]".format(', '.join(self._names), self._counter.val(), self._counter.max_val())

    @staticmethod
    def read(*spec):
        """
        Reads constraint specification into a list of Constraint objects
        :param spec: a list such that an element is of the form ( one or more names, threshold)
        :return:
        """
        constraints = []
        for e in spec:
            constraint = e[-1]
            names = e[:-1]
            constraints.append(Constraint(names, constraint))

        return constraints
