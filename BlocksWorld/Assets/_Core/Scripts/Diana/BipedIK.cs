/*
This script controls a humanoid (using a FinalIK FullBodyBipedIK component) 
to position the hand at the location specified by the blackboard.

Reads:		me:intent:handPosR (Vector3d, desired position of R hand, if any)
Writes:		me:actual:handPosR (Vector3d, actual current position of R hand)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion;
using RootMotion.FinalIK;

public class BipedIK : MonoBehaviour
{
	
	public float smoothTime = 0.1f;
	public float maxSpeed = 20f;
	
	Vector3 relaxedPos;
	Vector3 reachPos;
	Vector3 reachV;
	
	IKEffector reachingHand;
	
	protected void Start() {
		var ik = GetComponent<FullBodyBipedIK>();
		var solver = ik.GetIKSolver() as IKSolverFullBodyBiped;
		reachingHand = solver.rightHandEffector;
		reachingHand.positionWeight = 0;
		relaxedPos = reachPos = reachingHand.bone.position;
	}
	
	protected void Update() {
		Vector3 target = DataStore.GetVector3Value("me:intent:handPosR");
		if (target != default(Vector3)) {
			reachingHand.positionWeight = Mathf.MoveTowards(reachingHand.positionWeight, 1, 4 * Time.deltaTime);
			reachPos = Vector3.SmoothDamp(reachPos, target, ref reachV, smoothTime, maxSpeed);
			reachingHand.position = reachPos;
		} else {			
			reachingHand.positionWeight = Mathf.MoveTowards(reachingHand.positionWeight, 0, 2 * Time.deltaTime);
		}
		DataStore.SetValue("me:actual:handPosR", new DataStore.Vector3Value(reachPos), null, "BipedIK");
	}
}
