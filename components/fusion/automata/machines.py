from components.fusion.automata.statemachines import StateMachine, BinaryStateMachine, PoseStateMachine

from components.fusion.automata import rules as rules

engage = BinaryStateMachine('engage', rules.All(('engaged', 1)))

posack = PoseStateMachine('posack', rules.Any(('rh thumbs up', 'lh thumbs up', 5)))

negack = PoseStateMachine('negack', rules.Any(('rh thumbs down', 'lh thumbs down', 5)))

wave = PoseStateMachine('wave', rules.Any(('la wave', 'ra wave', 5)))

left_point = PoseStateMachine('left point',
                              rules.All(('lh point down', 'lh point right', 'lh point front', 5), ('la still', 5)))

right_point = PoseStateMachine('right point',
                               rules.All(('rh point down', 'rh point left', 'rh point front', 5), ('ra still', 5)))

left_point_continuous = PoseStateMachine('left point continuous',
                                         rules.All(('lh point down', 'lh point right', 'lh point front', 5)))

right_point_continuous = PoseStateMachine('right point continuous',
                                          rules.All(('rh point down', 'rh point right', 'rh point front', 5)))

push_left = PoseStateMachine('push left', rules.All(('rh closed left', 'rh open left', 5), ('ra move left', 5)))

push_servo_left = PoseStateMachine('push servo left', rules.All(('rh closed left', 'rh open left', 5), ('ra servo', 5)))

push_right = PoseStateMachine('push right', rules.All(('lh closed right', 'lh open right', 5), ('la move right', 5)))

push_servo_right = PoseStateMachine('push servo right', rules.All(('lh closed right', 'lh open right', 5), ('la servo', 5)))

push_front = PoseStateMachine('push front', rules.Or(
    rules.All(('rh closed front', 5), ('ra move front', 5)),
    rules.All(('lh closed front', 5), ('la move front', 5))
))

push_back = PoseStateMachine('push back', rules.Or(
    rules.All(('rh open back', 'rh closed back', 5), ('ra move back', 5)),
    rules.All(('lh open back', 'lh closed back', 5), ('la move back', 5)),
    rules.All(('rh beckon', 'lh beckon', 5))
))

nevermind = PoseStateMachine('nevermind', rules.All(('rh stop', 'lh stop', 20)))

teaching = StateMachine('teaching',
                        ['stop', 'start', 'succeeded 1', 'succeeded 2', 'succeeded 3', 'succeeded 4', 'succeeded 5', 'succeeded 6'],
                        {
                            'stop': {
                                'start': rules.Any(('rh teaching', 5), ('lh teaching', 5))
                            },

                            'start': {
                                # Stop when neither teaching nor taught for 1 frame for both hands
                                'stop': rules.All(('rh teaching', 1), ('lh teaching', 1),
                                                  ('rh taught gesture 1', 1), ('rh taught gesture 2', 1), ('rh taught gesture 3', 1),
                                                  ('lh taught gesture 4', 1), ('lh taught gesture 5', 1), ('lh taught gesture 6', 1), invert=True),
                                'succeeded 1': rules.All(('rh taught gesture 1', 1)),
                                'succeeded 2': rules.All(('rh taught gesture 2', 1)),
                                'succeeded 3': rules.All(('rh taught gesture 3', 1)),
                                'succeeded 4': rules.All(('lh taught gesture 4', 1)),
                                'succeeded 5': rules.All(('lh taught gesture 5', 1)),
                                'succeeded 6': rules.All(('lh taught gesture 6', 1)),
                            },

                            'succeeded 1': {
                                'stop': rules.Always()
                            },

                            'succeeded 2': {
                                'stop': rules.Always()
                            },

                            'succeeded 3': {
                                'stop': rules.Always()
                            },

                            'succeeded 4': {
                                'stop': rules.Always()
                            },

                            'succeeded 5': {
                                'stop': rules.Always()
                            },

                            'succeeded 6': {
                                'stop': rules.Always()
                            },
                        },
                        'stop')

grab = StateMachine('grab',
                    ['stop', 'start', 'move up start', 'move down start', 'move left start', 'move right start',
                     'move front start', 'move back start'],
                    {
                        'stop': {
                            'start': rules.Or(
                                rules.All(('rh claw down', 5), ('ra still', 5)),
                                rules.All(('lh claw down', 5), ('la still', 5))
                            )
                        },

                        'start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'move up start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move up start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'start': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move down start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 8)),
                                rules.All(('lh claw down', 2), ('la move down', 8))
                            ),
                            'move left start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move down start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'start': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 8)),
                                rules.All(('lh claw down', 2), ('la move up', 8))
                            ),
                            'move left start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move left start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'start': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move right start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 8)),
                                rules.All(('lh claw down', 2), ('la move right', 8))
                            ),
                            'move front start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move right start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'start': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 8)),
                                rules.All(('lh claw down', 2), ('la move left', 8))
                            ),
                            'move front start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move front', 2)),
                                rules.All(('lh claw down', 2), ('la move front', 2))
                            ),
                            'move back start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 2)),
                                rules.All(('lh claw down', 2), ('la move back', 2))
                            )
                        },
                        'move front start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'start': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move back start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move back', 8)),
                                rules.All(('lh claw down', 2), ('la move back', 8))
                            )
                        },
                        'move back start': {
                            'stop': rules.All(('rh claw down', 5), ('lh claw down', 5), invert=True),
                            'start': rules.Or(
                                rules.All(('rh claw down', 8), ('ra still', 8)),
                                rules.All(('lh claw down', 8), ('la still', 8))
                            ),
                            'move up start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move up', 2)),
                                rules.All(('lh claw down', 2), ('la move up', 2))
                            ),
                            'move down start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move down', 2)),
                                rules.All(('lh claw down', 2), ('la move down', 2))
                            ),
                            'move left start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move left', 2)),
                                rules.All(('lh claw down', 2), ('la move left', 2))
                            ),
                            'move right start': rules.Or(
                                rules.All(('rh claw down', 2), ('ra move right', 2)),
                                rules.All(('lh claw down', 2), ('la move right', 2))
                            ),
                            'move front start': rules.Or(
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
        #('speak_and_point.csv', [right_point]),
        ('grab.csv', [grab])
    ]

    detailed = True

    for g_file, sm_to_test in test_sets:
        print('*' * 20)
        print("\nFile: " + g_file, end='\n\n')
        print('*' * 20)

        with open(g_file, 'r') as f:
            reader = csv.DictReader(f)
            i = 2
            for row in reader:
                if detailed:
                    print("{}:{}".format(i, list(row.values())))
                for sm in sm_to_test:
                    triggered = sm.input(*row.values())
                    if detailed:
                        print("{}:{}".format(i, sm), end='\n\n')
                    if triggered:
                        print("\n{}:{}".format(i, sm.get_full_state()), end='\n\n')
                i += 1
        print('*' * 20)
