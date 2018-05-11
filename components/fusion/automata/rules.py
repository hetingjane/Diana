from ..conf.postures import to_vec


class Counter:
    def __init__(self, init_val=0, min_val=None, max_val=None):
        if min_val is not None and min_val > init_val:
            raise ValueError("Initial value must be greater than minimum value")

        if max_val is not None and max_val < init_val:
            raise ValueError("Initial value must be less than maximum value")

        self._init_val = init_val
        self._val = init_val
        self._min_val = min_val
        self._max_val = max_val

    def val(self):
        return self._val

    def min_val(self):
        return self._min_val

    def max_val(self):
        return self._max_val

    def init_val(self):
        return self._init_val

    def has_min_val(self):
        return self._min_val is not None

    def has_max_val(self):
        return self._max_val is not None

    def reset_to_min(self):
        if not self.has_min_val():
            raise NoMinValue()
        self._val = self._min_val

    def reset_to_max(self):
        if not self.has_max_val():
            raise NoMaxValue()
        self._val = self._max_val

    def reset(self):
        self._val = self._init_val

    def inc(self):
        self._val += 1
        if self.has_max_val() and self._val > self._max_val:
            self._val = self._max_val

    def dec(self):
        self._val -= 1
        if self.has_min_val() and self._val < self._min_val:
            self._val = self._min_val

    def at_min(self):
        if not self.has_min_val():
            raise NoMinValue()
        return self._val == self._min_val

    def at_max(self):
        if not self.has_max_val():
            raise NoMaxValue()
        return self._val == self._max_val


class NoMaxValue(Exception):
    message = "Counter does not have a maximum value"


class NoMinValue(Exception):
    message = "Counter does not have a minimum value"


class RuleState:
    def __init__(self, default_threshold, *postures):
        self._thresholds, self._counts = self._get_thresholds_and_counts(default_threshold, postures)

    def _get_thresholds_and_counts(self, default_threshold, *postures):
        # Assume that postures is a list such that an element can be one of these forms
        # 1. posture name eqv. to (posture, default_threshold)
        # 2. a tuple in form of (posture, threshold)
        # 3. a tuple in form of (posture1, posture2, ..., postureN, threshold) which is basically 2. but
        # the postures will share common current threshold value at all times
        poses = []
        counts = {}

        # Initialize norm_postures and cur_counts
        for posture in postures:
            if isinstance(posture, tuple):
                if len(posture) == 2:
                    poses.append(posture)
                    if posture not in counts:
                        counts[posture] = Counter()
                elif len(posture) > 2:
                    count = Counter()
                    for p in posture[:-1]:
                        poses.append((p, posture[-1]))
                        counts[posture] = count
                else:
                    raise ValueError("Input specification is wrong")
            else:
                poses.append((posture, default_threshold))
                counts[posture] = default_threshold

        thresholds = dict(poses)
        return thresholds, counts

    def threshold(self, posture):
        return self._thresholds[posture]

    def cur_count(self, posture):
        return self._counts[posture]

    def reset(self):
        for posture in self._counts.keys():
            self._counts[posture].reset_minimum()


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
