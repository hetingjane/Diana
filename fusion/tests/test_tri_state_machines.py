import pytest

from fusion.automata.tri_state_machines import *
from support.postures import posture_to_vec


disengage = (False, 0, 0)

def get_high_low_stop(trigger, non_trigger):
    high = (True, trigger, non_trigger)
    low = (True, non_trigger, trigger)
    stop = (True, non_trigger, non_trigger)
    return high, low, stop

posack_trigger = posture_to_vec['engage'] | posture_to_vec['rh thumbs up']
posack_non_trigger = posture_to_vec['engage'] | posture_to_vec['rh thumbs down']

posack_high, posack_low, posack_stop = get_high_low_stop(posack_trigger, posack_non_trigger)

@pytest.mark.parametrize("test_input, expected_output", [
    # Excess stops do not trigger a transition from initial state
    ([posack_stop] * 20, ['stop']),
    # Excess lows trigger a transition to low
    ([posack_low] * 8, ['stop', 'low']),
    # Excess highs trigger a transition to low
    ([posack_high] * 8, ['stop', 'high']),
    # Tests the transition stop -> high -> low -> high -> stop
    ([posack_high]*5 + [posack_low]*5 + [posack_high]*5 + [posack_stop]*5, ['stop', 'high', 'low', 'high', 'stop']),
    # Tests the transition stop -> low -> high -> low -> stop
    ([posack_low] * 5 + [posack_high] * 5 + [posack_low] * 5 + [posack_stop] * 5, ['stop', 'low', 'high', 'low', 'stop']),
    # Test the transition stop -> excess high -> partial low -> partial high -> stop
    ([posack_low]*3 + [posack_high]*8 + [posack_low] * 4 + [posack_high]*2 + [posack_stop]*5, ['stop', 'high', 'stop']),
    # Test the transition stop -> excess high -> disengage once -> partial high
    ([posack_high]*6 + [disengage] + [posack_high]*4, ['stop', 'high', 'stop'])
])
def test_posack(test_input, expected_output):
    tsm_posack.reset()
    actual_output = [tsm_posack.get_state().split(' ')[-1]]
    for i in test_input:
        if tsm_posack.input(*i):
            actual_output.append(tsm_posack.get_state().split(' ')[-1])
    assert actual_output == expected_output

point_trigger = posture_to_vec['engage'] | posture_to_vec['RA: still'] | posture_to_vec['rh point front']
point_non_trigger = posture_to_vec['engage'] | posture_to_vec['rh thumbs down']

point_high, point_low, point_stop = get_high_low_stop(point_trigger, point_non_trigger)

@pytest.mark.parametrize("test_input, expected_output", [
    # Excess stops do not trigger a transition from initial state
    ([point_stop]*20, ['stop']),
    # Excess lows trigger a transition to low
    ([point_low]*8, ['stop', 'low']),
    # Excess highs trigger a transition to low
    ([point_high]*8, ['stop', 'high']),
    # Tests the transition stop -> high -> low -> high -> stop
    ([point_high] * 5 + [point_low] * 5 + [point_high] * 5 + [point_stop] * 5, ['stop', 'high', 'low', 'high', 'stop']),
    # Tests the transition stop -> low -> high -> low -> stop
    ([point_low] * 5 + [point_high] * 5 + [point_low] * 5 + [point_stop] * 5, ['stop', 'low', 'high', 'low', 'stop']),
    # Test the transition stop -> excess high -> disengage once -> partial high
    ([point_high]*6 + [disengage] + [point_high]*4, ['stop', 'high', 'stop'])
])
def test_point(test_input, expected_output):
    tsm_right_point_vec.reset()
    actual_output = [tsm_right_point_vec.get_state().split(' ')[-1]]
    for i in test_input:
        if tsm_right_point_vec.input(*i):
            actual_output.append(tsm_right_point_vec.get_state().split(' ')[-1])
    assert actual_output == expected_output
