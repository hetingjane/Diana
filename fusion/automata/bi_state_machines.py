from fusion.automata.state_machines import BinaryStateMachine
from fusion.automata.rules import *

bsm_posack = BinaryStateMachine(["posack start", "posack stop"], {
    "posack stop": {
        "posack start": match_any('rh thumbs up', 'lh thumbs up', 'head nod')
    },
    "posack start": {
        "posack stop": mismatch_all('rh thumbs up', 'lh thumbs up', 'head nod')
    }
}, "posack stop")


bsm_negack = BinaryStateMachine(["negack start", "negack stop"], {
    "negack stop": {
        "negack start": match_any('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop', 'head shake')
    },
    "negack start": {
        "negack stop": mismatch_all('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop', 'head shake')
    }
}, "negack stop")


bsm_engage = BinaryStateMachine(["engage start", "engage stop"], {
    "engage stop": {
        "engage start": match_any('engage')
    },
    "engage start": {
        "engage stop": mismatch_all('engage')
    }
}, "engage stop", 1)

bsm_left_point_vec = BinaryStateMachine(["left point start", "left point stop"], {
    "left point stop": {
        "left point start": and_rules(
            match_all('LA: still'),
            match_any('lh point down', 'lh point right', 'lh point front')
        )
    },
    "left point start": {
        "left point stop": or_rules(
            mismatch_any('LA: still'),
            mismatch_all('lh point down', 'lh point right', 'lh point front')
        )
    }
}, "left point stop")

bsm_right_point_vec = BinaryStateMachine(["right point start", "right point stop"], {
    "right point stop": {
        "right point start": and_rules(
            match_all('RA: still'),
            match_any('rh point down', 'rh point left', 'rh point front')
        )
    },
    "right point start": {
        "right point stop": or_rules(
            mismatch_any('RA: still'),
            mismatch_all('rh point down', 'rh point left', 'rh point front')
        )
    }
}, "right point stop")

bsm_left_continuous_point = BinaryStateMachine(["left continuous point start", "left continuous point stop"], {
    "left continuous point stop": {
        "left continuous point start": match_any('lh point down', 'lh point right', 'lh point front')
    },
    "left continuous point start": {
        "left continuous point stop": mismatch_all('lh point down', 'lh point right', 'lh point front')
    }
}, "left continuous point stop", 3)

bsm_right_continuous_point = BinaryStateMachine(["right continuous point start", "right continuous point stop"], {
    "right continuous point stop": {
        "right continuous point start": match_any('rh point down', 'rh point left', 'rh point front')
    },
    "right continuous point start": {
        "right continuous point stop": mismatch_all('rh point down', 'rh point left', 'rh point front')
    }
}, "right continuous point stop", 3)

bsm_grab = BinaryStateMachine(["grab start", "grab stop"], {
    "grab stop": {
        "grab start": match_any('rh claw down', 'lh claw down')
    },
    "grab start": {
        "grab stop": mismatch_all('rh claw down', 'lh claw down')
    }
}, "grab stop")


bsm_grab_move_right = BinaryStateMachine(["grab move right start", "grab move right stop"], {
    "grab move right start": {
        "grab move right stop": and_rules(
            mismatch_any('rh claw down', 'RA: move right'),
            mismatch_any('lh claw down', 'LA: move right')
        )
    },
    "grab move right stop": {
        "grab move right start": or_rules(
            match_all('rh claw down', 'RA: move right'),
            match_all('lh claw down', 'LA: move right')
        )
    }
}, "grab move right stop")


bsm_grab_move_left = BinaryStateMachine(["grab move left start", "grab move left stop"], {
    "grab move left start": {
        "grab move left stop": and_rules(
            mismatch_any('rh claw down', 'RA: move left'),
            mismatch_any('lh claw down', 'LA: move left')
        )
    },
    "grab move left stop": {
        "grab move left start": or_rules(
            match_all('rh claw down', 'RA: move left'),
            match_all('lh claw down', 'LA: move left')
        )
    }
}, "grab move left stop")


bsm_grab_move_up = BinaryStateMachine(["grab move up start", "grab move up stop"], {
    "grab move up start": {
        "grab move up stop": and_rules(
            mismatch_any('rh claw down', 'RA: move up'),
            mismatch_any('lh claw down', 'LA: move up')
        )
    },
    "grab move up stop": {
        "grab move up start": or_rules(
            match_all('rh claw down', 'RA: move up'),
            match_all('lh claw down', 'LA: move up')
        )
    }
}, "grab move up stop")


