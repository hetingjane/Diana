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

	public static bool paused = false;		// Hacky hack hack

    /// <summary>
    /// The state of the hand
    /// </summary>
    public enum State
    {
        /// <summary>
        /// Hand is in relaxed position
        /// </summary>
        Idle,
        /// <summary>
        /// Hand begins to be stretched out to reach the object
        /// </summary>
        Reaching,
        /// <summary>
        /// Hand begins to descend towards the object
        /// </summary>
        Grabbing,
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
    private Transform targetBlock;

    /// <summary>
    /// Placeholder for directing hand movements
    /// </summary>
    private Vector3 curReachTarget;

    /// <summary>
    /// Object to place the held object on top of
    /// </summary>
    private Transform setDownTarget;

    /// <summary>
    /// Location to place the held object at
    /// </summary>
    private Vector3 setDownPos;

    /// <summary>
    /// Public, static reference to the block currently being held
    /// </summary>
    public static Transform heldObject = null;

    /// <summary>
    /// Reference to the effector bone to be used for manipulating objects
    /// </summary>
    private Transform hand;

    /// <summary>
    /// Radius of the sphere to test for objects near to a location
    /// </summary>
    [Tooltip("Radius of the sphere to test for objects near to a location")]
    public float radius = .1f;

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

    protected override void Start()
    {
        base.Start();
        Debug.Assert(animator != null);
        hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Debug.Assert(hand != null);
	    currentState = State.Idle;
	    DataStore.Subscribe("user:isSpeaking", NoteSpeech);
    }

	void NoteSpeech(string key, DataStore.IValue value) {
		if (value.Equals(DataStore.BoolValue.True)) {
			//Debug.Log("Pausing to listen to the user");
			paused = true;
		}
	}
  
    /// <summary>
    /// Find an object transform nearest to a location within a given radius
    /// </summary>
    /// <param name="location">The location around which to search</param>
    /// <param name="radius">The radius of the sphere which checks for nearby overlapping objects</param>
    /// <returns>The transform of the closest object to <paramref name="location"/>"/> or <c>null</c> if it can't find any.</returns>
    private Transform FindTargetByLocation(Vector3 location, float radius)
    {
        Transform target = null;

        // Find a block that we can grab
        Collider[] colliders = Physics.OverlapSphere(location, radius, LayerMask.GetMask("Blocks"));

        if (colliders != null && colliders.Length > 0)
        {
            float minDistance = float.MaxValue;
            foreach (Collider c in colliders)
            {
                float distance = Vector3.Distance(c.transform.position, location);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = c.transform;
                }
            }
        }

        return target;
    }

    /// <summary>
    /// Tries to find an object transform nearest to a location within a given radius
    /// </summary>
    /// <param name="location">The location around which to search</param>
    /// <param name="radius">The radius of the sphere which checks for nearby overlapping objects</param>
    /// <param name="target">The transform of the nearest object if found, <c>null</c> otherwise</param>
    /// <returns><c>true</c> if the search was successful, <c>false</c> otherwise</returns>
    private bool TryFindTargetByLocation(Vector3 location, float radius, out Transform target)
    {
        target = FindTargetByLocation(location, radius);
        return target != null;
    }

    protected void Update()
	{
		if (paused) return;
    	
        string rightArmMotion = DataStore.GetStringValue("me:actual:motion:rightArm");

        switch (currentState)
        {
            case State.Idle:
                if (DataStore.GetStringValue("me:intent:action") == "pickUp")
                {
                    // Try to resolve the target by name
                    string name = DataStore.GetStringValue("me:intent:targetName");
                    targetBlock = string.IsNullOrEmpty(name) ? null : grabbableBlocks.Find(name);

                    if (targetBlock == null)
                    {
                        // No name for the target, so we resolve by target location
                        var targetLocation = DataStore.GetVector3Value("me:intent:target");
                        if (targetLocation != default)
                        {
                            targetBlock = FindTargetByLocation(targetLocation, radius);
                        }
                    }


                    if (targetBlock != null)
                    {
                        // Either by name or location, we successfully resolved the target object
                        var bounds = targetBlock.GetComponent<Collider>().bounds;
                        // The default set down position is the same as the initial position of the block
                        // in case the user backs out of the action
                        setDownPos = bounds.center + Vector3.down * bounds.extents.y;

                        // Set the reach target to be high above the set down position accounting for hold offset
                        curReachTarget = setDownPos + Vector3.up * (bounds.size.y + reachHeight) - holdOffset;

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
                    var bounds = targetBlock.GetComponent<Collider>().bounds;
                    // Lower the reach target to be withing grabbing distance to the target
                    curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;

                    SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());

                    // We go to the next state
                    currentState = State.Grabbing;
                }
                // ToDo: check for interrupt.
                break;
            case State.Grabbing:
                if (rightArmMotion == "reached")
                {
                    // Do not respond to forces/collisions
                    targetBlock.GetComponent<Rigidbody>().isKinematic = true;

                    // Store a reference to the grabbed object
                    heldObject = targetBlock;

                    // Raise the reach target to be high above the target
                    var bounds = targetBlock.GetComponent<Collider>().bounds;
                    curReachTarget = setDownPos + Vector3.up * (bounds.size.y + liftHeight) - holdOffset;

                    SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());

                    // We go to the next state
                    currentState = State.Lifting;
                }
                break;
            case State.Lifting:
                // Move the held object along with the hand maintaining the same offset
                heldObject.transform.position = hand.transform.position + holdOffset;
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
                    setDownTarget = string.IsNullOrEmpty(name) ? null : grabbableBlocks.Find(name);
                    // In case we need to resolve by target location
                    Vector3 targetPos = DataStore.GetVector3Value("me:intent:target");

                    if (setDownTarget != null)
                    {
                        // We have the set down target block by name

                        // Turn off physical interaction with the set down target block
                        setDownTarget.GetComponent<Rigidbody>().isKinematic = true;

                        // Set down position is on the top surface of the set down target block
                        var bounds = setDownTarget.GetComponent<Collider>().bounds;
                        setDownPos = bounds.center + bounds.extents.y * Vector3.up;

                        // Set the reach target to be high above the set down position accounting for hold offset
                        curReachTarget = setDownPos + Vector3.up * reachHeight - holdOffset;
                        SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
                        currentState = State.Traversing;
                    }
                    else if (targetPos != default)
                    {
                        // Set down position is the position on the table
                        setDownPos = targetPos;

                        // Set the reach target to be high above the set down position accounting for hold offset
                        curReachTarget = setDownPos + Vector3.up * reachHeight - holdOffset;
                        SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
                        currentState = State.Traversing;
                    }
                    else
                    {
                        var bounds = heldObject.GetComponent<Collider>().bounds;
                        // Set the reach target to be just above the set down position accounting for hold offset
                        curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;
                        SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
                        currentState = State.Lowering;
                    }
                }
                break;
            case State.Traversing:
                // Move the held object along with the hand maintaining the same offset
                heldObject.transform.position = hand.transform.position + holdOffset;

                if (rightArmMotion == "reached")
                {
                    var bounds = heldObject.GetComponent<Collider>().bounds;
                    // Set the reach target to be just above the set down position accounting for hold offset
                    curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;
                    SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
                    currentState = State.Lowering;
                }
                break;
            case State.Lowering:
                heldObject.transform.position = hand.transform.position + holdOffset;

                if (rightArmMotion == "reached")
                {
                    heldObject.SetParent(grabbableBlocks);
                    var bounds = heldObject.GetComponent<Collider>().bounds;
                    heldObject.position = setDownPos + Vector3.up * bounds.extents.y;
                    heldObject.eulerAngles = Vector3.zero;

                    heldObject.GetComponent<Rigidbody>().isKinematic = false;

                    curReachTarget = heldObject.position + Vector3.up * reachHeight - holdOffset;

                    SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());

                    DataStore.SetValue("me:holding", new DataStore.StringValue(""), null, "BipedIKGrab released " + targetBlock.name);
                    heldObject = null;

                    currentState = State.Releasing;
                }
                break;
            case State.Releasing:
                if (rightArmMotion == "reached")
                {
                    if (setDownTarget != null)
                        setDownTarget.GetComponent<Rigidbody>().isKinematic = false;
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

        SetValue("me:intent:handPosR", curReachTarget, currentState.ToString());
    }
}
