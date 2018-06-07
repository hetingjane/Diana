from components.fusion.automata.statemachines import StateMachine, BinaryStateMachine, PoseStateMachine

from components.fusion.automata import rules as rules

engage = BinaryStateMachine('engage', rules.All(('engaged', 1)))

posack = PoseStateMachine('posack', rules.Any(('rh thumbs up', 'lh thumbs up', 5), ('speak yes', 1)))

negack = PoseStateMachine('negack', rules.Any(('rh thumbs down', 'lh thumbs down', 5), ('speak no', 1)))

wave = PoseStateMachine('wave', rules.Any(('la wave', 'ra wave', 5)))

left_point = PoseStateMachine('left point', rules.And(
    rules.All(('lh point down', 'lh point right', 'lh point front', 5)),
    rules.Or(
        rules.All(('la still', 5)),
        rules.All(('speak there', 'speak here', 'speak this', 'speak that', 1))
    )
))

right_point = PoseStateMachine('right point', rules.And(
    rules.All(('rh point down', 'rh point left', 'rh point front', 5)),
    rules.Or(
        rules.All(('ra still', 5)),
        rules.All(('speak there', 'speak here', 'speak this', 'speak that', 1))
    )
))

left_point_continuous = PoseStateMachine('left point continuous',
                                            rules.All(('lh point down', 'lh point right', 'lh point front', 5)))

right_point_continuous = PoseStateMachine('right point continuous',
                                             rules.All(('rh point down', 'rh point right', 'rh point front', 5)))

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

grab = StateMachine('grab',
                    ['stop', 'high', 'move up high', 'move down high', 'move left high', 'move right high',
                     'move front high', 'move back high'],
                    {
                        'stop': {
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8)),
                                rules.All(('speak grab', 1))
                            )
                        },

                        'high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'move up high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move up high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move down high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 8)),
                                rules.All(('lh claw down', 2), ('la move down', 8))
                            ),
                            'move left high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move down high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 8)),
                                rules.All(('lh claw down', 2), ('la move up', 8))
                            ),
                            'move left high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move left high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move right high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 8)),
                                rules.All(('lh claw down', 2), ('la move right', 8))
                            ),
                            'move front high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move right high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 8)),
                                rules.All(('lh claw down', 2), ('la move left', 8))
                            ),
                            'move front high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move front high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move back high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 8)),
                                rules.All(('lh claw down', 2), ('la move back', 8))
                            )
                        },
                        'move back high': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'high': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front high': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 8)),
                                rules.All(('lh claw down', 2), ('la move front', 8))
                            )
                        }
                    }, 'stop')

# We also need to add engage requirement to each of the states in grab
# and add disengage as an alternative way to enter the stop state
for from_state, to_state in grab.get_transitions():
    cur_rule = grab.get_rule(from_state, to_state)
    if to_state != 'stop':
        new_rule = rules.And(rules.All(('engaged', 1)), cur_rule)
    else:
        new_rule = rules.Or(rules.All(('engaged', 1), invert=True), cur_rule)
    grab.set_rule(from_state, to_state, new_rule)


if __name__ == '__main__':
    import csv

    test_sets = [
        #('others.csv', [engage, wave, posack, negack, push_left, push_right, push_front, push_back]),
        #('point.csv', [engage, wave, right_point_continuous, right_point, left_point_continuous, left_point]),
        ('speak_and_point.csv', [right_point]),
        #('grab.csv', [grab])
    ]

    for g_file, sm_to_test in test_sets:
        print('*' * 20 + '\n')
        print("File: " + g_file + '\n')
        print('*' * 20)

        with open(g_file, 'r') as f:
            reader = csv.DictReader(f)
            i = 1
            for row in reader:
                print("{}:{}".format(i, row.values()))
                for sm in sm_to_test:
                    triggered = sm.input(*row.values())
                    print("{}:{}\n".format(i, sm))
                    if triggered:
                        print("\n{}:{}\n".format(i, sm.get_full_state()))
                i += 1
        print('*' * 20)
