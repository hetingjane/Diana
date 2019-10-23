using UnityEngine;

using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Vox;

public enum GraspState
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
	/// Hand is stationary with the object held
	/// </summary>
	Reached,
	Moving,
	/// <summary>
	/// Hand begins to retreat towards relaxed position
	/// </summary>
	Unreaching
}

public class GraspModule : ModuleBase
{
	/// <summary>
	/// Reference to the animator attached to the humanoid character
	/// </summary>
	[Tooltip("Drag drop the character model here")]
	public Animator animator;

	/// <summary>
	/// The object to reach
	/// </summary>
	private Voxeme target;

	/// <summary>
	/// Placeholder for directing hand movements
	/// </summary>
	private Vector3 movePosition;

	/// <summary>
	/// Object being held currently
	/// </summary>
	private Voxeme held;

	/// <summary>
	/// Reference to the effector bone to be used for manipulating objects
	/// </summary>
	private Transform hand;

	/// <summary>
	/// Current state
	/// </summary>
	private GraspState currentState;

	// Start is called before the first frame update
	protected override void Start()
    {
		base.Start();
		Debug.Assert(animator != null);
		hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
		Debug.Assert(hand != null);
		currentState = GraspState.Idle;
	}

	private const string reachAction = "reach";
	private const string holdAction = "hold";
	private const string moveAction = "move";
	private const string releaseAction = "release";
	private const string unreachAction = "unreach";

	// Update is called once per frame
	void Update()
    {
		string action = DataStore.GetStringValue("me:intent:action");
		string rightArmMotion = DataStore.GetStringValue("me:actual:motion:rightArm");
		
		switch (currentState)
		{
			case GraspState.Idle:
				if (action == reachAction)
				{
					// Try to resolve the target by name
					string targetName = DataStore.GetStringValue("me:intent:targetName");
					// If the target is a named object get its Voxeme component
					var curTarget = string.IsNullOrEmpty(targetName) ? null : GameObject.Find(targetName).GetComponent<Voxeme>();

					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if (curMovePosition != default)
					{
						target = curTarget;
						movePosition = curMovePosition;

						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
						SetValue("me:intent:motion:rightArm", new DataStore.StringValue("reach"), "");
						SetValue("me:intent:action:isComplete", false, "");
						// We begin reaching for the object or the location
						currentState = GraspState.Reaching;
					}
				}
				break;
			case GraspState.Reaching:
				if (rightArmMotion == "reached")
				{
					SetValue("me:intent:action:isComplete", true, "");
					currentState = GraspState.Reached;
				}
				else if (action == unreachAction)
				{
					target = null;
					movePosition = default;

					SetValue("me:intent:motion:rightArm", new DataStore.StringValue("unreach"), "");
					SetValue("me:intent:action:isComplete", false, "");
					currentState = GraspState.Unreaching;
				}
				else if (action == reachAction)
				{
					// Try to resolve the target by name
					string targetName = DataStore.GetStringValue("me:intent:targetName");
					// If the target is a named object get its Voxeme component
					var curTarget = string.IsNullOrEmpty(targetName) ? null : GameObject.Find(targetName).GetComponent<Voxeme>();

					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if (curTarget != target && curMovePosition != default)
					{
						target = curTarget;
						movePosition = curMovePosition;

						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
					}
				}
				break;
			case GraspState.Reached:
				if (action == holdAction)
				{
					if (target != null)
					{
						held = target;
						SetValue("me:holding", held.name, $"Holding {held.name}");
						// Do not respond to forces/collisions
						Rigging rigging = held.GetComponent<Rigging>();
						if (rigging != null)
						{
                            if (rigging.usePhysicsRig)
                            {	rigging.ActivatePhysics(false);
                                //RiggingHelper.RigTo(held.gameObject, hand.gameObject);
                            }
						}
					}
					else
						Debug.LogWarning("hold action set on me:intent:action when me:intent:target is empty");
					
				}
				else if (action == releaseAction)
				{
					if (held != null)
					{
						SetValue("me:holding", "", $"Releasing {held.name}");

						// Reactivate phsyics
						Rigging rigging = held.GetComponent<Rigging>();
						if (rigging != null)
						{
                            if (!rigging.usePhysicsRig)
                            {
    							rigging.ActivatePhysics(true);
    						} 
                        }
						held = null;
					}
					else
						Debug.LogWarning("release action set on me:intent:action without holding an object");
				}
				else if (action == unreachAction)
				{
					if (held != null)
					{
						SetValue("me:holding", "", $"Holding nothing");

						// Reactivate phsyics
						Rigging rigging = held.GetComponent<Rigging>();
						if (rigging != null)
						{
							rigging.ActivatePhysics(true);
						}
					}
					held = null;

					target = null;
					movePosition = default;

					SetValue("me:intent:motion:rightArm", new DataStore.StringValue("unreach"), "");
					SetValue("me:intent:action:isComplete", false, "");
					currentState = GraspState.Unreaching;
				}
				else if (action == reachAction)
				{
					// Try to resolve the target by name
					string targetName = DataStore.GetStringValue("me:intent:targetName");

					// If the target is a named object get its Voxeme component
					var curTarget = string.IsNullOrEmpty(targetName) ? null : GameObject.Find(targetName).GetComponent<Voxeme>();

					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if ((curTarget == null || curTarget != target) && curMovePosition != default)
					{
						target = curTarget;
						movePosition = curMovePosition;

						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
						SetValue("me:intent:action:isComplete", false, "");

						currentState = GraspState.Reaching;
					}
				}
				else if (action == moveAction)
				{
					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if (curMovePosition != default && curMovePosition != movePosition)
					{
						movePosition = curMovePosition;
						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
						SetValue("me:intent:action:isComplete", false, "");

						currentState = GraspState.Moving;
					}
				}
				break;
			case GraspState.Moving:
				if (rightArmMotion == "reached")
				{
					SetValue("me:intent:action:isComplete", true, "");
					currentState = GraspState.Reached;
				}
				else if (rightArmMotion == "moving")
				{
					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if (curMovePosition != default && curMovePosition != movePosition)
					{
						movePosition = curMovePosition;
						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
					}
				}
				break;
			case GraspState.Unreaching:
				if (rightArmMotion == "idle")
				{
					SetValue("me:intent:action:isComplete", true, "");
					currentState = GraspState.Idle;
				}
				break;
		}
	}
}