bsm_grab_move_down = BinaryStateMachine(["grab move down start", "grab move down stop"], {
    "grab move down start": {
        "grab move down stop": and_rules(
            mismatch_any('rh claw down', 'RA: move down'),
            mismatch_any('lh claw down', 'LA: move down')
        )
    },
    "grab move down stop": {
        "grab move down start": or_rules(
            match_all('rh claw down', 'RA: move down'),
            match_all('lh claw down', 'LA: move down')
        )
    }
}, "grab move down stop")


bsm_grab_move_front = BinaryStateMachine(["grab move front start", "grab move front stop"], {
    "grab move front start": {
        "grab move front stop": and_rules(
            mismatch_any('rh claw down', 'RA: move front'),
            mismatch_any('lh claw down', 'LA: move front')
        )
    },
    "grab move front stop": {
        "grab move front start": or_rules(
            match_all('rh claw down', 'RA: move front'),
            match_all('lh claw down', 'LA: move front')
        )
    }
}, "grab move front stop")


bsm_grab_move_back = BinaryStateMachine(["grab move back start", "grab move back stop"], {
    "grab move back start": {
        "grab move back stop": and_rules(
            mismatch_any('rh claw down', 'RA: move back'),
            mismatch_any('lh claw down', 'LA: move back')
        )
    },
    "grab move back stop": {
        "grab move back start": or_rules(
            match_all('rh claw down', 'RA: move back'),
            match_all('lh claw down', 'LA: move back')
        )
    }
}, "grab move back stop")


bsm_grab_move_right_front = BinaryStateMachine(["grab move right front start", "grab move right front stop"], {
    "grab move right front start": {
        "grab move right front stop": and_rules(
            mismatch_any('rh claw down', 'RA: move right front'),
            mismatch_any('lh claw down', 'LA: move right front')
        )
    },
    "grab move right front stop": {
        "grab move right front start": or_rules(
            match_all('rh claw down', 'RA: move right front'),
            match_all('lh claw down', 'LA: move right front')
        )
    }
}, "grab move right front stop")


bsm_grab_move_left_front = BinaryStateMachine(["grab move left front start", "grab move left front stop"], {
    "grab move left front start": {
        "grab move left front stop": and_rules(
            mismatch_any('rh claw down', 'RA: move left front'),
            mismatch_any('lh claw down', 'LA: move left front')
        )
    },
    "grab move left front stop": {
        "grab move left front start": or_rules(
            match_all('rh claw down', 'RA: move left front'),
            match_all('lh claw down', 'LA: move left front')
        )
    }
}, "grab move left front stop")


bsm_grab_move_left_back = BinaryStateMachine(["grab move left back start", "grab move left back stop"], {
    "grab move left back start": {
        "grab move left back stop": and_rules(
            mismatch_any('rh claw down', 'RA: move left back'),
            mismatch_any('lh claw down', 'LA: move left back')
        )
    },
    "grab move left back stop": {
        "grab move left back start": or_rules(
            match_all('rh claw down', 'RA: move left back'),
            match_all('lh claw down', 'LA: move left back')
        )
    }
}, "grab move left back stop")


bsm_grab_move_right_back = BinaryStateMachine(["grab move right back start", "grab move right back stop"], {
    "grab move right back start": {
        "grab move right back stop": and_rules(
            mismatch_any('rh claw down', 'RA: move right back'),
            mismatch_any('lh claw down', 'LA: move right back')
        )
    },
    "grab move right back stop": {
        "grab move right back start": or_rules(
            match_all('rh claw down', 'RA: move right back'),
            match_all('lh claw down', 'LA: move right back')
        )
    }
}, "grab move right back stop")


bsm_push_left = BinaryStateMachine(["push left start", "push left stop"], {
    "push left start": {
        "push left stop": or_rules(
            mismatch_all('rh closed left', 'rh open left'),
            mismatch_any('RA: move left')
        )
    },
    "push left stop": {
        "push left start": and_rules(
            match_any('rh closed left', 'rh open left'),
            match_all('RA: move left')
        )
    }
}, "push left stop")


bsm_push_right = BinaryStateMachine(["push right start", "push right stop"], {
    "push right start": {
        "push right stop": or_rules(
            mismatch_all('lh closed right', 'lh open right'),
            mismatch_any('LA: move right')
        )
    },
    "push right stop": {
        "push right start": and_rules(
            match_any('lh closed right', 'lh open right'),
            match_all('LA: move right')
        )
    }
}, "push right stop")


