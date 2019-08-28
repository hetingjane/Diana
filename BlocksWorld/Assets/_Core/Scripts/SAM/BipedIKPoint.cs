/*
This script makes a humanoid (using a FinalIK FullBodyBipedIK component) 
point at the location specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "point" or "pickUp")
			me:intent:target (Vector3d, position to point at)
Writes:		(nothing)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class BipedIKPoint : MonoBehaviour
{
	[Tooltip("Reach position is moved a bit in the direction of this transform (typically, the right elbow)")]
	public Transform backoffTowards;
	
	public float smoothTime = 0.1f;
	public float maxSpeed = 20f;
	
	Vector3 relaxedPos;
	Vector3 reachPos;
	Vector3 reachV;
	
	IKEffector reachingHand;
	
	protected void Start() {
		var ik = GetComponent<FullBodyBipedIK>();
		var solver = ik.GetIKSolver() as IKSolverFullBodyBiped;
		reachingHand = solver.rightHandEffector;
		reachingHand.positionWeight = 0;
		relaxedPos = reachPos = reachingHand.bone.position;
	}
	
	protected void Update() {
		Vector3 target;
		if (DataStore.GetStringValue("me:intent:action") != "point") {
			target = relaxedPos;
			reachingHand.positionWeight = Mathf.MoveTowards(reachingHand.positionWeight, 0, 2 * Time.deltaTime);
		} else {
			target = DataStore.GetVector3Value("me:intent:target");
			// Position the hand slightly above the target position, and a bit closer to the agent.
			target += Vector3.up * 0.10f;
			target += (backoffTowards.position - target).normalized * 0.20f;
			reachingHand.positionWeight = Mathf.MoveTowards(reachingHand.positionWeight, 1, 4 * Time.deltaTime);
		}

		// Move smoothly towards the target position.
		reachPos = Vector3.SmoothDamp(reachPos, target, ref reachV, smoothTime, maxSpeed);
		reachingHand.position = reachPos;
	}
}
