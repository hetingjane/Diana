from fusion.automata.state_machines import TriStateMachine
from fusion.automata.rules import *

tsm_engage = TriStateMachine("engage", match_any('engage'), 1)

tsm_posack = TriStateMachine("posack",
                             match_any('rh thumbs up', 'lh thumbs up'))#, 'head nod'))

tsm_negack = TriStateMachine("negack",
                             match_any('rh thumbs down', 'lh thumbs down', 'rh stop', 'lh stop'))#, 'head shake'))

tsm_left_point_vec = TriStateMachine("left point",
                                     and_rules(
                                         match_all('LA: still'),
                                         match_any('lh point down', 'lh point right', 'lh point front')
                                     ))

tsm_right_point_vec = TriStateMachine("right point",
                                      and_rules(
                                          match_all('RA: still'),
                                          match_any('rh point down', 'rh point left', 'rh point front')
                                      ))

tsm_grab_move_up = TriStateMachine("grab move up",
                                   or_rules(
                                       match_all('rh claw down', 'RA: move up'),
                                       match_all('lh claw down', 'LA: move up')
                                   ))

tsm_gram_move_down = TriStateMachine("grab move down",
                                     or_rules(
                                         match_all('rh claw down', 'RA: move down'),
                                         match_all('lh claw down', 'LA: move down')
                                     ))

tsm_grab_move_left = TriStateMachine("grab move left",
                                     or_rules(
                                         match_all('rh claw down', 'RA: move left'),
                                         match_all('lh claw down', 'LA: move left')
                                     ))

tsm_grab_move_right = TriStateMachine("grab move right",
                                      or_rules(
                                          match_all('rh claw down', 'RA: move right'),
                                          match_all('lh claw down', 'LA: move right')
                                      ))

tsm_grab_move_front = TriStateMachine("grab move front",
                                      or_rules(
                                          match_all('rh claw down', 'RA: move front'),
                                          match_all('lh claw down', 'LA: move front')
                                      ))

tsm_grab_move_back = TriStateMachine("grab move back",
                                     or_rules(
                                         match_all('rh claw down', 'RA: move back'),
                                         match_all('lh claw down', 'LA: move back')
                                     ))

tsm_push_left = TriStateMachine("push left",
                                and_rules(
                                    match_any('rh closed left', 'rh open left'),
                                    match_all('RA: move left')
                                ))

tsm_push_right = TriStateMachine("push right",
                                 and_rules(
                                     match_any('lh closed right', 'lh open right'),
                                     match_all('LA: move right')
                                 ))

tsm_push_front = TriStateMachine("push front",
                                 or_rules(
                                     match_all('rh closed front', 'RA: move front'),
                                     match_all('lh closed front', 'LA: move front')
                                 ))

tsm_push_back = TriStateMachine("push back",
                                or_rules(
                                    and_rules(
                                        match_any('rh open back', 'rh closed back'),
                                        match_all('RA: move back')
                                    ),
                                    and_rules(
                                        match_any('lh open back', 'lh closed back'),
                                        match_all('LA: move back')
                                    ),
                                    match_any('rh beckon', 'lh beckon')
                                ))

tsm_count_one = TriStateMachine("count one", match_any('rh one front', 'lh one front'))

tsm_count_two = TriStateMachine("count two", match_any('rh two front', 'rh two back', 'lh two front', 'lh two back'))

tsm_count_three = TriStateMachine("count three",
                                  match_any('rh three front', 'rh three back', 'lh three front', 'lh three back'))

tsm_count_four = TriStateMachine("count four", match_any('rh four front', 'lh four front'))

tsm_count_five = TriStateMachine("count five", match_any('rh five front', 'lh five front'))
