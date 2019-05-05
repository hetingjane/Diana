/*
This trivial module just makes our agent decide to continuously point
at whatever the user's pointing at.

Reads:		user:isPointing (BoolValue)
			user:pointPos (Vector3Value)
			
Writes:		me:intent:action (StringValue; set to "point")
			me:intent:target (Vector3Value, position to point at)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointAtPointModule : ModuleBase
{
	protected void Update() {
		if (DataStore.GetBoolValue("user:isPointing")) {
			SetValue("me:intent:target", DataStore.GetValue("user:pointPos"), "following user point");
			SetValue("me:intent:action", "point", "following user point");
		} else {
			if (DataStore.GetStringValue("me:intent:action") == "point") {
				SetValue("me:intent:action", "", "user not pointing");
			}
		}
	}
}
