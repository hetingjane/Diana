/*
This script makes a humanoid (using a FinalIK FullBodyBipedIK component) 
pick up or put down an object specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "pickUp" or "putDown")
			me:intent:target (Vector3d, position of object to pick up or set down)
			me:intent:targetName (StringValue; name of object to pick up, or to set down on)
Writes:		me:holding (StringValue; name of object we're holding)

Note that this module *also* provides a static Transform reference which
is the object it's currently holding.  That's important, since such a block
is currently inside the avatar hierarchy, and can't be found in the usual
part of the scene hierarchy.

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
		Traversing,	// (i.e., moving to new desired location)
		Lowering,
		Releasing,
		Unreaching
	}
	public State state;
	Transform targetBlock;
	Vector3 curReachTarget;
	
	Transform setDownTarget;		// object to place ours on top of
	Vector3 setDownPos;				// location to place our block in
	
	// Public, static reference to the block currently being held.
	public static Transform heldObject = null;
	
	protected void Start() {
		var ik = GetComponent<FullBodyBipedIK>();
		var solver = ik.GetIKSolver() as IKSolverFullBodyBiped;
		reachingHand = solver.rightHandEffector;
		reachingHand.positionWeight = 0;
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
				setDownPos = curReachTarget;
				
				// Position the hand slightly above the target position.
				curReachTarget += Vector3.up * 0.20f;
				state = State.Reaching;
			}
			break;
		case State.Reaching:
			reachingHand.positionWeight = Mathf.MoveTowards(reachingHand.positionWeight, 1, 4 * Time.deltaTime);
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = DataStore.GetVector3Value("me:intent:target");
				if (targetBlock != null) curReachTarget = targetBlock.position + Vector3.up * 0.08f;				
				state = State.Grabbing;
				reachingHand.positionWeight = 1;
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
				heldObject = targetBlock;
			}
			break;
		case State.Holding:
			if (DataStore.GetStringValue("me:intent:action") == "setDown") {
				Vector3 v = DataStore.GetVector3Value("me:intent:target");
				string name = DataStore.GetStringValue("me:intent:targetName");
				setDownTarget = string.IsNullOrEmpty(name) ? null : grabbableBlocks.Find(name);
				if (setDownTarget != null) {
					// target position specified as block name
					setDownPos = setDownTarget.position + Vector3.up * 0.05f;
					curReachTarget = setDownPos + Vector3.up * 0.3f;
					state = State.Traversing;
					Debug.Log("Traversing to " + setDownTarget.name + " at " + setDownPos);
				} else if (v != default(Vector3)) {
					// target position specified by location
					setDownTarget = null;
					setDownPos = v;
					curReachTarget = v * 0.3f;
					state = State.Traversing;
					Debug.Log("Traversing to " + setDownPos);
				} else {
					// no target position specified; put it back where it came from
					setDownTarget = null;
					curReachTarget = setDownPos + Vector3.up * 0.08f;
					state = State.Lowering;
				}
			}
			break;
		case State.Traversing:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = setDownPos + Vector3.up * 0.08f;
				state = State.Lowering;
			}
			break;		
		case State.Lowering:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				Debug.Log("Drop!");
				// ToDo: open hand animation
				targetBlock.SetParent(grabbableBlocks);
				targetBlock.eulerAngles = Vector3.zero;
				targetBlock.GetComponent<Rigidbody>().isKinematic = false;
				DataStore.SetValue("me:holding", new DataStore.StringValue(""), null, "BipedIKGrab released " + targetBlock.name);
				heldObject = null;
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
			reachingHand.positionWeight = Mathf.MoveTowards(reachingHand.positionWeight, 0, 2 * Time.deltaTime);
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
