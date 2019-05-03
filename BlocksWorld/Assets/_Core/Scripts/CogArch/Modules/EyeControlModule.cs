/*
This module controls the eyes, based on inputs like alertness and attention,
as well as internal state such as the need to blink.

Reads:		me:attending (StringValue)
			me:alertness (IntValue)
			user:isSpeaking (BoolValue)
			me:speech:current (StringValue)
			me:intent:eyesClosed (BoolValue)
Writes:		(none)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeControlModule : ModuleBase {

	public EyePuppet2D eyePuppet;
	
	float targetOpenLevel = 0.7f;
	float eyesOpenSince = 0;
	float nextOpenLimit = 5f;
	float nextSaccade = 0;
	
	bool lookingAtUser = false;
	bool holdingEyesClosed = false;
	
	protected override void Start() {
		base.Start();
		DataStore.Subscribe("me:attending", NoteMeAttending);
		DataStore.Subscribe("me:intent:eyesClosed", NoteMeIntentEyesClosed);
	}

	void NoteMeAttending(string key, DataStore.IValue value) {
		string attending = value.ToString();
		lookingAtUser = (attending == "user");
		if (lookingAtUser && !holdingEyesClosed) targetOpenLevel = 0.7f;
		UpdateDisplayComment();
	}
	
	void NoteMeIntentEyesClosed(string key, DataStore.IValue value) {
		holdingEyesClosed = (value as DataStore.BoolValue).val;
		if (holdingEyesClosed) {
			targetOpenLevel = 0;
		} else {
			targetOpenLevel = 0.7f;
		}
		UpdateDisplayComment();
	}
	
	
	protected void Update() {
		UpdateLids();
		UpdateGaze();		
	}
	
	void UpdateLids() {
		// See how dry the eyes are getting; if it's been too long, start a blink (if we haven't already).
		float openTime = Time.time - eyesOpenSince;
		if (targetOpenLevel > 0.1f && openTime > nextOpenLimit) {
			// OK, that's long enough!  Start a blink.
			targetOpenLevel = 0;
		}

		eyePuppet.lidsOpen = Mathf.MoveTowards(eyePuppet.lidsOpen, targetOpenLevel, Time.deltaTime / 0.25f);
		
		// Once the eye is completely closed, reset our target and next open-limit time.
		if (eyePuppet.lidsOpen <= 0) {
			eyesOpenSince = Time.time;
			if (holdingEyesClosed) return;	// intentionally holding eyes closed; don't open

			// Open eyes (level depends on alertness)
			targetOpenLevel = DataStore.GetIntValue("me:alertness") * 0.1f;
			
			// Select next blink interval generally around 5 seconds.  
			// But we'll blink sooner if speaking, and later if listening.
			nextOpenLimit = Random.Range(3f, 7f);
			if (DataStore.HasValue("me:speech:current")) nextOpenLimit -= Random.Range(1,3);
			if (DataStore.GetBoolValue("user:isSpeaking")) nextOpenLimit += Random.Range(1,3);
			UpdateDisplayComment();
		}		
	}
	
	void UpdateGaze() {
		if (Time.time < nextSaccade) return;
		if (holdingEyesClosed) {
			eyePuppet.lookLeftRight = eyePuppet.lookUpDown = 0;
		} else if (lookingAtUser) {
			// While looking at user, just alternate between several points around the user's face
			// (i.e. straight ahead).
			eyePuppet.convergence = 0.25f;
			switch (Random.Range(0,3)) {
			case 0:		// look at user's left eye
				eyePuppet.lookLeftRight = -0.1f;
				eyePuppet.lookUpDown = 0.05f;
				break;
			case 1:		// look at user's right eye
				eyePuppet.lookLeftRight = 0.1f;
				eyePuppet.lookUpDown = 0.05f;
				break;
			default:	// look at user's chin
				eyePuppet.lookLeftRight = 0;
				eyePuppet.lookUpDown = -0.15f;
				break;
			}
		} else {
			// While not looking at user, alternate between small shifts around the current focus,
			// and big jumps, all at random.
			if (Random.Range(0,100) < 70) {
				// Small shift
				eyePuppet.lookLeftRight += Random.Range(-0.1f, 0.1f);
				eyePuppet.lookUpDown += Random.Range(-0.1f, 0.1f);
				eyePuppet.convergence += Random.Range(-0.03f, 0.03f);
			} else {
				// Big jump
				eyePuppet.lookLeftRight = Random.Range(-0.5f, 0.5f);
				eyePuppet.lookUpDown = Random.Range(-0.6f, 0.4f);
				eyePuppet.convergence = Random.Range(0.1f, 0.3f);
			}
		}
		nextSaccade = Time.time + Random.Range(0.2f, 0.5f);
	}
	
	void UpdateDisplayComment() {
		comment = "Blink time: " + nextOpenLimit + "; lookingAtUser: " + lookingAtUser;
		if (display != null) {
			display.ShowUpdate(null, null, comment);
		}		
	}
}
