from components.fusion.automata.statemachines import BinaryStateMachine, PoseStateMachine
from components.fusion.automata import rules as rules

posack = PoseStateMachine('posack', rules.Any(('rh thumbs up', 'lh thumbs up', 5), ('speak yes', 1)))

negack = PoseStateMachine('negack', rules.Any(('rh thumbs down', 'lh thumbs down', 5), ('speak no', 1)))

engage = BinaryStateMachine('engaged', rules.All(('engaged', 1)))

left_point = PoseStateMachine('left point', rules.And(
    rules.All(('lh point down', 'lh point right', 'lh point front', 5)),
    rules.Or(
        rules.All(('la still', 5)),
        rules.All(('speak there', 'speak here', 'speak this', 'speak that', 1))
    )
))

right_point = PoseStateMachine('left point', rules.And(
    rules.All(('lh point down', 'lh point right', 'lh point front', 5)),
    rules.Or(
        rules.All(('la still', 5)),
        rules.All(('speak there', 'speak here', 'speak this', 'speak that', 1))
    )
))

push_left = PoseStateMachine('push left', rules.And(
    rules.Any(('rh closed left', 'rh open left', 5)),
    rules.All(('ra move left', 5))
))

push_right = PoseStateMachine('push right', rules.And(
    rules.Any(('rh closed left', 'rh open left', 5)),
    rules.All(('ra move left', 5))
))

push_front = PoseStateMachine('push front', rules.Or(
    rules.All(('rh closed front', 5), ('ra move front', 5)),
    rules.All(('lh closed front', 5), ('la move front', 5))
))

if __name__ == '__main__':
    import csv

    sm_to_test = [posack]

    with open('gestures_prady.csv', 'r') as f:
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
