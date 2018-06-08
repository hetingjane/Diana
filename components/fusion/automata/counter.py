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
