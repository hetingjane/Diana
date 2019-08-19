/*
This script reads blackboard values like me:eyes:open, and adjusts SkinnedMeshRenderer
blend shapes ("Blink_Left" and "Blink_Right") accordingly.

Reads:		me:eyes:open (IntValue, 0=closed, 100=wide open)

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyelidMorphs : MonoBehaviour
{
	// List of SkinnedMeshRenderers to change blend shapes on
	List<SkinnedMeshRenderer> renderers;
	
	// Index of the left and right blend shapes to set for each renderer above.
	List<int> blinkLeftIndex;
	List<int> blinkRightIndex;

	protected void Start() {
		renderers = new List<SkinnedMeshRenderer>();
		blinkLeftIndex = new List<int>();
		blinkRightIndex = new List<int>();
		
		foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>()) {
			Mesh mesh = smr.sharedMesh;
			int leftIdx = mesh.GetBlendShapeIndex("Blink_Left");
			int rightIdx = mesh.GetBlendShapeIndex("Blink_Right");
			if (leftIdx >= 0 && rightIdx >= 0) {
				renderers.Add(smr);
				blinkLeftIndex.Add(leftIdx);
				blinkRightIndex.Add(rightIdx);
			}
		}
	}

	protected void LateUpdate() {
		int eyesOpen = DataStore.GetIntValue("me:eyes:open", 70);
		float morphValue = 100 * (1f - eyesOpen / 70f);
		for (int i=0; i<renderers.Count; i++) {
			renderers[i].SetBlendShapeWeight(blinkLeftIndex[i], morphValue);
			renderers[i].SetBlendShapeWeight(blinkRightIndex[i], morphValue);
		}
	}

}
