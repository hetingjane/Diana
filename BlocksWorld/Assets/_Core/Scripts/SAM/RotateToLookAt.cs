/*
This simple script rotates the Transform it's attached to to point at a point
obtained from the blackboard.  Used, for example, to make SAM look where the
user is pointing.

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateToLookAt : MonoBehaviour
{
	public string targetKey = "user:pointPos";
	public float speed = 720;
	public Vector3 rotOffset = new Vector3(0, -90, -90);
	
	Quaternion startRot;
	
	protected void Start() {
		startRot = transform.localRotation;
	}

	protected void Update() {
		Quaternion targetRot;
		Vector3 targetPos = DataStore.GetVector3Value(targetKey);
		if (targetPos.sqrMagnitude < 0.01f) {
			// no target set
			targetRot = startRot;
		} else {
			// look towards the target
			Vector3 dir = (targetPos - transform.position).normalized;
			targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(rotOffset);
		}
		transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
	}

}
