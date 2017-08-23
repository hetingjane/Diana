stream_masks = {
    "Color": 0x2,
    "Speech": 0x4,
    "Audio": 0x8,
    "Depth": 0x10,
    "Body": 0x20,
    "LH": 0x40,
    "RH": 0x70,
    "Head": 0x100
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