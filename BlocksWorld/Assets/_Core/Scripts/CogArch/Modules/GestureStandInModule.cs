/* This module stands in for the visual-based gesture recognizer.
By holding certain keys on the keyboard, the user can put gestures
on the blackboard, allowing us to test the rest of the system
without the full Kinect setup.

Reads:		(actual keyboard)
Writes:		user:gesture (StringValue) (or whatever key is set in `blackboardKey`)

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureStandInModule : ModuleBase
{
	[System.Serializable]
	public struct KeyToGestureEntry {
		public KeyCode keyboardKey;
		public string gesture;
	}

    /// <summary>
    /// Blackboard key to set
    /// </summary>
    private string blackboardKey = "user:gesture";
	
	[Tooltip("Key-gesture pairs")]
	public KeyToGestureEntry[] gestures;
	
	KeyCode lastKeyUsed = KeyCode.None;
	
	protected void Update() {
		foreach (var entry in gestures) {
			if (Input.GetKey(entry.keyboardKey)) {
				if (lastKeyUsed != entry.keyboardKey) {
					SetValue(blackboardKey, entry.gesture, "Set by stand-in key " + entry.keyboardKey);
					lastKeyUsed = entry.keyboardKey;
				}
			} else if (lastKeyUsed == entry.keyboardKey) {
				SetValue(blackboardKey, "", "Released stand-in key " + lastKeyUsed);
				lastKeyUsed = KeyCode.None;
			}
		}
	}	
	
}
