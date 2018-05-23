from abc import ABCMeta, abstractmethod
from copy import deepcopy

from components.fusion.automata.constraint import Constraint, ConstraintSpecification


class Rule:
    """
    Rule allowing matches with thresholds
    IMPORTANT: A Rule is never reset automatically
    """
    __metaclass__ = ABCMeta

    IS_TRUE = 2
    MATCHED = 1
    IS_FALSE = 0

    def __init__(self, *spec):
        self._constraints = ConstraintSpecification.read(*spec)

    @abstractmethod
    def match(self, *inputs):
        pass

    def reset(self):
        for constraint in self._constraints:
            constraint.reset()

    def __repr__(self):
        return '{}({})'.format(self.__class__.__name__, ', '.join(map(str, self._constraints)))


class Any(Rule):
    def match(self, *inputs):
        assert len(inputs) > 0
        some_satisfied = False
        some_names_matched = False
        for constraint in self._constraints:
            result = constraint.input(*inputs)
            some_satisfied = some_satisfied or (result == Constraint.SATISFIED)
            some_names_matched = some_names_matched or (result in [Constraint.MATCHED_NAMES, Constraint.SATISFIED])

        if some_satisfied:
            return Rule.IS_TRUE
        elif some_names_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE


class All(Rule):
    def match(self, *inputs):
        assert len(inputs) > 0
        all_satisfied = True
        all_names_matched = True
        for constraint in self._constraints:
            result = constraint.input(*inputs)
            all_names_matched = all_names_matched and (result in [Constraint.MATCHED_NAMES, Constraint.SATISFIED])
            all_satisfied = all_satisfied and (result == Constraint.SATISFIED)

        if all_satisfied:
            return Rule.IS_TRUE
        elif all_names_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE


class MetaRule(Rule):
    def __init__(self, *rules):
        self._rules = deepcopy(rules)

    def reset(self):
        for rule in self._rules:
            rule.reset()

    def __repr__(self):
        return '({})'.format((' ' + self.__class__.__name__.lower() + ' ').join(map(str, self._rules)))


class And(MetaRule):
    def __init__(self, *rules):
        assert len(rules) > 1
        MetaRule.__init__(self, *rules)

    def match(self, *inputs):
        all_true = True
        all_matched = True

        for rule in self._rules:
            result = rule.match(*inputs)
            all_true = all_true and (result == Rule.IS_TRUE)
            all_matched = all_matched and (result in [Rule.MATCHED, Rule.IS_TRUE])

        if all_true:
            return Rule.IS_TRUE
        elif all_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE


class Or(MetaRule):
    def __init__(self, *rules):
        assert len(rules) > 1
        MetaRule.__init__(self, *rules)

    def match(self, *inputs):
        some_matched = False
        for rule in self._rules:
            result = rule.match(*inputs)
            if result == Rule.IS_TRUE:
                return Rule.IS_TRUE
            if result == Rule.MATCHED:
                some_matched = True

        return Rule.MATCHED if some_matched else Rule.IS_FALSE


class Not(MetaRule):
    def __init__(self, rule):
        MetaRule.__init__(self, rule)

    def match(self, *inputs):
        result = self._rules[0].match(*inputs)
        if result == Rule.IS_TRUE:
            return Rule.IS_FALSE
        elif result == Rule.MATCHED:
            return Rule.MATCHED
        else:
            return Rule.IS_TRUE


if __name__ == '__main__':
    import csv
    engage_rule = All(('engaged', 1))
    point_rule = And(
        All(('rh point down', 'rh point right', 'rh point front', 5)),
        Or(
            All(('ra still', 5)),
            All(('speak there', 'speak here', 'speak this', 'speak that', 1))
        )
    )

    point_rule = And(engage_rule, point_rule)

    rules_to_test = [point_rule]

    with open('point.csv', 'r') as f:
        f.readline()
        reader = csv.reader(f)
        i = 1
        for row in reader:
            for rule in rules_to_test:
                result = rule.match(*row)
                if result == Rule.MATCHED:
                    result = 'match'
                elif result == Rule.IS_FALSE:
                    result = 'false'
                elif result == Rule.IS_TRUE:
                    result = 'true'
                print("{}:{}:{}".format(i, result, ', '.join(row)))
                print(rule)
            i += 1

