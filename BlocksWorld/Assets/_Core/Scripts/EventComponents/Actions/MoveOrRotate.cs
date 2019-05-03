using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveOrRotate : MonoBehaviour {

	
	public void MoveX(float distance) {
		transform.position += new Vector3(distance, 0, 0);
	}
	
	public void MoveY(float distance) {
		transform.position += new Vector3(0, distance, 0);
	}
	
	public void MoveZ(float distance) {
		transform.position += new Vector3(0, 0, distance);
	}
	
	public void RotateX(float degrees) {
		transform.rotation = Quaternion.Euler(degrees, 0, 0) * transform.rotation;
	}
	
	public void RotateY(float degrees) {
		transform.rotation = Quaternion.Euler(0, degrees, 0) * transform.rotation;
	}
	public void RotateZ(float degrees) {
		transform.rotation = Quaternion.Euler(0, 0, degrees) * transform.rotation;
	}
	
}
