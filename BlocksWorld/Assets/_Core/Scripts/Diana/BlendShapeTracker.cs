/* This script makes one SkinnedMeshRenderer copy blend shape weights
from another.  For example, put it on the Eyelashes object of a Fuse
character, and have it track eye-related blendshapes from the Body.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendShapeTracker : MonoBehaviour
{
	public SkinnedMeshRenderer sourceMesh;
	public string[] blendShapesToTrack;
	
	SkinnedMeshRenderer myRenderer;
	Dictionary<int, int> sourceToMyBlendIndex;
	
	protected void Awake() {
		myRenderer = GetComponent<SkinnedMeshRenderer>();
		Debug.Assert(myRenderer != null && sourceMesh != null);
		
		sourceToMyBlendIndex = new Dictionary<int, int>();
		foreach (string s in blendShapesToTrack) {
			int sourceIdx = sourceMesh.sharedMesh.GetBlendShapeIndex(s);
			int myIdx = myRenderer.sharedMesh.GetBlendShapeIndex(s);
			Debug.Assert(sourceIdx >= 0 && myIdx >= 0);
			sourceToMyBlendIndex[sourceIdx] = myIdx;
		}
	}
	
	protected void LateUpdate() {
		foreach (var kv in sourceToMyBlendIndex) {
			myRenderer.SetBlendShapeWeight(kv.Value, sourceMesh.GetBlendShapeWeight(kv.Key));
		}
	}
}
