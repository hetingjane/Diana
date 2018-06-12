from abc import ABCMeta, abstractmethod

from components.fusion.automata.constraint import Constraint


class Rule:
    """
    Rule allowing to check one or more constraint satisfaction
    IMPORTANT: A Rule is never reset automatically (the constraints are, though)
    """
    __metaclass__ = ABCMeta

    IS_TRUE = 2
    MATCHED = 1
    IS_FALSE = 0

    def __init__(self, *spec, **options):
        self._spec = spec
        self._constraints = Constraint.read(*spec)
        invert = options['invert'] if 'invert' in options else False
        if invert:
            for constraint in self._constraints:
                constraint.invert()

    @abstractmethod
    def match(self, *inputs):
        pass

    @abstractmethod
    def inverted(self):
        pass

    def reset(self):
        for constraint in self._constraints:
            constraint.reset()

    def __repr__(self):
        combined_constraints = ', '.join(map(str, self._constraints))
        if len(self._constraints) == 1:
            return combined_constraints
        else:
            return '{}({})'.format(self.__class__.__name__, combined_constraints)


class Any(Rule):
    """
    Any rule is true when any of the constraints is satisfied
    Any rule is matched when some of the constraints are matched
    Any rule is false when none of the constraints are matched
    """

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

    def inverted(self):
        return All(*self._spec, invert=True)


class All(Rule):
    """
    All rule is true when all the constraints are satisfied
    All rule is matched when some of the constraints are matched
    All rule is false when none of the constraints are matched
    """
    def match(self, *inputs):
        assert len(inputs) > 0
        all_satisfied = True
        some_names_matched = False
        for constraint in self._constraints:
            result = constraint.input(*inputs)
            some_names_matched = some_names_matched or (result in [Constraint.MATCHED_NAMES, Constraint.SATISFIED])
            all_satisfied = all_satisfied and (result == Constraint.SATISFIED)

        if all_satisfied:
            return Rule.IS_TRUE
        elif some_names_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE

    def inverted(self):
        return Any(*self._spec, invert=True)


class MetaRule(Rule):

    __metaclass__ = ABCMeta

    def __init__(self, *rules):
        self._rules = rules

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
        some_matched = True

        for rule in self._rules:
            result = rule.match(*inputs)
            all_true = all_true and (result == Rule.IS_TRUE)
            some_matched = some_matched or result in [Rule.MATCHED, Rule.IS_TRUE]

        if all_true:
            return Rule.IS_TRUE
        elif some_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE

    def inverted(self):
        rules = []
        for rule in self._rules:
            rules.append(rule.inverted())
        return Or(*rules)


class Or(MetaRule):
    def __init__(self, *rules):
        assert len(rules) > 1
        MetaRule.__init__(self, *rules)

    def match(self, *inputs):
        some_true = False
        some_matched = False
        for rule in self._rules:
            result = rule.match(*inputs)
            some_true = some_true or result == Rule.IS_TRUE
            some_matched = some_matched or result in [Rule.MATCHED, Rule.IS_TRUE]

        if some_true:
            return Rule.IS_TRUE
        elif some_matched:
            return Rule.MATCHED
        else:
            return Rule.IS_FALSE

    def inverted(self):
        rules = []
        for rule in self._rules:
            rules.append(rule.inverted())
        return And(*rules)


if __name__ == '__main__':
    import csv
    engage_rule = All(('engaged', 1))
    disengage_rule = engage_rule.inverted()

    rules_to_test = [engage_rule, disengage_rule]

    with open('speak_and_point.csv', 'r') as f:
        f.readline()
        reader = csv.reader(f)
        i = 1
        for row in reader:
            for rule in rules_to_test:
                result = rule.match(*row)
                print(rule)
                if result == Rule.MATCHED:
                    result = 'match'
                elif result == Rule.IS_FALSE:
                    result = 'false'
                elif result == Rule.IS_TRUE:
                    result = 'true'
                print("{}:{}:{}\n".format(i, result, ', '.join(row)))
            i += 1

