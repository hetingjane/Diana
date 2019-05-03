/*
This module controls Diana's attention.

Reads:		user:isSpeaking (BoolValue)
Writes:		me:attending (StringValue)
			me:alertness (IntValue: 0 = comatose, 7 = normal, 10 = hyperexcited)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionModule : ModuleBase {

	[Header("Configuration")]
	[Tooltip("How long (in sec) before our attention starts to wander")]
	public float attentionWanderAfter = 10f;

	// Last time at which something grabbed our attention:
	float lastSalientTime;

	protected override void Start() {
		base.Start();
		SetValue("me:alertness", 7, "initialization");
		SetValue("me:attending", "none", "initialization");
		lastSalientTime = Time.time;
		DataStore.Subscribe("user:isSpeaking", NoteUserIsSpeaking);
	}

	void NoteUserIsSpeaking(string key, DataStore.IValue value) {
		if ((value as DataStore.BoolValue).val) {
			// User's talking, better pay attention.
			SetValue("me:alertness", 7, "user is speaking");
			SetValue("me:attending", "user", "user is speaking");
			lastSalientTime = Time.time;
		}
	}
	
	protected void Update() {
		// When nothing of interest has happened for a while, change attention
		// to "none"... or if it was already at none, then gradually reduce
		// our alertness level down to some limit.
		if (Time.time - lastSalientTime > attentionWanderAfter) {
			if (DataStore.GetValue("me:attending").ToString() != "none") {
				// Let our attention wander.
				SetValue("me:attending", "none", "I'm bored!");
			} else {
				// Still nothing going on; reduce alertness level (within limits).
				int alertness = DataStore.GetIntValue("me:alertness");
				if (alertness > 3) {
					SetValue("me:alertness", alertness - 1, "still bored");
				}
			}
			lastSalientTime = Time.time;
		}
	}
}
