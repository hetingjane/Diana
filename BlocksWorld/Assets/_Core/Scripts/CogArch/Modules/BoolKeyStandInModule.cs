/* This module lets you set boolean keys on the blackboard when
you press certain physical keys on your keyboard.  It's used as a
stand-in for debugging, or for when the real perception hardware
is not available.

Reads:		(actual physical keyboard)
Writes:		whatever key is set in `blackboardKey`

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoolKeyStandInModule : ModuleBase
{
	[System.Serializable]
	public struct KeyToKeyEntry {
		public KeyCode keyboardKey;
		public string blackboardKey;
	}
	
	public KeyToKeyEntry[] keyMappings;
	
	Dictionary<KeyCode, bool> wasSet = new Dictionary<KeyCode, bool>();

	protected void Start() {
		foreach (var entry in keyMappings) {
			wasSet[entry.keyboardKey] = false;
		}
	}
	

	protected void Update() {
		foreach (var entry in keyMappings) {
			if (Input.GetKey(entry.keyboardKey)) {
				if (!wasSet[entry.keyboardKey]) {
					SetValue(entry.blackboardKey, true, "Set by stand-in key " + entry.keyboardKey);
					wasSet[entry.keyboardKey] = true;
				}
			} else if (wasSet[entry.keyboardKey]) {
				SetValue(entry.blackboardKey, false, "Released stand-in key " + entry.keyboardKey);
				wasSet[entry.keyboardKey] = false;
			}
		}
	}	
	
}
