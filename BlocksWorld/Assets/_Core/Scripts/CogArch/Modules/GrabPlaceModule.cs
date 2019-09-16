/*
This script makes a humanoid (using a FinalIK FullBodyBipedIK component) 
pick up or put down an object specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "pickUp" or "putDown")
			me:intent:target (Vector3, position of object to pick up or set down)
			me:intent:targetName (StringValue; name of object to pick up, or to set down on)
			me:actual:handPosR (Vector3, current position of hand)
Writes:		me:holding (StringValue; name of object we're holding)
			me:intent:handPosR (Vector3, desired position of hand)

Note that this module *also* provides a static Transform reference which
is the object it's currently holding.  That's important, since such a block
is currently inside the avatar hierarchy, and can't be found in the usual
part of the scene hierarchy.

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GrabPlaceModule : ModuleBase
{
	public float smoothTime = 0.1f;
	public float maxSpeed = 20f;
	
	public Transform grabbableBlocks;
	public Transform hand;
	
	Vector3 relaxedPos;
	Vector3 reachPos;
	Vector3 reachV;

	public event EventHandler<StateChangedEventArgs> StateChanged;
	
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

	public class StateChangedEventArgs
	{
		public State PreviousState
		{
			get;
		}

		public State CurrentState
		{
			get;
		}

		public StateChangedEventArgs(State previousState, State currentState)
		{
			PreviousState = previousState;
			CurrentState = currentState;
		}
	}

	private State currentState;

	public State CurrentState
	{
		get => currentState;

		private set
		{
			State curState = currentState;
			currentState = value;
			StateChanged?.Invoke(this, new StateChangedEventArgs(curState, value));
		}
	}

	Transform targetBlock;
	Vector3 curReachTarget;
	
	Transform setDownTarget;		// object to place ours on top of
	Vector3 setDownPos;				// location to place our block in
	
	// Public, static reference to the block currently being held.
	public static Transform heldObject = null;
	Vector3 holdOffset;
	
	protected void Start() {
		CurrentState = State.Idle;
		relaxedPos = hand.position;
	}
	
	protected void Update() {
		reachPos = DataStore.GetVector3Value("me:actual:handPosR");
		switch (CurrentState) {
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
				CurrentState = State.Reaching;
			}
			break;
		case State.Reaching:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = DataStore.GetVector3Value("me:intent:target");
				if (targetBlock != null) curReachTarget = targetBlock.position + Vector3.up * 0.08f;
				CurrentState = State.Grabbing;
			}
			else if (Input.GetKeyDown(KeyCode.Space)) Debug.Log("reachPos:" + reachPos + " curReachTarget:" + curReachTarget + "  distance: " + Vector3.Distance(reachPos, curReachTarget));
			// ToDo: check for interrupt.
			break;
		case State.Grabbing:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				Debug.Log("Grab!");
				// ToDo: grab hand animation
				targetBlock.GetComponent<Rigidbody>().isKinematic = true;
				heldObject = targetBlock;
				holdOffset = heldObject.transform.position - hand.transform.position;
				CurrentState = State.Lifting;
				curReachTarget = targetBlock.position + Vector3.up * 0.3f;
			}
			break;
		case State.Lifting:
			heldObject.transform.position = hand.transform.position + holdOffset;
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				CurrentState = State.Holding;
				DataStore.SetValue("me:holding", new DataStore.StringValue(targetBlock.name), null, "BipedIKGrab");
			}
			break;
		case State.Holding:
			heldObject.transform.position = hand.transform.position + holdOffset;
			if (DataStore.GetStringValue("me:intent:action") == "setDown") {
				Vector3 v = DataStore.GetVector3Value("me:intent:target");
				string name = DataStore.GetStringValue("me:intent:targetName");
				setDownTarget = string.IsNullOrEmpty(name) ? null : grabbableBlocks.Find(name);
				if (setDownTarget != null) {
					// target position specified as block name
					setDownPos = setDownTarget.position + Vector3.up * 0.05f;
					curReachTarget = setDownPos + Vector3.up * 0.3f;
					CurrentState = State.Traversing;
					Debug.Log("Traversing to " + setDownTarget.name + " at " + setDownPos);
				} else if (v != default(Vector3)) {
					// target position specified by location
					setDownTarget = null;
					setDownPos = v;
					curReachTarget = v + Vector3.up * 0.3f;
					CurrentState = State.Traversing;
					Debug.Log("Traversing to " + setDownPos);
				} else {
					// no target position specified; put it back where it came from
					setDownTarget = null;
					curReachTarget = setDownPos + Vector3.up * 0.08f;
					CurrentState = State.Lowering;
				}
			}
			break;
		case State.Traversing:
			heldObject.transform.position = hand.transform.position + holdOffset;
				Debug.Log($"Distance: {Vector3.Distance(reachPos, curReachTarget)}");
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = setDownPos + Vector3.up * 0.08f;
				CurrentState = State.Lowering;
			}
			break;		
		case State.Lowering:
			heldObject.transform.position = hand.transform.position + holdOffset;
			if (Vector3.Distance(reachPos, curReachTarget) < 0.02f) {
				Debug.Log("Drop!");
				// ToDo: open hand animation
				targetBlock.SetParent(grabbableBlocks);
				targetBlock.eulerAngles = Vector3.zero;
				targetBlock.GetComponent<Rigidbody>().isKinematic = false;
				DataStore.SetValue("me:holding", new DataStore.StringValue(""), null, "BipedIKGrab released " + targetBlock.name);
				heldObject = null;
				CurrentState = State.Releasing;
				curReachTarget = targetBlock.position + Vector3.up * 0.2f;
			}
			break;
		case State.Releasing:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.05f) {
				curReachTarget = relaxedPos;
				CurrentState = State.Unreaching;
			}
			break;	
		case State.Unreaching:
			if (Vector3.Distance(reachPos, curReachTarget) < 0.3f) {
				CurrentState = State.Idle;
				DataStore.ClearValue("me:intent:handPosR");
			}
			break;
		}
		
		if (CurrentState != State.Idle) {
			SetValue("me:intent:handPosR", curReachTarget, CurrentState.ToString());
		}
	}
}
