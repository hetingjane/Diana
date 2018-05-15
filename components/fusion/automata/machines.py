from .statemachines import BinaryStateMachine, PoseStateMachine
from .rules import Any, All, Never

posack = PoseStateMachine('posack', Any(('rh thumbs up', 'lh thumbs up', 4), ('head nod', 5), ('speak yes', 1)))

negack = PoseStateMachine('negack', Any(('rh thumbs down', 'lh thumbs down', 4), ('head shake', 5), ('speak no', 1)))

engaged = BinaryStateMachine('engage', All(('engage', 1)))
