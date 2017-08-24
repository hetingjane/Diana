stream_masks = {
    "Color": 0x2,   #2
    "Speech": 0x4,  #4
    "Audio": 0x8,   #8
    "Depth": 0x10,  #16
    "Body": 0x20,   #32
    "LH": 0x40,     #64
    "RH": 0x80,     #128
    "Head": 0x100   #256
}

active_streams = ["Body", "LH", "RH", "Head"]

for s in active_streams:
    if s not in stream_masks.keys():
        raise Exception("Active streams configured incoorectly.\n{} not present in stream list.\n".format(s))


def get_stream_id(stream_str):
    return stream_masks[stream_str]


def is_valid(stream_id):
    return stream_id in stream_masks.values()

def get_stream_type(stream_id):
    for s in stream_masks.keys():
        if stream_masks[s] & stream_id != 0:
            return s
    raise KeyError("Invalid stream type")


def get_streams():
    return stream_masks.keys()


def get_streams_count():
    return len(stream_masks)


def get_active_streams():
    return active_streams


def get_active_streams_count():
    return len(active_streams)