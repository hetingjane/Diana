/*

OBSOLETE (replaced by SelfKnowledge.cs, 2019-11-11).


This trivial module injects a few bits of information about the robot arm itself
onto the blackboard, for use by other modules.

Reads:		(nothing)
Writes:		me.name
			me.voice
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotArmSelfKnowledge : ModuleBase
{
	protected override void Start() {
		base.Start();
		SetValue("me:name", "Botarm", "initial state");
		SetValue("me:voice", "Fred;Microsoft David Desktop;english;en;Daniel", "initial state");
    }
}
