/*
This trivial module injects a few bits of information about Diana herself
onto the blackboard, for use by other modules.

Reads:		(nothing)
Writes:		me.name
			me.voice
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DianaSelfKnowledge : ModuleBase
{
	protected void Start() {
		SetValue("me:name", "Diana", "initial state");
		SetValue("me:voice", "Victoria", "initial state");
    }
}
