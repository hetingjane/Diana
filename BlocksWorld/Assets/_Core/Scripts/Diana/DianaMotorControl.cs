using System.Collections;
using UnityEngine;

/// <summary>
/// This script makes a humanoid place the hand at a specified position.
/// When <c>me:intent:handPosR</c> is changed, it can respond in different ways:
/// - If Diana is not in grab animation, it starts the grab animation towards the intended hand position
///   and sets the <c>me:rightArm:motion</c> key to "reaching"
/// - If Diana completes the grab animation, the key <c>me:rightArm:motion</c> is set to "reached"
/// - If <c>me:intent:handPosR</c> is set while "reaching" or having "reached", it starts manipulating the
///   ongoing animation so that it ends up at new set value. During this operation, <c>me:rightArm:motion</c>
///   is set to "moving"
/// - If <c>me:intent:handPosR</c> is set to default value of <see cref="Vector3"/>, the ungrab animation starts.
/// Reads:		me:intent:handPosR		The intended position of the hand
/// Writes:		me:rightArm:motion		The motion that Diana is doing with right arm
///				me:actual:handPosR		The actual position of the hand updated every frame
/// </summary>
[RequireComponent(typeof(Animator))]
public class DianaMotorControl : MonoBehaviour
{
	/// <summary>
	/// Reference to the Animator
	/// </summary>
	private Animator animator;

	#region Key values
	/// <summary>
	/// <see cref="DataStore.StringValue"/> equivalent of the string <c>"reaching"</c>.
	/// Represents the state when the agent is in grab animation and the animation hasn't
	/// finished yet.
	/// </summary>
	private readonly DataStore.StringValue reaching = new DataStore.StringValue("reaching");

	/// <summary>
	/// <see cref="DataStore.StringValue"/> equivalent of the string <c>"reached"</c>.
	/// Represents teh state when the agent has completed the grab animation.
	/// </summary>
	private readonly DataStore.StringValue reached = new DataStore.StringValue("reached");

	/// <summary>
	/// <see cref="DataStore.StringValue"/> equivalent of the string <c>"moving"</c>.
	/// Represents the state when the agent's hand position is being manually manipulated 
	/// to be along a trajectory.
	/// </summary>
	private readonly DataStore.StringValue moving = new DataStore.StringValue("moving");

	/// <summary>
	/// <see cref="DataStore.StringValue"/> equivalent of the string <c>"unreaching"</c>.
	/// Represents the state when the agent is in ungrab animation.
	/// </summary>
	private readonly DataStore.StringValue unreaching = new DataStore.StringValue("unreaching");

	/// <summary>
	/// <see cref="DataStore.StringValue"/> equivalent of the string <c>"idle"</c>.
	/// Represents the state when the agent is in idle animation.
	/// </summary>
	private readonly DataStore.StringValue idle = new DataStore.StringValue("idle");

	/// <summary>
	/// <see cref="DataStore.StringValue"/> equivalent of the string <c>"waving"</c>.
	/// Represents the state when the agent is in wave animation.
	/// </summary>
	private readonly DataStore.StringValue waving = new DataStore.StringValue("waving");
	#endregion

	/// <summary>
	/// The key to which this script writes on the blackboard. The key
	/// describes Diana's animation state (manual or automatic).
	/// </summary>
	private const string meRightArmMotion = "me:rightArm:motion";

	/// <summary>
	/// Reference to the coroutine used to manipulate Diana's hand
	/// position manually.
	/// </summary>
	private IEnumerator animatorLerpCoroutine;

	/// <summary>
	/// Movement speed when Diana's hand position is being manipulated manually.
	/// Note that this does not affect the movement speed for genuine animations
	/// like grab, ungrab, wave, etc.
	/// </summary>
	public float movementSpeed = .5f;

	/// <summary>
	/// Flag to mark if the hand is being moved manually or not.
	/// </summary>
	private bool isMoving = false;

	/// <summary>
	/// Establishes reference to the Animator component and
	/// subscribes to <c>"me:intent:handPosR"</c> key.
	/// </summary>
	void Start()
	{
		animator = GetComponent<Animator>();
		Debug.Assert(animator != null);

		DataStore.Subscribe("me:intent:handPosR", OnIntentHandPoseRChanged);
	}

