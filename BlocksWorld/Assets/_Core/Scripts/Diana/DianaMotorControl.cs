﻿using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// This script controls a humanoid's right arm motions like waving, grabbing, and other gestures.
/// 
/// <para>
/// When <c>me:intent:motion:rightArm</c> is changed, it can respond in different ways depending on what value is set:
/// 
/// <list type="bullet">
/// <item>
/// <term>"reach"</term>			<description>If Diana is not in a grab animation, it starts the grab animation 
///												 towards the target specified by <c>me:intent:handPosR</c> as long as it's not 
///												 <see cref="default(Vector3)"/> and sets <c>me:actual:motion:rightArm</c> to 
///												 <c>"reaching"</c></description>. When the grab animation completes, it sets
///												 <c>me:actual:motion:rightArm</c> to <c>"reached"</c>
///	</item>
///	
/// <item>
/// <term>"unreach"</term>			<description>If Diana is <c>me:actual:motion:rightArm</c> is <c>"reaching"</c>, <c>"reached"</c>
///												 or <c>"moving"</c>, then this will start the ungrab animation</description>
///												 
/// </item>
/// 
/// <item>
/// <term>"wave"</term>				<description>Starts the wave animation</description>
/// </item>
/// 
/// </list>
/// </para>
///
/// <para>
/// When <c>me:intent:handPosR</c> is changed when <c>me:actual:motion:rightArm</c> is <c>"reaching"</c> or <c>"reached"</c>,
/// it starts manipulating the ongoing animation so that it ends up at new set value. During this operation, <c>me:actual:motion:rightArm</c>
/// is set to "moving".
/// </para>
/// 
/// <para>
/// Reads:		<c>me:intent:handPosR</c>				The intended position of the hand
///				<c>me:intent:motion:rightArm</c>		The intended motion with right arm. Possible values are "reach", "unreach", "wave"
/// Writes:		<c>me:actual:handPosR</c>				The actual position of the hand updated every frame	
///				<c>me:actual:motion:rightArm</c>		The motion that Diana is doing with right arm. Possible values
///														are "idle", "reaching", "reached", "moving", "waving", "unreaching"
///				
/// </para>
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


	#region Animator Hashes
	private readonly int animX = Animator.StringToHash("x");
	private readonly int animY = Animator.StringToHash("y");
	private readonly int animZ = Animator.StringToHash("z");

	private readonly int animGrab = Animator.StringToHash("grab");
	private readonly int animWave = Animator.StringToHash("wave");

	#endregion
	/// <summary>
	/// The key to which this script writes on the blackboard. The key
	/// describes Diana's animation state (manual or automatic).
	/// </summary>
	private const string actualMotionRightArm = "me:actual:motion:rightArm";

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
	[Range(.1f, 1f)]
	[Tooltip("Movement speed when the hand position is manually manipulated")]
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
		DataStore.Subscribe("me:intent:motion:rightArm", OnIntentMotionRightArmChanged);
	}

	private bool AnimatorGrab
	{
		get => animator.GetFloat(animGrab) > 0.0f;

		set => animator.SetFloat(animGrab, value ? 1f : -1f);
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

		// If target is not Vector3.default, we move
		if (target != default)
		{
			// If already grabbing/grabbed
			if (AnimatorGrab)
			{
				// Changes should be over time
				if (animatorLerpCoroutine != null)
				{
					StopCoroutine(animatorLerpCoroutine);
				}

				animatorLerpCoroutine = LerpAnimatorXYZ(target);
				StartCoroutine(animatorLerpCoroutine);
			}
			else
			{
				// Set animator position parameters in anticipation of future writes to
				// me:intent:motion:rightArm
				SetAnimatorXYZ(target.x, target.y, target.z);
			}
		}
	}

	private void OnIntentMotionRightArmChanged(string key, DataStore.IValue value)
	{
		string motion = (value as DataStore.StringValue).val;

		if (motion == "reach")
		{
			if (!AnimatorGrab)
			{
				Vector3 target = DataStore.GetVector3Value("me:intent:handPosR");
				// Prepare animator to start the grab motion towards the target
				// Changes are instantaneous

				if (target != default)
				{
					SetAnimatorXYZ(x: target.x, y: target.y, z: target.z);
					AnimatorGrab = true;
				}
			}
		}
		else if (motion == "unreach")
			AnimatorGrab = false;
		else if (motion == "wave")
			animator.SetBool(animWave, true);
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
				animator.GetFloat(animX),
				animator.GetFloat(animY),
				animator.GetFloat(animZ));
		
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
			animator.SetFloat(animX, x);
		if (!float.IsNaN(y))
			animator.SetFloat(animY, y);
		if (!float.IsNaN(z))
			animator.SetFloat(animZ, z);
	}

	/// <summary>
	/// Updates <c>"me:actual:motion:rightArm"</c> when there's a change.
	/// </summary>
	void Update()
	{
		AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		var rightArmState = DataStore.GetValue(actualMotionRightArm) as DataStore.StringValue;

		if (animatorStateInfo.IsName("Grab"))
		{
			// Set to reaching if animation hasn't finished
			if (animatorStateInfo.normalizedTime < 1f && rightArmState != reaching)
				DataStore.SetValue(actualMotionRightArm, reaching, null, "DianaMotorControl");
			// Set to reached if animation has finished
			else if (animatorStateInfo.normalizedTime > 1f)
			{
				// unless it is moving
				if (isMoving)
				{
					// in which case set it to moving if it's not already set
					if (rightArmState != moving)
						DataStore.SetValue(actualMotionRightArm, moving, null, "DianaMotorControl");
				}
				else
				{
					if (rightArmState != reached)
						DataStore.SetValue(actualMotionRightArm, reached, null, "DianaMotorControl");
				}
			}
		}
		else if (animatorStateInfo.IsName("Ungrab"))
		{
			// Set to unreaching if animation hasn't finished
			if (rightArmState != unreaching)
				DataStore.SetValue(actualMotionRightArm, unreaching, null, "DianaMotorControl");
		}
		else if (animatorStateInfo.IsName("Idle"))
		{
			if (rightArmState != idle)
				DataStore.SetValue(actualMotionRightArm, idle, null, "DianaMotorControl");
		}
		else if (animatorStateInfo.IsName("Wave"))
		{
			if (rightArmState != waving)
				DataStore.SetValue(actualMotionRightArm, waving, null, "DianaMotorControl");
		}

		var curState = DataStore.GetStringValue(actualMotionRightArm, defaultValue: string.Empty);
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
