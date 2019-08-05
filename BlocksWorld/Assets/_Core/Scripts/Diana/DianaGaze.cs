/* Script to make Diana actually look at things, by turning her head and eyes.

This is still very much a work in progress.   -- JJS
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DianaGaze : MonoBehaviour
{
	public Transform target;	

	public Transform head;
	public Transform leftEye;
	public Transform rightEye;

	protected Animator animator;

	protected void Awake() {
		animator = GetComponent<Animator>();
	}

	protected void OnAnimatorIK(int layerIndex) {
		if (target == null) {
			animator.SetLookAtWeight(0);
		} else {
			animator.SetLookAtWeight(1);
			
			animator.SetLookAtPosition(target.position);
			leftEye.LookAt(target.position);
			rightEye.LookAt(target.position);
		}
	}
}
