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
	
	[Tooltip("Sprite image, color, and rotation to use when the pointer position is valid")]
	public Sprite validPointIndicator;
	public Color validColor = new Color(0.5f, 0, 1f);
	public Vector3 validEulerAngles = new Vector3(-90, 0, 0);
	
	[Tooltip("Sprite image, color, and rotation to use when the pointer position is not valid")]
	public Sprite invalidPointIndicator;
	public Color invalidColor = Color.white;
	public Vector3 invalidEulerAngles = new Vector3(0, 0, 0);
	
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
			if (DataStore.GetBoolValue("user:pointValid")) {
				if (renderer.sprite != validPointIndicator) {
					renderer.sprite = validPointIndicator;
					renderer.color = validColor;
					transform.localEulerAngles = validEulerAngles;
				}
			} else {
				renderer.sprite = invalidPointIndicator;
				renderer.color = invalidColor;
				transform.localEulerAngles = invalidEulerAngles;
				// (We assign the rotation on every frame, since we really
				// don't want our "No" indicator to rotate.)
			}
		}
	}
    
}
