using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorActions : MonoBehaviour {

	Animator anim;

	void Awake() {
		anim = GetComponent<Animator>();
	}
	
	public void SetBool(string boolName) {
		anim.SetBool(boolName, true);
	}
	
	public void ClearBool(string boolNamE) {
		anim.SetBool(boolNamE, false);
	}
}
