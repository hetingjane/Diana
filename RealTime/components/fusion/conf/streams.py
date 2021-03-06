_stream_ids = {
    "Color": 0x2,   # 2
    "Speech": 0x4,  # 4
    "Audio": 0x8,   # 8
    "Depth": 0x10,  # 16
    "Body": 0x20,   # 32
    "LH": 0x40,     # 64
    "RH": 0x80,     # 128
    "Emotion": 0x200,  # 512
    "Face": 0x400,   #1024
}

_active_streams = frozenset(["LH", "RH", "Body", "Speech", "Emotion"])

_streams = frozenset(_stream_ids.keys())

for s in _active_streams:
    if s not in _stream_ids:
        raise Exception("Active streams configured incorrectly.\n{} not present in stream list.\n".format(s))


def get_stream_id(stream_name):
    return _stream_ids[stream_name]


def is_valid(stream_name):
    return stream_name in _stream_ids


def is_valid_id(stream_id):
    return stream_id in _stream_ids.values()


def is_active(stream_name):
    return stream_name in _active_streams


def is_active_id(stream_id):
    return get_stream_name(stream_id) in _active_streams


def get_stream_name(stream_id):
    for sname, sid in _stream_ids.items():
        if stream_id == sid:
            return sname
    raise InvalidStreamError("Invalid stream id: {}".format(stream_id))


def get_stream_names():
    return _streams


def get_streams_count():
    return len(_streams)


def get_active_streams_count():
    return len(_active_streams)


def all_connected(connected_streams):
    """
    Check if all the streams in active_streams have been connected
    :return: True if all active streams are connected, False otherwise
    """
    return _active_streams.intersection(connected_streams) == _active_streams


class InvalidStreamError(Exception):
    pass
