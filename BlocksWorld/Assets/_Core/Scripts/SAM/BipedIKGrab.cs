/*
This script makes a humanoid (using a FinalIK FullBodyBipedIK component) 
pick up or put down an object specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "pickUp" or "putDown")
			me:intent:target (Vector3d, position of object to pick up)
			me:intent:targetName (string, name of object to pick up)
Writes:		(nothing)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class BipedIKGrab : MonoBehaviour
{
	public float smoothTime = 0.1f;
	public float maxSpeed = 20f;
	
	public Transform grabbableBlocks;
	
	Vector3 relaxedPos;
	Vector3 reachPos;
	Vector3 reachV;
	
	IKEffector reachingHand;
	
	public enum State {
		Idle,
		Reaching,
		Grabbing,
		Lifting,
		Holding,
		Lowering,
		Releasing,
		Unreaching
	}
	public State state;
	Transform targetBlock;
	Vector3 curReachTarget;
	Vector3 originalPos;		// position at which we picked up the object
	
	protected void Start() {
		var ik = GetComponent<FullBodyBipedIK>();
		var solver = ik.GetIKSolver() as IKSolverFullBodyBiped;
		reachingHand = solver.rightHandEffector;
		relaxedPos = curReachTarget = reachPos = reachingHand.bone.position;
		state = State.Idle;
	}
	
	protected void Update() {
		switch (state) {
		case State.Idle:
			if (DataStore.GetStringValue("me:intent:action") == "pickUp") {
				curReachTarget = DataStore.GetVector3Value("me:intent:target");
				targetBlock = grabbableBlocks.Find(DataStore.GetStringValue("me:intent:targetName"));
				if (targetBlock != null) {
					curReachTarget = targetBlock.position;
				} else {
					// ToDo: if targetBlock not found or not specified, then pick the closest
					// block to curReachTarget.
				}
				originalPos = curReachTarget;
				
				// Position the hand slightly above the target position.
				curReachTarget += Vector3.up * 0.20f;
				state = State.Reaching;
			}
			break;
		case State.Reaching:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = DataStore.GetVector3Value("me:intent:target");
				if (targetBlock != null) curReachTarget = targetBlock.position + Vector3.up * 0.08f;				
				state = State.Grabbing;
			}
			// ToDo: check for interrupt.
			break;
		case State.Grabbing:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				Debug.Log("Grab!");
				// ToDo: grab hand animation
				targetBlock.SetParent(reachingHand.bone);
				targetBlock.GetComponent<Rigidbody>().isKinematic = true;
				state = State.Lifting;
				curReachTarget = targetBlock.position + Vector3.up * 0.3f;
			}
			break;
		case State.Lifting:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				state = State.Holding;
				DataStore.SetValue("me:holding", new DataStore.StringValue(targetBlock.name), null, "BipedIKGrab");
			}
			break;
		case State.Holding:
			if (DataStore.GetStringValue("me:intent:action") == "setDown") {
				curReachTarget = originalPos + Vector3.up * 0.08f;
				state = State.Lowering;
			}
			break;
		case State.Lowering:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				Debug.Log("Drop!");
				// ToDo: open hand animation
				targetBlock.SetParent(grabbableBlocks);
				targetBlock.GetComponent<Rigidbody>().isKinematic = false;
				state = State.Releasing;
				curReachTarget = targetBlock.position + Vector3.up * 0.2f;
			}
			break;
		case State.Releasing:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = relaxedPos;
				state = State.Unreaching;
			}
			break;	
		case State.Unreaching:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.1f) {
				state = State.Idle;
			}
			break;
		}
		
		// Move smoothly towards the target position.
		reachPos = Vector3.SmoothDamp(reachPos, curReachTarget, ref reachV, smoothTime, maxSpeed);
		reachingHand.position = reachPos;
	}
}
