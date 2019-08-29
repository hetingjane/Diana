/* Script to make Diana actually look at things, by turning her head and eyes.

This is still very much a work in progress.   -- JJS
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DianaGaze : ModuleBase
{
	public Vector3 target;

	public Transform head;
	public Transform leftEye;
	public Transform rightEye;

	public Transform lookableObjects;
	
	protected Animator animator;

	public enum Mode {
		Disengaged,
		Engaged,
		LookingAtMyPoint,
		LookingAtUserPoint,
		LookingAtObject
	}
	public Mode mode = Mode.Disengaged;
	
	float weight = 0;
	Vector3 curLookPos;
	
	protected void Awake() {
		animator = GetComponent<Animator>();
		target = Camera.main.transform.position;
		curLookPos = target;
	}

	protected override void Start() {
		base.Start();
		DataStore.Subscribe("me:intent:lookAt", NoteLookAt);
		DataStore.Subscribe("me:intent:pointAt", NotePointAt);
	}
	
	void NoteLookAt(string key, DataStore.IValue value) {
		string atWhat = value.ToString();
		if (atWhat == "userPoint") {
			mode = Mode.LookingAtUserPoint;
		} else if (string.IsNullOrEmpty(atWhat) || atWhat == "user") {
			mode = Mode.Engaged;
			target = Camera.main.transform.position;
		} else {
			Transform t = lookableObjects.Find(atWhat);
			if (t != null) {
				mode = Mode.LookingAtObject;
				target = t.position;
			}
		}
	}
	
	void NotePointAt(string key, DataStore.IValue value) {
		string atWhat = value.ToString();
		if (string.IsNullOrEmpty(atWhat)) {
			if (mode == Mode.LookingAtMyPoint) {
				mode = Mode.Engaged;
				target = Camera.main.transform.position;
			}
		} else {
			mode = Mode.LookingAtMyPoint;
		}
	}

	protected void Update() {
		float targetWeight = 1;
		if (mode == Mode.LookingAtUserPoint) {
			target = DataStore.GetVector3Value("user:pointPos");
			if (target == default(Vector3)) target = Camera.main.transform.position;
		} else if (mode == Mode.LookingAtMyPoint) {
			target = DataStore.GetVector3Value("me:intent:target");
			if (target == default(Vector3)) target = Camera.main.transform.position;
			targetWeight = 0.5f;
		} else if (mode == Mode.Disengaged) {
			targetWeight = 0;
		}
		
		weight = Mathf.MoveTowards(weight, targetWeight, 2 * Time.deltaTime);
		curLookPos = Vector3.MoveTowards(curLookPos, target, 4 * Time.deltaTime);
	}
	
	protected void OnAnimatorIK(int layerIndex) {
		animator.SetLookAtWeight(weight);
		animator.SetLookAtPosition(curLookPos);
		leftEye.LookAt(curLookPos);
		rightEye.LookAt(curLookPos);
	}
}
