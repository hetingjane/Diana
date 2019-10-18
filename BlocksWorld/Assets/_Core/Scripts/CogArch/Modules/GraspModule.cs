using VoxSimPlatform.Vox;
using UnityEngine;

public enum GraspState
{
	/// <summary>
	/// Hand is in relaxed position
	/// </summary>
	Idle,
	/// <summary>
	/// Hand begins to be stretched out to reach the object
	/// </summary>
	Grasping,
	/// <summary>
	/// Hand is stationary with the object held
	/// </summary>
	Holding,
	Moving,
	/// <summary>
	/// Hand begins to retreat towards relaxed position
	/// </summary>
	Ungrasping
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

	private const string graspAction = "grasp";
	private const string moveAction = "move";
	private const string ungraspAction = "ungrasp";

	// Update is called once per frame
	void Update()
    {
		string action = DataStore.GetStringValue("me:intent:action");
		string rightArmMotion = DataStore.GetStringValue("me:actual:motion:rightArm");
		
		switch (currentState)
		{
			case GraspState.Idle:
				if (action == graspAction)
				{
					// Try to resolve the target by name
					string targetName = DataStore.GetStringValue("me:intent:targetName");
					// If the target is a named object get its Voxeme component
					var curTarget = string.IsNullOrEmpty(targetName) ? null : GameObject.Find(targetName).GetComponent<Voxeme>();

					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if (curTarget != null && curMovePosition != default)
					{
						target = curTarget;
						movePosition = curMovePosition;

						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
						SetValue("me:intent:motion:rightArm", new DataStore.StringValue("reach"), "");
						SetValue("me:intent:action:isComplete", false, "");
						// We begin grasping
						currentState = GraspState.Grasping;
					}
				}
				break;
			case GraspState.Grasping:
				if (rightArmMotion == "reached")
				{
					held = target;
					SetValue("me:holding", held.name, $"Holding {held.name}");

					SetValue("me:intent:action:isComplete", true, "");
					currentState = GraspState.Holding;
				}
				else if (action == ungraspAction)
				{
					SetValue("me:intent:motion:rightArm", new DataStore.StringValue("unreach"), "");
					SetValue("me:intent:action:isComplete", false, "");
					currentState = GraspState.Ungrasping;
				}
				else if (action == graspAction)
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
			case GraspState.Holding:
				if (action == ungraspAction)
				{
					held = null;
					SetValue("me:holding", "", $"Holding nothing");

					target = null;
					movePosition = default;

					SetValue("me:intent:motion:rightArm", new DataStore.StringValue("unreach"), "");
					SetValue("me:intent:action:isComplete", false, "");
					currentState = GraspState.Ungrasping;
				}
				else if (action == graspAction)
				{
					// Try to resolve the target by name
					string targetName = DataStore.GetStringValue("me:intent:targetName");

					// If the target is a named object get its Voxeme component
					var curTarget = string.IsNullOrEmpty(targetName) ? null : GameObject.Find(targetName).GetComponent<Voxeme>();

					Vector3 curMovePosition = DataStore.GetVector3Value("me:intent:target");

					if (curTarget != null && curTarget != target)
					{
						target = curTarget;
						movePosition = curMovePosition;

						SetValue("me:intent:handPosR", movePosition, currentState.ToString());
						SetValue("me:intent:action:isComplete", false, "");

						currentState = GraspState.Grasping;
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
					currentState = GraspState.Holding;
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
			case GraspState.Ungrasping:
				if (rightArmMotion == "idle")
				{
					SetValue("me:intent:action:isComplete", true, "");
					currentState = GraspState.Idle;
				}
				break;
		}
	}
}
