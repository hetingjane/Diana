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
using UnityEngine;

using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

public class GrabPlaceModule : ModuleBase
{    
	/// <summary>
	/// Reference to the manipulable objects in the scene.
	/// Only these will be searched when an object is referred by name.
	/// </summary>
	public Transform grabbableBlocks;

	/// <summary>
	/// Reference to the animator attached to the humanoid character
	/// </summary>
	[Tooltip("Drag drop the character model here")]
	public Animator animator;
	
	/// <summary>
	/// The state of the hand
	/// </summary>
	public enum State {
		/// <summary>
		/// Hand is in relaxed position
		/// </summary>
		Idle,
		/// <summary>
		/// Hand begins to be stretched out to reach the object
		/// </summary>
		Reaching,
		/// <summary>
		/// Hand begins to be raised with the object held
		/// </summary>
		Lifting,
		/// <summary>
		/// Hand is stationary with the object held
		/// </summary>
		Holding,
		/// <summary>
		/// Hand is moving to a new position
		/// </summary>
		Traversing,
		/// <summary>
		/// Hand begins to descend towards the target object/position
		/// </summary>
		Lowering,
		/// <summary>
		/// Hand begins to be raised with no object held
		/// </summary>
		Releasing,
		/// <summary>
		/// Hand begins to retreat towards relaxed position
		/// </summary>
		Unreaching
	}

	/// <summary>
	/// Current state
	/// </summary>
	private State currentState;

	/// <summary>
	/// The object to reach
	/// </summary>
	private Voxeme targetBlock;

	/// <summary>
	/// Placeholder for directing hand movements
	/// </summary>
	private Vector3 curReachTarget;

	/// <summary>
	/// Object to place the held object on top of
	/// </summary>
	private Voxeme setDownTarget; 

	/// <summary>
	/// Location to place the held object at
	/// </summary>
	private Vector3 setDownPos;

	/// <summary>
	/// Public, static reference to the block currently being held
	/// </summary>
	public static Voxeme heldObject = null;

	/// <summary>
	/// Reference to the effector bone to be used for manipulating objects
	/// </summary>
	private Transform hand;

	/// <summary>
	/// Height at which to raise the object while holding and traversing
	/// relative to object's top surface
	/// </summary>
	private const float liftHeight = .3f;

	/// <summary>
	/// Height at which to reach the object relative to object's top surface
	/// </summary>
	private const float reachHeight = .05f;

