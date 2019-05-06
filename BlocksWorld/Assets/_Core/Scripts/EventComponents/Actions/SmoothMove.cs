using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothMove : MonoBehaviour {

	public float moveTime = 0.5f;
	Vector3 targetPos;
	Vector3 velocity;
	
	protected void Awake() {
		targetPos = transform.position;
		velocity = Vector3.zero;
	}
	
	protected void Update() {
		if (transform.position != targetPos) {
			transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, moveTime, 100f);
		}
	}
	
	public void MoveX(float distance) {
		targetPos += new Vector3(distance, 0, 0);
	}

	public void MoveY(float distance) {
		targetPos += new Vector3(0, distance, 0);
	}

	public void MoveZ(float distance) {
		targetPos += new Vector3(0, 0, distance);
	}

	public void Move(Vector3 delta) {
		targetPos += delta;
	}
}
