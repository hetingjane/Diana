stream_masks = {
    "Body": 0x1,
    "LH": 0x2,
    "RH": 0x4
}


def get_stream_id(stream_str):
    return stream_masks[stream_str]


def is_valid(stream_id):
    if stream_id in stream_masks.values():
        return True
    return False


def get_stream_type(stream_id):
    for s in stream_masks.keys():
        if stream_masks[s] & stream_id != 0:
            return s
    raise KeyError("Invalid stream type")


def get_stream_count():
    return len(stream_masks)