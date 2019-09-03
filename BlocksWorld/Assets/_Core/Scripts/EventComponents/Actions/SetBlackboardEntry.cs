using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetBlackboardEntry : MonoBehaviour
{
	public string blackboardKey;
	
	public void SetBoolean(bool value) {
		DataStore.SetValue(blackboardKey, new DataStore.BoolValue(value), null, "Set by " + name);
	}
    
}
