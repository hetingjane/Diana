/*
This script is a servomotor for one joint on the robot arm.  You give it a target
position (from 0-1), and it applies torque to try to achieve that position
(relative to the limits set).
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotServo : MonoBehaviour
{
	// Servo input: target position, from 0-1.
	[Range(0,1)] public float target = 0.5f;
	
	[Tooltip("Rotation speed, in degrees/sec")]
	public float speed = 100;

	[Tooltip("Minimum angle relative to starting angle.")]
	public float minAngle = -90;
	
	[Tooltip("Maximum angle relative to starting angle.")]
	public float maxAngle = 90;
	

	// If you prefer to get/set the target as an angle, use targetAngle.
	public float targetAngle {
		get {
			return Mathf.Lerp(minAngle, maxAngle, target);
		}
		set {
			target = (value - minAngle) / (maxAngle - minAngle);
		}
	}
	
	Rigidbody rb;
	float curAngle = 0;
	Vector3 startPosition;
	Quaternion startAngle;
	new AudioSource audio;
	float baseVolume, basePitch;

	protected void Awake() {
		rb = GetComponent<Rigidbody>();
		Debug.Assert(rb != null);
		startAngle = transform.localRotation;
		startPosition = transform.localPosition;
		audio = GetComponent<AudioSource>();
		if (audio != null) {
			baseVolume = audio.volume;
			basePitch = audio.pitch;
			audio.volume = 0;
		}
	}
	
	protected void FixedUpdate() {
		float targ = targetAngle;
		float delta = targ - curAngle;
		float targetVolume = 0;
		if (delta != 0) {
			curAngle = Mathf.MoveTowards(curAngle, targ, speed * Time.deltaTime);
			targetVolume = Mathf.Clamp01(Mathf.Abs(delta) / 30);
		}
		var localRot = startAngle * Quaternion.Euler(0, 0, curAngle);
		rb.MoveRotation(transform.parent.rotation * localRot);
		rb.MovePosition(transform.parent.TransformPoint(startPosition));

		if (audio != null) {
			float p = Mathf.MoveTowards(audio.volume, targetVolume, 5 * Time.deltaTime);
			audio.volume = p * baseVolume;
			audio.pitch = p * basePitch;
		}
		
	}
}
