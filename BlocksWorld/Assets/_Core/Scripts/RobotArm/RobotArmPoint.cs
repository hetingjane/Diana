/*
This script makes the robot arm point at the location specified by the blackboard.

Reads:		me:intent:action (StringValue; watching for "point" or "reach")
			me:intent:target (Vector3d, position to point at)
Writes:		(nothing)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotArmPoint : MonoBehaviour
{
	public RobotServo[] servos;
	
	public float debugAngle;
		
	protected void Update() {
		if (DataStore.GetStringValue("me:intent:action") != "point") return;
		Vector3 target = DataStore.GetVector3Value("me:intent:target");
		
		// Find the 2D angle normal to the Y axis.  Point the base servo in that direction.
		Vector3 pos = transform.position;
		float ang = 90 - Mathf.Atan2(target.z - pos.z, target.x - pos.x) * Mathf.Rad2Deg;
		debugAngle = ang;
		servos[0].targetAngle = ang;
		
		// Put the arm joints in a standard "parked" position.
		servos[1].target = 0.85f;
		servos[2].target = 0.10f;
		
		// Now, point the gripper right at the target.
		// Start by getting the target position relative to the wrist joint.
		Vector3 localTarg = servos[2].transform.InverseTransformPoint(target)
						  - servos[3].transform.localPosition;
		// Then we can compute the proper angle for the last joint.
		ang = Mathf.Atan2(localTarg.y, localTarg.x) * Mathf.Rad2Deg + 90;
		servos[3].targetAngle = ang;
	}
}
