/*
This script reads blackboard values like me:eyes:open, and adjusts Morph3D morphs accordingly.

Reads:		me:eyes:open (IntValue, 0=closed, 100=wide open)

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MORPH3D;

public class EyelidMorphs : MonoBehaviour
{
	M3DCharacterManager charMgr;

	protected void Start() {
		charMgr = GetComponent<M3DCharacterManager>();
		Debug.Assert(charMgr != null);
	}

	protected void LateUpdate() {
		int eyesOpen = DataStore.GetIntValue("me:eyes:open", 70);
		float morphValue = (70f - eyesOpen);
		charMgr.SetBlendshapeValue("eCTRLEyesClosed", morphValue);
	}

}
