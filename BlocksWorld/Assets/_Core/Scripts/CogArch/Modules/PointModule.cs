/*
This script makes a humanoid (using a FinalIK FullBodyBipedIK component) 
point at the location specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "point" or "pickUp")
			me:intent:target (Vector3d, position to point at)
Writes:		me:intent:handPosR (Vector3d, desired position of R hand)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class PointModule : ModuleBase
{
	[Tooltip("Reach position is moved a bit in the direction of this transform (typically, the right elbow)")]
	public Transform backoffTowards;
	
	public float smoothTime = 0.1f;
	public float maxSpeed = 20f;
		
	bool isPointing = false;
	
	protected void Update() {
		Vector3 target;
		if (DataStore.GetStringValue("me:intent:action") != "point") {
			if (isPointing) {
				DataStore.ClearValue("me:intent:handPosR");
				isPointing = false;
			}
		} else {
			target = DataStore.GetVector3Value("me:intent:target");
			// Position the hand slightly above the target position, and a bit closer to the agent.
			target += Vector3.up * 0.10f;
			target += (backoffTowards.position - target).normalized * 0.20f;
			SetValue("me:intent:handPosR", target, "pointing");
			isPointing = true;
		}
	}
}
