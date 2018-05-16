from components.fusion.automata.statemachines import BinaryStateMachine, PoseStateMachine
from components.fusion.automata.rules import Any, All

posack = PoseStateMachine('posack', Any(('rh thumbs up', 'lh thumbs up', 4), ('head nod', 5), ('speak yes', 1)))

negack = PoseStateMachine('negack', Any(('rh thumbs down', 'lh thumbs down', 4), ('head shake', 5), ('speak no', 1)))

engage = BinaryStateMachine('engage', All(('engage', 1)))
