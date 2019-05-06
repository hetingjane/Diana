using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MoveOnLine : MonoBehaviour {
	#region Public Properties
	[Tooltip("One end of the line.")]
	public Vector3 positionA;
	
	[Tooltip("Other end of the line.")]
	public Vector3 positionB;
	
	[Tooltip("Speed at which to move, in units/second.")]
	public float speed = 5;

	[Tooltip("If true, don't allow movement past the endpoints.")]
	public bool limitRange = true;

	[Tooltip("If true, automatically reverse motion when an endpoint is reached.")]
	public bool pingPong = false;

	[Tooltip("Whether to rotate the object so +Z is in the direction of movement.")]
	public bool faceForward = false;

	public UnityEvent onReachedA;
	public UnityEvent onReachedB;

	public float distanceToA {
		get {
			return (transform.position - positionA).magnitude;
		}
	}

	public float distanceToB {
		get {
			return (transform.position - positionB).magnitude;
		}
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	float direction;	// -1 to move towards A; 1 to move towards B; 0 if not moving

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Update() {
		if (direction == 0) return;
		if (limitRange || pingPong) {
			float maxMove = speed * Time.deltaTime;
			if (direction > 0) {
				transform.position = Vector3.MoveTowards(transform.position,
				                                         positionB, maxMove);
				if (transform.position == positionB) {
					onReachedB.Invoke();
					if (pingPong) MoveInDirectionA();
				}
			} else {
				transform.position = Vector3.MoveTowards(transform.position,
				                                         positionA, maxMove);
				if (transform.position == positionA) {
					onReachedA.Invoke();
					if (pingPong) MoveInDirectionB();
				}
			}
		} else {
			Vector3 dpos = (positionB - positionA).normalized * direction;
			transform.position += dpos * speed * Time.deltaTime;
		}
	}

//	void OnDrawGizmos() {
//		Gizmos.color = Color.green;
//		Gizmos.DrawLine(positionA, positionB);
//	}
	
	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods

	public void MoveInDirectionA() {
		direction = -1;
		Rotate();
	}

	public void MoveInDirectionB() {
		direction = 1;
		Rotate();
	}

	public void MoveToNearest() {
		if (distanceToA < distanceToB) MoveInDirectionA();
		else MoveInDirectionB();
	}

	public void MoveToFarthest() {
		if (distanceToA > distanceToB) MoveInDirectionA();
		else MoveInDirectionB();
	}
	
	public void StopMoveInDirectionA() {
		if (direction < 0) direction = 0;
	}

	public void StopMoveInDirectionB() {
		if (direction > 0) direction = 0;
	}

	public void SetSpeed(float speed) {
		this.speed = speed;
	}

	[ContextMenu("Jump to A")]
	public void JumpToA() {
		transform.position = positionA;
	}

	[ContextMenu("Jump to B")]
	public void JumpToB() {
		transform.position = positionB;
	}

	[ContextMenu("Set Position A")]
	public void SetPositionA() {
		positionA = transform.position;
	}
	
	[ContextMenu("Set Position B")]
	public void SetPositionB() {
		positionB = transform.position;
	}
	

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Methods

	void Rotate() {
		if (faceForward) {
			if (direction == 1) transform.LookAt(positionB);
			if (direction == -1) transform.LookAt(positionA);
		}
	}

	#endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(MoveOnLine))]
class MoveOnLineEditor : Editor {
	void OnSceneGUI() {
		var mol = target as MoveOnLine;

		Handles.DrawLine(mol.positionA, mol.positionB);

		mol.positionA = Handles.FreeMoveHandle(mol.positionA,
	       Quaternion.identity,
	       HandleUtility.GetHandleSize(mol.positionA) * 0.1f,
	       Vector3.zero,
                       Handles.CubeCap);
		Handles.Label(mol.positionA, "A");
		
		mol.positionB = Handles.FreeMoveHandle(mol.positionB,
	       Quaternion.identity,
	       HandleUtility.GetHandleSize(mol.positionA) * 0.1f,
	       Vector3.zero,
	       Handles.CubeCap);
		Handles.Label(mol.positionB, "B");
		
		if (GUI.changed) EditorUtility.SetDirty(target);
	}
}
#endif
