from ..conf.postures import to_vec, from_vec
from .counter import Counter


class Threshold:
    def __init__(self, threshold, *names):
        assert len(names) > 0
        self._names = set(names)
        self._counter = Counter(0, min_val=0, max_val=threshold)

    def input(self, name):
        if name in self._names:
            self._counter.inc()
            if self._counter.at_max():
                self._counter.reset_to_min()
                return True
            return False
        else:
            self._counter.reset()

    @staticmethod
    def read_spec(default_threshold, spec):
        # Assume that `spec` is a list such that an element can be one of these:
        # 1. just a string `name` which is equivalent to (name, default_threshold)
        # 2. a tuple in form of (name, threshold)
        # 3. a tuple in form of (name1, name2, ..., nameN, threshold) to indicated shared threshold

        read_spec = []

        for e in spec:
            if isinstance(e, tuple):
                if len(e) >= 2:
                    read_spec.append(Threshold(e[-1], *e[:-1]))
                else:
                    raise ValueError("Input specification is invalid")
            else:
                read_spec.append(Threshold(default_threshold, e))

        return read_spec


class RuleState:
    def __init__(self, default_threshold, *postures):
        self._thresholds = Threshold.read_spec(default_threshold, postures)




def match_any(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    at least one of the postures matches with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """
    posture_vecs = [to_vec[posture] for posture in postures]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) == v:
                return True
        return False

    return f


def match_any_v2(default_threshold=1, *postures):

    state = RuleState(default_threshold, postures)

    def f(in_sym):
        for p in postures:
            p_v = to_vec[p]
            count = state.cur_count(p)
            if (in_sym & p_v) == p_v:
                count += 1
                if count > state.threshold(p):
                    state.reset()
                    return True
            else:
                count.reset()

        return False

    return f


def match_all(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    all of the postures match with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """
    posture_vecs = [to_vec[posture] for posture in postures]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) != v:
                return False
        return True

    return f


def mismatch_any(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    at least one of the postures does not match with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """

    posture_vecs = [to_vec[posture] for posture in postures]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) != v:
                return True
        return False

    return f


def mismatch_all(*postures):
    """
    Returns a rule to match one or more postures with the input mask that is True when
    none of the postures matches with the input mask, else False
    :param postures: One or more postures as strings
    :return: Rule to match the input postures with the input mask
    """
    posture_vecs = [to_vec[posture] for posture in postures]

    def f(in_sym):
        for v in posture_vecs:
            if (in_sym & v) == v:
                return False
        return True

    return f


# Meta rule for ANDing
def and_rules(*rules):
    """
    Returns a rule that computes the logical AND of the input rules with the input mask
    :param rules: One or more boolean rules
    :return: Rule that is logical AND of the input boolean rules
    """
    def f(in_sym):
        for rule in rules:
            if not rule(in_sym):
                return False
        return True

    return f


# Meta rule for ORing
def or_rules(*rules):
    """
    Returns a rule that computes the logical OR of the input rules with the input mask
    :param rules: One or more boolean rules
    :return: Rule that is logical OR of the input boolean rules
    """
    def f(in_sym):
        for rule in rules:
            if rule(in_sym):
                return True
        return False

    return f