	/// <summary>
	/// The coroutine to move the hand manually to the specified position.
	/// </summary>
	/// <param name="intentXYZ">The intended position of the hand.</param>
	/// <returns></returns>
	IEnumerator LerpAnimatorXYZ(Vector3 intentXYZ)
	{
		//Debug.Log("LerpAnimatorXYZ");
		Vector3 originalAnimatorXYZ = new Vector3(
				animator.GetFloat("x"),
				animator.GetFloat("y"),
				animator.GetFloat("z"));
		
		float originalDistance = Vector3.Distance(originalAnimatorXYZ, intentXYZ);

		if (originalDistance > 0f)
		{
			// We start moving the hand now
			isMoving = true;
			float t = 0f;

			do
			{
				t += (Time.deltaTime * movementSpeed) / originalDistance;
				var animatorXYZ = Vector3.Lerp(originalAnimatorXYZ, intentXYZ, t);

				//Debug.Log($"t: {t}, animatorXYZ: {animatorXYZ}, intentXYZ: {intentXYZ}");

				SetAnimatorXYZ(x: animatorXYZ.x, y: animatorXYZ.y, z: animatorXYZ.z);

				yield return null;
			}
			while (t <= 1f);

		}

		isMoving = false;
		animatorLerpCoroutine = null;
	}

	/// <summary>
	/// Sets the animator x, y, z parameters to the specified ones.
	/// </summary>
	/// <param name="x">If specified, sets the value of <c>animator.x</c></param>
	/// <param name="y">If specified, sets the value of <c>animator.y</c></param>
	/// <param name="z">If specified, sets the value of <c>animator.z</c></param>
	private void SetAnimatorXYZ(float x = float.NaN, float y = float.NaN, float z = float.NaN)
	{
		if (!float.IsNaN(x))
			animator.SetFloat("x", x);
		if (!float.IsNaN(y))
			animator.SetFloat("y", y);
		if (!float.IsNaN(z))
			animator.SetFloat("z", z);
	}

	/// <summary>
	/// The handler for whenever the <c>"me:intent:handPosR"</c> key changes.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	private void OnIntentHandPoseRChanged(string key, DataStore.IValue value)
	{
		//Debug.Log("OnIntentHandPoseRChanged");
		Vector3 target = (value as DataStore.Vector3Value).val;

		if (!animator.GetBool("grab"))
		{
			if (target != default)
			{
				// Prepare animator to start the grab motion towards the target
				// Changes are instantaneous
				animator.SetBool("grab", true);
				SetAnimatorXYZ(x: target.x, y: target.y, z: target.z);
			}
		}
		else
		{
			// If target is Vector3.default, we ungrab
			if (target == default)
			{
				animator.SetBool("grab", false);
			}
			else
			{
				// Changes should be over time
				if (animatorLerpCoroutine != null)
				{
					StopCoroutine(animatorLerpCoroutine);
				}

				animatorLerpCoroutine = LerpAnimatorXYZ(target);
				StartCoroutine(animatorLerpCoroutine);
			}
		}
	}

	/// <summary>
	/// Updates <c>"me:rightArm:motion"</c> when there's a change.
	/// </summary>
	void Update()
	{
		AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		var rightArmState = DataStore.GetValue(meRightArmMotion) as DataStore.StringValue;

		if (animatorStateInfo.IsName("Grab"))
		{
			// Set to grabbing if animation hasn't finished
			if (animatorStateInfo.normalizedTime < 1f && rightArmState != reaching)
				DataStore.SetValue(meRightArmMotion, reaching, null, "DianaMotorControl");
			// Set to grabbed if animation has finished
			else if (animatorStateInfo.normalizedTime > 1f)
			{
				if (isMoving)
				{
					if (rightArmState != moving)
						DataStore.SetValue(meRightArmMotion, moving, null, "DianaMotorControl");
				}
				else
				{
					if (rightArmState != reached)
						DataStore.SetValue(meRightArmMotion, reached, null, "DianaMotorControl");
				}
			}
		}
		else if (animatorStateInfo.IsName("Ungrab"))
		{
			// Set to grabbing if animation hasn't finished
			if (rightArmState != unreaching)
				DataStore.SetValue(meRightArmMotion, unreaching, null, "DianaMotorControl");
		}
		else if (animatorStateInfo.IsName("Idle"))
		{
			if (rightArmState != idle)
				DataStore.SetValue(meRightArmMotion, idle, null, "DianaMotorControl");
		}
		else if (animatorStateInfo.IsName("Wave"))
		{
			if (rightArmState != waving)
				DataStore.SetValue(meRightArmMotion, waving, null, "DianaMotorControl");
		}

		var curState = DataStore.GetStringValue(meRightArmMotion, defaultValue: string.Empty);
		if (rightArmState == null || curState != rightArmState.val)
		{
			Debug.Log($"State: {curState}");
		}
	}

	/// <summary>
	/// Sets <c>"me:actual:handPosR"</c> in case a module depends on it.
	/// </summary>
	private void LateUpdate()
	{
		Vector3 actualPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
		DataStore.SetValue("me:actual:handPosR", new DataStore.Vector3Value(actualPos), null, "DianaMotorControl");
	}
}
