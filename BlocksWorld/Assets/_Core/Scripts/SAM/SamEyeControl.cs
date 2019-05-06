/*
This script reads blackboard values like me:eyes:open, and adjusts SAM's robot eyes accordingly.

Reads:		me:eyes:open (IntValue, 0=closed, 100=wide open)

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SamEyeControl : MonoBehaviour
{
	public SpriteRenderer leftEye;
	public SpriteRenderer rightEye;
	
	protected void LateUpdate() {
		int eyesOpen = DataStore.GetIntValue("me:eyes:open", 70);
		float brightness = 0.3f + eyesOpen * 0.01f;
		Color c = Color.Lerp(Color.black, Color.white, brightness);
		leftEye.color = rightEye.color = c;
	}
}
