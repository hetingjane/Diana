/*
This trivial module just makes our agent decide to continuously point
at whatever the user's pointing at.

Reads:		user:isPointing (BoolValue)
			user:pointPos (Vector3Value)
			me:eyes:open (IntValue, 0=closed, 100=wide open)
			
Writes:		me:intent:action (StringValue; set to "point")
			me:intent:target (Vector3Value, position to point at)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointAtPointModule : ModuleBase
{
	float eyesClosedTime = 0;
	protected void Update() {
		if (DataStore.GetIntValue("me:eyes:open") < 5) eyesClosedTime += Time.deltaTime;
		else eyesClosedTime = 0;
		
		if (DataStore.GetBoolValue("user:isPointing")) {
			if (eyesClosedTime > 0.2f) {
				SetValue("me:intent:target", "", "can't see");
				SetValue("me:intent:action", "", "can't see");
			} else {
				SetValue("me:intent:target", DataStore.GetValue("user:pointPos"), "following user point");
				SetValue("me:intent:action", "point", "following user point");
			}
		} else {
			if (DataStore.GetStringValue("me:intent:action") == "point") {
				SetValue("me:intent:action", "", "user not pointing");
			}
		}
	}
}
