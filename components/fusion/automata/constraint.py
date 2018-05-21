from components.fusion.automata.counter import Counter


class Constraint:
    """
    A constraint to match one or more names with given threshold
    The behavior of the constraint is as follows:
    If the input matches:
        If the threshold is reached, the constraint is SATISFIED if it wasn't so previously else CONTINUE to be satisfied.
        Otherwise, the constraint is partially matched (i.e. without the threshold)
    It the input does not match, the constraint is NOT_SATISFIED and is reset.
    """

    NOT_SATISFIED = 0
    PARTIAL = 1
    CONTINUE = 2
    SATISFIED = 3

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
                 SATISFIED: the constraint is satisfied but not reset
                 PARTIAL: the constraint is satisfied in terms of input names but the threshold isn't matched
                 CONTINUE: the constraint was previously satisfied and current input does not change that.
                 NOT_SATISFIED: the constraint was not satisfied in terms of matching names
        """
        assert len(in_names) > 0

        has_matching_names = any(map(lambda x: x in self._names, in_names))

        if has_matching_names:
            prev_satisfied = self._counter.at_max()
            if prev_satisfied:
                return Constraint.CONTINUE
            else:
                self._counter.inc()
                satisfied = self._counter.at_max()
                return Constraint.SATISFIED if satisfied else Constraint.PARTIAL
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
