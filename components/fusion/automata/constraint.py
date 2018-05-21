from components.fusion.automata.counter import Counter


class Constraint:
    """
    A constraint to match one or more names with given threshold
    The behavior of the constraint is as follows:
    If the input matches:
        If the threshold is reached, the constraint is SATISFIED
        Otherwise, the constraint MATCHED_NAMES
    It the input does not match, the constraint is NOT_SATISFIED and is reset.
    """

    NOT_SATISFIED = 0
    MATCHED_NAMES = 1
    SATISFIED = 2

    def __init__(self, names, threshold):
        assert len(names) > 0
        self._names = set(names)
        self._counter = Counter(0, min_val=0, max_val=threshold)

    def has_matching_names(self, *in_names):
        for name in in_names:
            if name in self._names:
                return True
        return False

    def input(self, *in_names):
        """
        Triggers the threshold depending on values in the in_names which is the multi-modal input
        If none of the in_names matches the initializing names, it resets.
        If any of them matches, and the count reaches threshold, it is triggered as well as reset
        :param in_names: the input names
        :return: Any of these mutually exclusive outputs:
        """
        assert len(in_names) > 0

        if self.has_matching_names(*in_names):
            self._counter.inc()
            satisfied = self._counter.at_max()
            return Constraint.SATISFIED if satisfied else Constraint.MATCHED_NAMES
        else:
            self.reset()
            return Constraint.NOT_SATISFIED

    def reset(self):
        self._counter.reset_to_min()

    def __repr__(self):
        return "[{}; {}, {}]".format(', '.join(self._names), self._counter.val(), self._counter.max_val())


class ConstraintSpecification:
    def __init__(self):
        raise NotImplementedError("Usage: ConstraintSpecification.read()")

    @staticmethod
    def read(*spec):
        """

        :param spec: a list such that an element is of the form ( one or more names, threshold)
        :return:
        """
        constraints = []
        for e in spec:
            constraint = e[-1]
            names = e[:-1]
            constraints.append(Constraint(names, constraint))

        return constraints