	/// <summary>
	/// Offset of the block's position (geometrical center in world coordinates) w.r.t hand bone.
	/// Hand bone position + hold offset = block position
	/// (Block) Position - hold offset = Hand bone position 
	/// </summary>
	private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);
	
	protected override void Start() {
		base.Start();
		Debug.Assert(animator != null);
		hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
		Debug.Assert(hand != null);
		currentState = State.Idle;
	}
	
	protected void Update()
	{
		string rightArmMotion = DataStore.GetStringValue("me:actual:motion:rightArm");

		switch (currentState)
		{
			case State.Idle:
				if (DataStore.GetStringValue("me:intent:action") == "pickUp")
				{
					// Try to resolve the target by name
					string name = DataStore.GetStringValue("me:intent:targetName");

					// If the target is a named object get its Voxeme component
					targetBlock = string.IsNullOrEmpty(name) ? null : GameObject.Find(name).GetComponent<Voxeme>();

					if (targetBlock == null)
					{
						// No name for the target, so we resolve by target location
						var targetLocation = DataStore.GetVector3Value("me:intent:target");
						if (targetLocation != default)
						{
							// Get the Voxeme component of object resolved by target location
							targetBlock = GlobalHelper.FindTargetByLocation(targetLocation, .1f, LayerMask.GetMask("Blocks")).GetComponent<Voxeme>();
						}
					}

					if (targetBlock != null)
					{
						// Either by name or location, we successfully resolved the target object
						// Get bounds of the Voxeme geometry
						var bounds = GlobalHelper.GetObjectWorldSize(targetBlock.gameObject);
						
						// The default set down position is the same as the initial position of the block
						// in case the user backs out of the action
						setDownPos = bounds.center + Vector3.down * bounds.extents.y;

						// Set the reach target to be within grabbing distance to the target
						curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;
						SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
						SetValue("me:intent:motion:rightArm", new DataStore.StringValue("reach"), "");

						// We go to the next state
						currentState = State.Reaching;
					}
					else
					{
						// TODO: Agent wasn't able to resolve the target either by name or by location, so must react accordingly
					}
				}
				break;
			case State.Reaching:
				// If the grab animation is completed
				if (rightArmMotion == "reached")
				{
					// Do not respond to forces/collisions
					Rigging rigging = targetBlock.GetComponent<Rigging>();
					if (rigging != null)
					{
						rigging.ActivatePhysics(false);
					}

					// Store a reference to the grabbed object
					heldObject = targetBlock;

					var bounds = GlobalHelper.GetObjectWorldSize(targetBlock.gameObject);
					// Raise the reach target to be high above the target
					curReachTarget = setDownPos + Vector3.up * (bounds.size.y + liftHeight) - holdOffset;

					// Set the target position of the held object to maintain the hold offset
					// NOTE: DianaMotorControl move speed and the voxeme move speed must be the same after this point
					//  or this isn't going to look right
					heldObject.targetPosition = curReachTarget + holdOffset;

					SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());

					// We go to the next state
					currentState = State.Lifting;
				}
				// ToDo: check for interrupt.
				break;
			case State.Lifting:
				if (rightArmMotion == "reached")
				{
					currentState = State.Holding;
					DataStore.SetValue("me:holding", new DataStore.StringValue(targetBlock.name), null, "BipedIKGrab");
				}
				break;
			case State.Holding:
				// Deliberation state
				// We proceed differently depending:
				//     - if we receive a block to set down the current block on
				//     - if we receive a location
				//     - if the user backs out in which case we place the block back where it was
				if (DataStore.GetStringValue("me:intent:action") == "setDown")
				{                    
					// Try to resolve set down target block by name
					string name = DataStore.GetStringValue("me:intent:targetName");
					setDownTarget = string.IsNullOrEmpty(name) ? null : grabbableBlocks.Find(name).GetComponent<Voxeme>();
					// In case we need to resolve by target location
					Vector3 targetPos = DataStore.GetVector3Value("me:intent:target");

					if (setDownTarget != null)
					{
						// We have the set down target block by name

						// Turn off physical interaction with the set down target block
						Rigging rigging = setDownTarget.GetComponent<Rigging>();
						if (rigging != null) {
							rigging.ActivatePhysics(false);
						}

						// Set down position is on the top surface of the set down target block
						var bounds = GlobalHelper.GetObjectWorldSize(setDownTarget.gameObject);
						setDownPos = bounds.center + bounds.extents.y * Vector3.up;

						// Set the reach target to be high above the set down position accounting for hold offset
						curReachTarget = setDownPos + Vector3.up * reachHeight - holdOffset;
						SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
						currentState = State.Traversing;
					}
					else if (targetPos != default)
					{
						// Set down position is the position on the table
						//  NOW: "Set down" position is just Vector3
						setDownPos = targetPos;

						// Set the reach target to be high above the set down position accounting for hold offset
						//curReachTarget = setDownPos + Vector3.up * reachHeight - holdOffset;
						curReachTarget = setDownPos - holdOffset;
						SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
						currentState = State.Traversing;
					}
					else
					{
						var bounds = GlobalHelper.GetObjectWorldSize(heldObject.gameObject);
						// Set the reach target to be just above the set down position accounting for hold offset
						curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;
						SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
						currentState = State.Lowering;
					}
				}
				break;
			case State.Traversing:
				// Move the held object along with the hand maintaining the same offset
				//heldObject.targetPosition = hand.transform.position + holdOffset;
			
				if (rightArmMotion == "reached")
				{
					var bounds = GlobalHelper.GetObjectWorldSize(heldObject.gameObject);
					// Set the reach target to be just above the set down position accounting for hold offset
					//curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;
					curReachTarget = setDownPos - holdOffset;
					SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
					currentState = State.Lowering;
				}
				break;        
			case State.Lowering:
				//heldObject.targetPosition = hand.transform.position + holdOffset;

				if (rightArmMotion == "reached")
				{
					heldObject.transform.SetParent(grabbableBlocks);
					var bounds = GlobalHelper.GetObjectWorldSize(heldObject.gameObject);
					//heldObject.targetPosition = setDownPos + Vector3.up * bounds.extents.y;
					heldObject.targetRotation = Vector3.zero;

					// reactivate physics on this object
					Rigging rigging = heldObject.GetComponent<Rigging>();
					if (rigging != null) {
						rigging.ActivatePhysics(true);
					}

					curReachTarget = heldObject.targetPosition + Vector3.up * reachHeight - holdOffset;
					SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());

					DataStore.SetValue("me:holding", new DataStore.StringValue(""), null, "BipedIKGrab released " + targetBlock.name);
					heldObject = null;

					currentState = State.Releasing;
				}
				break;
			case State.Releasing:
				if (rightArmMotion == "reached")
				{
					if (setDownTarget != null) {
						Rigging rigging = setDownTarget.GetComponent<Rigging>();
						if (rigging != null) {
							rigging.ActivatePhysics(true);
						}
					}
					curReachTarget = default;
					SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
					SetValue("me:intent:motion:rightArm", new DataStore.StringValue("unreach"), "");

					currentState = State.Unreaching;
				}
				break;    
			case State.Unreaching:
				if (rightArmMotion == "idle")
				{
					currentState = State.Idle;
					DataStore.ClearValue("me:intent:handPosR");
				}
				break;
		}
	}
}
