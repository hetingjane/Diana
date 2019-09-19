using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DianaMotorControl : MonoBehaviour
{
    private Animator animator;
	
	private readonly DataStore.StringValue reaching = new DataStore.StringValue("reaching");
	private readonly DataStore.StringValue reached = new DataStore.StringValue("reached");
	private readonly DataStore.StringValue moving = new DataStore.StringValue("moving");
	private readonly DataStore.StringValue unreaching = new DataStore.StringValue("unreaching");

	private readonly DataStore.StringValue idle = new DataStore.StringValue("idle");
	private readonly DataStore.StringValue waving = new DataStore.StringValue("waving");

	private const string meRightArmMotion = "me:rightArm:motion";

	private IEnumerator animatorLerpCoroutine;

	public float movementSpeed = .5f;
	private bool isMoving = false;

	// Start is called before the first frame update
	void Start()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);

		DataStore.Subscribe("me:intent:handPosR", OnIntentHandPoseRChanged);
    }

	IEnumerator LerpAnimatorXYZ(Vector3 intentXYZ)
	{
		Debug.Log("LerpAnimatorXYZ");
		Vector3 originalAnimatorXYZ = new Vector3(
				animator.GetFloat("x"),
				animator.GetFloat("y"),
				animator.GetFloat("z"));
		
		float originalDistance = Vector3.Distance(originalAnimatorXYZ, intentXYZ);

		if (originalDistance > 0f)
		{
			isMoving = true;
			float t = 0f;

			do
			{
				t += (Time.deltaTime * movementSpeed) / originalDistance;
				var animatorXYZ = Vector3.Lerp(originalAnimatorXYZ, intentXYZ, t);

				// Debug.Log($"t: {t}, animatorXYZ: {animatorXYZ}, intentXYZ: {intentXYZ}");

				SetAnimatorXYZ(x: animatorXYZ.x, y: animatorXYZ.y, z: animatorXYZ.z);

				yield return null;
			}
			while (t <= 1f);

		}

		isMoving = false;
		animatorLerpCoroutine = null;
	}

	private void SetAnimatorXYZ(float x = float.NaN, float y = float.NaN, float z = float.NaN)
	{
		if (!float.IsNaN(x))
			animator.SetFloat("x", x);
		if (!float.IsNaN(y))
			animator.SetFloat("y", y);
		if (!float.IsNaN(z))
			animator.SetFloat("z", z);
	}

	private void OnIntentHandPoseRChanged(string key, DataStore.IValue value)
	{
		Debug.Log("OnIntentHandPoseRChanged");
		Vector3 target = (value as DataStore.Vector3Value).val;

		if (!animator.GetBool("grab"))
		{
			if (target != default)
			{
				// Prepare animator to start the grab motion towards the target
				animator.SetBool("grab", true);
				SetAnimatorXYZ(x: target.x, y: target.y, z: target.z);
			}
		}
		else
		{
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

	// Update is called once per frame
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

		var curState = DataStore.GetStringValue(meRightArmMotion);
		if (curState != rightArmState.val)
		{
			Debug.Log($"State: {curState}");
		}
	}

	private string state = "";

    private void LateUpdate()
    {
        Vector3 actualPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        DataStore.SetValue("me:actual:handPosR", new DataStore.Vector3Value(actualPos), null, "DianaMotorControl");
    }
}
