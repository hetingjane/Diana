import components.fusion.automata.machines as machines
from components.fusion.automata.rules import MetaRule
from components.fusion.automata.statemachines import StateMachine
from components.fusion.conf.postures import left_hand_postures, right_hand_postures


def get_rule_constraints(rule):
    constraints = []
    if type(rule) == MetaRule or issubclass(type(rule), MetaRule):
        for sub_rule in rule._rules:
            constraints.extend(get_rule_constraints(sub_rule))
    else:
        for constraint in rule._constraints:
            for name in constraint._names:
                constraints.extend([name])
    return constraints


def get_blacklist():
    constraints = set()
    for machine in dir(machines):
        obj = eval('machines.' + machine)
        if issubclass(type(obj), StateMachine) or type(obj) == StateMachine:
            for name, rule in obj._rules.items():
                for name, item in rule.items():
                    constraints.update(set(get_rule_constraints(item)))
    postures = {posture for posture in left_hand_postures + right_hand_postures}
    blacklist = constraints.intersection(postures)
    return blacklist