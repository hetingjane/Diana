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
        self._thresholds = ConstraintSpecification.read(*spec)

    @abstractmethod
    def match(self, *inputs):
        pass

    def reset(self):
        for threshold in self._thresholds:
            threshold.reset()

    def __repr__(self):
        return '{}({})'.format(self.__class__.__name__, ' | '.join(map(str, self._thresholds)))


class Any(Rule):
    def match(self, *inputs):
        assert len(inputs) > 0
        any_satisfied = False
        names_matched = False
        for threshold in self._thresholds:
            result = threshold.input(*inputs)
            any_satisfied = any_satisfied or (result == Constraint.SATISFIED)
            names_matched = names_matched or (result in [Constraint.PARTIAL, Constraint.CONTINUE, Constraint.SATISFIED])

        if any_satisfied:
            return Rule.IS_TRUE
        elif names_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE


class All(Rule):
    def match(self, *inputs):
        assert len(inputs) > 0
        all_satisfied = True
        names_matched = False
        for threshold in self._thresholds:
            result = threshold.input(*inputs)
            if result in [Constraint.PARTIAL, Constraint.CONTINUE, Constraint.SATISFIED]:
                names_matched = True
            if result != Constraint.SATISFIED:
                all_satisfied = False

        if all_satisfied:
            return Rule.IS_TRUE
        elif names_matched:
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
        return '{} ({})'.format(self.__class__.__name__, ' | '.join(map(str, self._rules)))


class And(MetaRule):
    def __init__(self, *rules):
        assert len(rules) > 1
        MetaRule.__init__(self, *rules)

    def match(self, *inputs):
        all_true = True
        some_matched = False

        for rule in self._rules:
            result = rule.match(*inputs)
            all_true = all_true and (result == Rule.IS_TRUE)
            some_matched = some_matched or (result in [Rule.MATCHED, Rule.IS_TRUE])

        if all_true:
            return Rule.IS_TRUE
        elif some_matched:
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
            some_matched = some_matched or (result == Rule.MATCHED)

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
        elif result == Rule.IS_FALSE:
            return Rule.IS_TRUE


if __name__ == '__main__':
    import csv
    rules_to_test = [And(All(('engaged', 1)), Any(('rh thumbs up', 'lh thumbs up', 5), ('speak yes', 1)))]
    rules_to_test.append(Not(rules_to_test[0]))

    with open('gestures_prady.csv', 'r') as f:
        f.readline()
        reader = csv.reader(f)
        i = 1
        for row in reader:
            print("{}:{}".format(i, ', '.join(row)))
            for rule in rules_to_test:
                result = rule.match(*row)
                if result == Rule.MATCHED:
                    result = 'match'
                elif result == Rule.IS_FALSE:
                    result = 'false'
                elif result == Rule.IS_TRUE:
                    result = 'true'
                print(result)

            i += 1

