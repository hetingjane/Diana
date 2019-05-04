/*
Attach this script to the Sprite that shows where the user is pointing.
It will position and show/hide itself according to the following blackboard values:

Reads:		user:isPointing (BoolValue)
			user:pointPos (Vector3Value)
Writes:		(nothing)

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserPointMarker : MonoBehaviour
{
	[Tooltip("Small offset to keep the marker from z-fighting the table")]
	public Vector3 offset = new Vector3(0, 0.01f, 0);
	
	SpriteRenderer renderer;
	
	protected void Awake() {
		renderer = GetComponent<SpriteRenderer>();
	}
	
	protected void LateUpdate() {
		if (!DataStore.GetBoolValue("user:isPointing")) {
			renderer.enabled = false;
		} else {
			renderer.enabled = true;
			Vector3 pos = DataStore.GetVector3Value("user:pointPos");
			transform.position = pos + offset;
		}
	}
    
}
