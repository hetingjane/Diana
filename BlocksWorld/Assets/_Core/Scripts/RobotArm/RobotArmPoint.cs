/*
This script makes the robot arm point at the location specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "point")
			me:intent:target (Vector3d, position to point at)
Writes:		(nothing)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotArmPoint : MonoBehaviour
{
	public RobotServo baseServo;
	
	public float debugAngle;
		
	protected void Update() {
		if (DataStore.GetStringValue("me:intent:action") != "point") return;
		Vector3 target = DataStore.GetVector3Value("me:intent:target");
		
		// Find the 2D angle normal to the Y axis.
		Vector3 pos = transform.position;
		float ang = 90 - Mathf.Atan2(target.z - pos.z, target.x - pos.x) * Mathf.Rad2Deg;
		debugAngle = ang;
		baseServo.targetAngle = ang;
		
		// That points the base.  ToDo: get the other joints to point the gripper
		// directly at the target position (perhaps with IK).
	}
}