bsm_push_front = BinaryStateMachine(["push front start", "push front stop"], {
    "push front start": {
        "push front stop": and_rules(
            mismatch_any('rh closed front', 'RA: move front'),
            mismatch_any('lh closed front', 'LA: move front')
        )
    },
    "push front stop": {
        "push front start": or_rules(
            match_all('rh closed front', 'RA: move front'),
            match_all('lh closed front', 'LA: move front')
        )
    }
}, "push front stop")


bsm_push_back = BinaryStateMachine(["push back start", "push back stop"], {
    "push back start": {
        "push back stop": and_rules(
            or_rules(
                mismatch_all('rh open back', 'rh closed back'),
                mismatch_any('RA: move back')
            ),
            or_rules(
                mismatch_all('lh open back', 'lh closed back'),
                mismatch_any('LA: move back')
            ),
            mismatch_all('rh beckon', 'lh beckon')
        )
    },
    "push back stop": {
        "push back start": or_rules(
            and_rules(
                match_any('rh open back', 'rh closed back'),
                match_all('RA: move back')
            ),
            and_rules(
                match_any('lh open back', 'lh closed back'),
                match_all('LA: move back')
            ),
            match_any('rh beckon', 'lh beckon')
        )
    }
}, "push back stop")


bsm_count_one = BinaryStateMachine(["count one start", "count one stop"], {
    "count one stop": {
        "count one start": match_any('rh one front', 'lh one front')
    },
    "count one start": {
        "count one stop": mismatch_all('rh one front', 'lh one front')
    }
}, "count one stop")


bsm_count_two = BinaryStateMachine(["count two start", "count two stop"], {
    "count two stop": {
        "count two start": match_any('rh two front', 'rh two back', 'lh two front', 'lh two back')
    },
    "count two start": {
        "count two stop": mismatch_all('rh two front', 'rh two back', 'lh two front', 'lh two back')
    }
}, "count two stop")


bsm_count_three = BinaryStateMachine(["count three start", "count three stop"], {
    "count three stop": {
        "count three start": match_any('rh three front', 'rh three back', 'lh three front', 'lh three back')
    },
    "count three start": {
        "count three stop": mismatch_all('rh three front', 'rh three back', 'lh three front', 'lh three back')
    }
}, "count three stop")


bsm_count_four = BinaryStateMachine(["count four start", "count four stop"], {
    "count four stop": {
        "count four start": match_any('rh four front', 'lh four front')
    },
    "count four start": {
        "count four stop": mismatch_all('rh four front', 'lh four front')
    }
}, "count four stop")


bsm_count_five = BinaryStateMachine(["count five start", "count five stop"], {
    "count five stop": {
        "count five start": match_any('rh five front', 'lh five front')
    },
    "count five start": {
        "count five stop": mismatch_all('rh five front', 'lh five front')
    }
}, "count five stop")


bsm_arms_apart_X = BinaryStateMachine(["arms apart X start", "arms apart X stop"], {
    "arms apart X stop": {
        "arms apart X start": match_all('LA: apart X', 'RA: apart X')
    },
    "arms apart X start": {
        "arms apart X stop": mismatch_any('LA: apart X', 'RA: apart X')
    }
}, "arms apart X stop")


bsm_arms_together_X = BinaryStateMachine(["arms together X start", "arms together X stop"], {
    "arms together X stop": {
        "arms together X start": match_all('LA: together X', 'RA: together X')
    },
    "arms together X start": {
        "arms together X stop": mismatch_any('LA: together X', 'RA: together X')
    }
}, "arms together X stop")


bsm_arms_apart_Y = BinaryStateMachine(["arms apart Y start", "arms apart Y stop"], {
    "arms apart Y stop": {
        "arms apart Y start": match_all('LA: apart Y', 'RA: apart Y')
    },
    "arms apart Y start": {
        "arms apart Y stop": mismatch_any('LA: apart Y', 'RA: apart Y')
    }
}, "arms apart Y stop")


bsm_arms_together_Y = BinaryStateMachine(["arms together Y start", "arms together Y stop"], {
    "arms together Y stop": {
        "arms together Y start": match_all('LA: together Y', 'RA: together Y')
    },
    "arms together Y start": {
        "arms together Y stop": mismatch_any('LA: together Y', 'RA: together Y')
    }
}, "arms together Y stop")
