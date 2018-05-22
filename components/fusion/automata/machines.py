from collections import OrderedDict

from components.fusion.automata.statemachines import StateMachine, PoseStateMachine, OldPoseStateMachine, OldBinaryStateMachine
from components.fusion.automata import rules as rules

posack = PoseStateMachine('posack', rules.Any(('rh thumbs up', 'lh thumbs up', 5), ('speak yes', 1)))

negack = PoseStateMachine('negack', rules.Any(('rh thumbs down', 'lh thumbs down', 5), ('speak no', 1)))

engage = OldBinaryStateMachine('engage', rules.All(('engaged', 1)))

wave = OldPoseStateMachine('wave', rules.Any(('la wave', 'ra wave', 5)))

left_point = PoseStateMachine('left point', rules.And(
    rules.All(('lh point down', 'lh point right', 'lh point front', 5)),
    rules.Or(
        rules.All(('la still', 5)),
        rules.All(('speak there', 'speak here', 'speak this', 'speak that', 1))
    )
))

right_point = PoseStateMachine('right point', rules.And(
    rules.All(('rh point down', 'rh point right', 'rh point front', 5)),
    rules.Or(
        rules.All(('ra still', 5)),
        rules.All(('speak there', 'speak here', 'speak this', 'speak that', 1))
    )
))

left_point_continuous = OldPoseStateMachine('left point continuous', rules.All(('lh point down', 'lh point right', 'lh point front', 5)))

right_point_continuous = OldPoseStateMachine('right point continuous', rules.All(('rh point down', 'rh point right', 'rh point front', 5)))

push_left = PoseStateMachine('push left', rules.All(('rh closed left', 'rh open left', 5), ('ra move left', 5)))

push_right = PoseStateMachine('push right', rules.All(('lh closed right', 'lh open right', 5), ('la move right', 5)))

push_front = PoseStateMachine('push front', rules.Or(
    rules.All(('rh closed front', 5), ('ra move front', 5)),
    rules.All(('lh closed front', 5), ('la move front', 5))
))

push_back = PoseStateMachine('push back', rules.Or(
    rules.All(('rh open back', 'rh closed back', 5), ('ra move back', 5)),
    rules.All(('lh open back', 'lh closed back', 5), ('la move back', 5)),
    rules.All(('rh beckon', 'lh beckon', 5))
))

nevermind = PoseStateMachine('nevermind', rules.Any(
    ('rh stop', 'lh stop', 20), ('speak nevermind', 1)
))

grab_state_machine = StateMachine('grab',
                                  ['stop', 'high', 'move up high', 'move down high', 'move left high', 'move right high',
                                   'move front high', 'move back high'],
                                  OrderedDict(
                                      [('stop', rules.Not(rules.Any(('rh claw down', 5), ('lh claw down', 5)))),
                                       ('high', rules.Or(
                                          rules.All(('rh claw down', 8), ('ra still', 8)),
                                          rules.All(('lh claw down', 8), ('la still', 8))
                                       )),
                                       ('move up high', rules.Or(
                                           rules.All(('rh claw down', 2), ('ra move up', 2)),
                                           rules.All(('lh claw down', 2), ('la move up', 2))
                                       )),
                                       ('move down high', rules.Or(
                                           rules.All(('rh claw down', 2), ('ra move down', 2)),
                                           rules.All(('lh claw down', 2), ('la move down', 2))
                                       )),
                                       ('move left high', rules.Or(
                                           rules.All(('rh claw down', 2), ('ra move left', 2)),
                                           rules.All(('lh claw down', 2), ('la move left', 2))
                                       )),
                                       ('move right high', rules.Or(
                                           rules.All(('rh claw down', 2), ('ra move right', 2)),
                                           rules.All(('lh claw down', 2), ('la move right', 2))
                                       )),
                                       ('move front high', rules.Or(
                                           rules.All(('rh claw down', 2), ('ra move front', 2)),
                                           rules.All(('lh claw down', 2), ('la move front', 2))
                                       )),
                                       ('move back high', rules.Or(
                                           rules.All(('rh claw down', 2), ('ra move back', 2)),
                                           rules.All(('lh claw down', 2), ('la move back', 2))
                                       ))]),
                                  'stop')


if __name__ == '__main__':
    import csv

    sm_to_test = [engage, wave, right_point, right_point_continuous, left_point_continuous, left_point]

    with open('gestures.csv', 'r') as f:
        reader = csv.DictReader(f)
        i = 1
        for row in reader:
            #print("{}:{}".format(i, row.values()))
            for sm in sm_to_test:
                triggered = sm.input(*row.values())
                #print(sm)
                if triggered:
                    print("{}:{}".format(i, sm.get_full_state()))
            i += 1
