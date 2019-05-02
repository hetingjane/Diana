from __future__ import print_function
from collections import deque, namedtuple
from time import sleep

# A named tuple for storing timestamp and data
TimedData = namedtuple('TimedData', ['timestamp', 'data'])


class Synchronizer:
    """
    Synchronizes data received from multiple sources using timestamps associated with each datum.
    Limitations: If some sources generate data faster than the others, eventually they will become out of sync.
    This synchronizer makes no effort to restore synchronization in such a case.
    """
    def __init__(self, names, history=10):
        assert history > 0
        self._max_len = history
        self._qs = dict(zip(names, [deque() for i in range(len(names))]))

        self._sync_point = None
        self._stale_ts = None

    def _warn(self, message):
        print("WARNING: {}: {}".format(self.__class__.__name__, message))

    def feed(self, name, timestamp, data):
        if self._is_stale(timestamp):
            self._warn("'{}' is lagging behind at timestamp {} but stale timestamp is {}".format(name, timestamp, self._stale_ts))
            return

        q = self._qs[name]

        q.append(TimedData(timestamp, data))

        sync_lost_by_adding = False
        sync_lost_by_removing = False
        sync_lost_by_unavailable_data = False

        # If queue for name was empty before the append
        if len(q) == 1:
            sync_lost_by_adding = self._remove_stale_data_for_all_except(name, timestamp)
            self._stale_ts = timestamp
        elif len(q) > self._max_len:
            q.popleft()
            sync_lost_by_removing = self._remove_stale_data_for_all_except(name, q[0].timestamp)
            self._stale_ts = q[0].timestamp
        else:
            sync_lost_by_unavailable_data = self._check_for_empty_queues()

        sync_lost = sync_lost_by_adding or sync_lost_by_removing or sync_lost_by_unavailable_data
        self._sync_point = None if sync_lost else q[0].timestamp

    def _is_stale(self, timestamp):
        return self._stale_ts is not None and timestamp < self._stale_ts

    def _remove_stale_data(self, name, timestamp):
        """
        Remove all data in queue named 'name' which is strictly older than 'timestamp'
        :param name: name of the queue
        :param timestamp: timestamp to compare with
        :return:
        """
        q = self._qs[name]
        while len(q) > 0 and q[0].timestamp < timestamp:
            q.popleft()
        return len(q) == 0

    def _remove_stale_data_for_all_except(self, name, timestamp):
        """
        Remove data in all queues except the one named 'name' which is strictly older than 'timestamp'
        :param name: name of the queue to be excluded
        :param timestamp: timestamp to compare with
        :return:
        """
        sync_lost = False
        for q_name, q in self._qs.items():
            if q_name != name:
                sync_lost_now = self._remove_stale_data(q_name, timestamp)
                sync_lost = sync_lost or sync_lost_now
        return sync_lost

    def _check_for_empty_queues(self):
        """
        Checks if there are any empty queues
        :return: True if an empty queue is found, else False
        """
        for q in self._qs.values():
            if len(q) == 0:
                return True

    def is_synced(self):
        """
        Check if the sync point has been found
        :return: True if the sync point has been found, else False
        """
        return self._sync_point is not None

    def get_synced_data(self):
        """
        Get the latest synchronized data. The user must check beforehand using self.is_synced() before calling this.
        :return: (synchronized timestamp, dict containing data keyed by source names)
        """
        assert self.is_synced(), "get_synced_data() called when the Synchronizer is not synced"
        # Records synced data
        synced_data = {}

        # Marks next sync timestamp, could be None if sync is lost during this operation
        next_sync_point = None

        for name, q in self._qs.items():
            timestamp, data = q.popleft()
            # next sync point is None if sync is lost
            # Note that we relegate checking for the timestamp being common to next get_synced_data() operation
            next_sync_point = None if len(q) == 0 else q[0].timestamp
            synced_data[name] = data
            # Timestamp for all oldest data should be the same
            assert timestamp == self._sync_point, "Missing timestamp {} in {}".format(self._sync_point, name)

        cur_sync_point = self._sync_point
        # Update sync point to the next one
        self._sync_point = next_sync_point
        return cur_sync_point, synced_data

    @property
    def sync_point(self):
        """
        Returns the timestamp corresponding to the sync point
        :return: the timstamp of the sync point
        """
        return self._sync_point

    @property
    def stale_timestamp(self):
        """
        Returns the stale timestmap
        :return: stale timestamp
        """
        return self._stale_ts

    def reset(self):
        """
        Resets the synchronizer
        :return:
        """
        for q in self._qs.values():
            q.clear()
        self._sync_point = None
        self._stale_ts = None
        self._reset_wait_times()

    @property
    def names(self):
        """
        Get names of sources as given in __init__()
        :return: collection of sources' names
        """
        return self._qs.keys()

    def __repr__(self):
        out = ""
        names = sorted(self._qs.keys())
        max_name_len = max(map(len, names))
        fmt_str = "{:" + str(max_name_len) + "}: {} ... {}\n"
        fmt_str_empty = "{:" + str(max_name_len) + "}: empty\n"
        for name in names:
            q = self._qs[name]
            if len(q) == 0:
                out += fmt_str_empty.format(name)
            else:
                out += fmt_str.format(name, q[0].timestamp, q[-1].timestamp if len(q) > 1 else '')
        return out


if __name__ == '__main__':
    from threading import Thread
    from Queue import Queue

    class MockClients:
        _shared_store = Queue()

        class MockClient(Thread):

            def __init__(self, name, frames_per_sec, init_ts=1, data_gen=None):
                Thread.__init__(self)
                self._name = name
                self._wait = 1.0 / frames_per_sec
                self._ts = init_ts - 1
                self._data_gen = data_gen if data_gen is not None else lambda: None

            def get_latest_data(self):
                self._ts += 1
                data = self._data_gen()
                sleep(self._wait)
                return self._name, self._ts, data

            def run(self):
                while True:
                    named_data = self.get_latest_data()
                    MockClients._shared_store.put(named_data)

        def __init__(self, *clients):
            self._clients = [self.MockClient(name, fps) for name, fps in clients]

        def begin(self):
            for client in self._clients:
                client.start()

        def get_data(self):
            return self._shared_store.get(block=True)

    s = Synchronizer(['body', 'rh', 'lh', 'head', 'speech'], history=10)
    mcg = MockClients(('body', 29.0), ('rh', 25.0), ('lh', 25.0), ('head', 25.0), ('speech', 30.0))
    mcg.begin()
    while True:
        name, ts, data = mcg.get_data()
        s.feed(name, ts, data)
        print(s)

        if s.is_synced():
            print(s.get_synced_data())

