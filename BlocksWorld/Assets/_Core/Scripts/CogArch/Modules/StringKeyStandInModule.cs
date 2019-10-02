/* This module lets you set string keys on the blackboard when
you press certain physical keys on your keyboard.  It's used as a
stand-in for debugging, or for when the real perception hardware
is not available.

Reads:		(actual physical keyboard)
Writes:		whatever key is set in `blackboardKey`

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringKeyStandInModule : ModuleBase
{
	[System.Serializable]
	public struct KeyToValueEntry {
		public KeyCode keyboardKey;
		public string valueToSet;
	}

	[Header("Configuration")]
	[Tooltip("Blackboard key to set")]
	public string blackboardKey = "user:whatever";
	
	[Tooltip("Value to set when no key is pressed")]
	public string defaultValue = "";
	
	[Tooltip("Key-value pairs")]
	public KeyToValueEntry[] keyMappings;
	
	KeyCode lastKeyUsed = KeyCode.None;
	
	protected void Update() {
		foreach (var entry in keyMappings) {
			if (Input.GetKey(entry.keyboardKey)) {
				if (lastKeyUsed != entry.keyboardKey) {
					SetValue(blackboardKey, entry.valueToSet, "Set by stand-in key " + entry.keyboardKey);
					lastKeyUsed = entry.keyboardKey;
				}
			} else if (lastKeyUsed == entry.keyboardKey) {
				SetValue(blackboardKey, defaultValue, "Released stand-in key " + lastKeyUsed);
				lastKeyUsed = KeyCode.None;
			}
		}
	}	
	
}
