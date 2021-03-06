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

push_front = PoseStateMachine('push front', rules.Or(
    rules.All(('rh closed front', 5), ('ra move front', 5)),
    rules.All(('lh closed front', 5), ('la move front', 5))
))

servo_back = PoseStateMachine('servo back', rules.All(('rh beckon', 'lh beckon', 8)))

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

push_servo_left = StateMachine('', ['push servo left stop', 'probable servo left', 'probable push left',
                                     'servo left start', 'push left start',
                                     'servo left stop', 'push left stop'],
                                {
                                    'push servo left stop': {
                                        'probable servo left': rules.All(('engaged', 1), ('rh closed left', 'rh open left', 5), ('ra servo', 10)),
                                        'probable push left': rules.All(('engaged', 1), ('rh closed left', 'rh open left', 5), ('ra move left', 5))
                                    },
                                    'probable servo left': {
                                        'servo left start': rules.All(('engaged', 1), ('rh closed left', 'rh open left', 2), ('ra servo', 2)),
                                        'probable push left': rules.All(('engaged', 1), ('rh closed left', 'rh open left', 3), ('ra move left', 3)),
                                        'push servo left stop': rules.Or(
                                            rules.All(('ra servo', 4), ('ra move left', 4), invert=True),
                                            rules.All(('engaged', 1), invert=True)
                                        )
                                    },
                                    'probable push left': {
                                        'push left start': rules.And(
                                            rules.All(('engaged', 1)),
                                            rules.Or(
                                                rules.All(('rh closed left', 'rh open left', 5), ('ra still', 5)),
                                                rules.All(('rh closed left', 'rh open left', 5), invert=True)
                                            )
                                        ),
                                        'servo left start': rules.All(('engaged', 1), ('rh closed left', 'rh open left', 5), ('ra servo', 10)),
                                        'push servo left stop': rules.Or(
                                            rules.All(('engaged', 1), invert=True),
                                            rules.And(
                                                rules.All(('ra move left', 10), invert=True),
                                                rules.All(('ra still', 5), invert=True),
                                                rules.All(('ra servo', 10), invert=True)
                                            )
                                        )
                                    },
                                    'servo left start': {
                                        'servo left stop': rules.Or(
                                            rules.All(('rh closed left', 'rh open left', 6), ('ra servo', 6), invert=True),
                                            rules.All(('engaged', 1), invert=True)
                                        )
                                    },
                                    'push left start': {
                                        'push left stop': rules.Always()
                                    },
                                    'servo left stop': {
                                        'push servo left stop': rules.Always()
                                    },
                                    'push left stop': {
                                        'push servo left stop': rules.Always()
                                    }
                                }, 'push servo left stop')

push_servo_right = StateMachine('', ['push servo right stop', 'probable servo right', 'probable push right',
                                     'servo right start', 'push right start',
                                     'servo right stop', 'push right stop'],
                                {
                                    'push servo right stop': {
                                        'probable servo right': rules.All(('engaged', 1), ('lh closed right', 'lh open right', 5), ('la servo', 10)),
                                        'probable push right': rules.All(('engaged', 1), ('lh closed right', 'lh open right', 5), ('la move right', 5))
                                    },
                                    'probable servo right': {
                                        'servo right start': rules.All(('engaged', 1), ('lh closed right', 'lh open right', 2), ('la servo', 2)),
                                        'probable push right': rules.All(('engaged', 1), ('lh closed right', 'lh open right', 3), ('la move right', 3)),
                                        'push servo right stop': rules.Or(
                                            rules.All(('la servo', 4), ('la move right', 4), invert=True),
                                            rules.All(('engaged', 1), invert=True)
                                        )
                                    },
                                    'probable push right': {
                                        'push right start': rules.And(
                                            rules.All(('engaged', 1)),
                                            rules.Or(
                                                rules.All(('lh closed right', 'lh open right', 5), ('la still', 5)),
                                                rules.All(('lh closed right', 'lh open right', 5), invert=True)
                                            )
                                        ),
                                        'servo right start': rules.All(('engaged', 1), ('lh closed right', 'lh open right', 5), ('la servo', 10)),
                                        'push servo right stop': rules.Or(
                                            rules.All(('engaged', 1), invert=True),
                                            rules.And(
                                                rules.All(('la move right', 10), invert=True),
                                                rules.All(('la still', 5), invert=True),
                                                rules.All(('la servo', 10), invert=True)
                                            )
                                        )
                                    },
                                    'servo right start': {
                                        'servo right stop': rules.Or(
                                            rules.All(('lh closed right', 'lh open right', 6), ('la servo', 6), invert=True),
                                            rules.All(('engaged', 1), invert=True)
                                        )
                                    },
                                    'push right start': {
                                        'push right stop': rules.Always()
                                    },
                                    'servo right stop': {
                                        'push servo right stop': rules.Always()
                                    },
                                    'push right stop': {
                                        'push servo right stop': rules.Always()
                                    }
                                }, 'push servo right stop')


attentive = StateMachine('', ['attentive stop', 'attentive start', 'inattentive left', 'inattentive right'], {
    'attentive stop': {
        'attentive start': rules.All(('engaged', 1), ('attentive', 5))
    },
    'attentive start': {
        'inattentive left': rules.All(('engaged', 1), ('inattentive left', 5)),
        'inattentive right': rules.All(('engaged', 1), ('inattentive right', 5)),
        'attentive stop': rules.All(('engaged', 1), invert=True)
    },
    'inattentive left': {
        'attentive start': rules.All(('engaged', 1), ('attentive', 5)),
        'inattentive right': rules.All(('engaged', 1), ('inattentive right', 5)),
        'attentive stop': rules.All(('engaged', 1), invert=True)
    },
    'inattentive right': {
        'attentive start': rules.All(('engaged', 1), ('attentive', 5)),
        'inattentive left': rules.All(('engaged', 1), ('inattentive left', 5)),
        'attentive stop': rules.All(('engaged', 1), invert=True)
    }
}, 'attentive stop')

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
