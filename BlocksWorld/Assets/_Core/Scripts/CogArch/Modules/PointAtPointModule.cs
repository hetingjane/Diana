/*
This demo module allows our agent to look and/or point
at whatever the user's pointing at.

Reads:		me:intent:lookAt (StringValue; looking for "userPoint")
			me:intent:pointAt (StringValue; looking for "userPoint")
			user:isPointing (BoolValue)
			user:pointPos (Vector3Value)
			me:eyes:open (IntValue, 0=closed, 100=wide open)
			
Writes:		me:intent:action (StringValue; set to "point")
			me:intent:target (Vector3Value, position to point at)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Semantics;

public class PointAtPointModule : ModuleBase
{
	public enum Mode {
		Off,
		Pointing,
		Looking
	}

	Mode mode = Mode.Off;
	float eyesClosedTime = 0;

	protected override void Start() {
		base.Start();
		DataStore.Subscribe("me:intent:lookAt", NoteLookAt);
		DataStore.Subscribe("me:intent:pointAt", NotePointAt);
	}
	
	void NoteLookAt(string key, DataStore.IValue value) {
		if (value.ToString() == "userPoint") mode = Mode.Looking;
		else if (mode == Mode.Looking) mode = Mode.Off;
	}

	void NotePointAt(string key, DataStore.IValue value) {
		if (value.ToString() == "userPoint") mode = Mode.Pointing;
		else if (mode == Mode.Pointing) mode = Mode.Off;
	}

	protected void Update() {
		if (DataStore.GetIntValue("me:eyes:open") < 5) eyesClosedTime += Time.deltaTime;
		else eyesClosedTime = 0;
		
		if (mode == Mode.Off) return;
		
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
