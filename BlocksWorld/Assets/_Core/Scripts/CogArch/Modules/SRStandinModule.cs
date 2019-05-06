/* This module is a stand-in for the normal low-level speech recognition (SR)
module.  It gets its input from a text input field in the UI, and presents that
to the rest of the system just as if it were transcribed speech.

Reads:		(nothing)
Writes:		user:isSpeaking (BoolValue)
			user:speech (StringValue)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SRStandinModule : ModuleBase {

	[Header("Configuration")]
	[Tooltip("How long (in sec) after the user stops typing do we report that they're no longer speaking")]
	public float pauseTimeToConsiderDone = 2f;

	string lastText;
	float lastChangeTime;

	public void HandleValueChanged(string newText) {
		lastChangeTime = Time.time;
		lastText = newText;
		SetValue("user:isSpeaking", true, "Text change detected");
	}
	
	public void HandleEndEdit(string text) {
		SetValue("user:speech", text, "End-edit detected");
	}

	
	protected void Update() {
		if (Time.time - lastChangeTime > pauseTimeToConsiderDone) {
			SetValue("user:isSpeaking", false, "Pause time past threshold");
			if (!string.IsNullOrEmpty(lastText)) {
				SetValue("user:speech", lastText, "Last received speech");
			}
		}
	}
	
}
