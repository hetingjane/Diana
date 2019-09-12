using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SetBoolPref : MonoBehaviour
{
	public string prefKey = "SomePref";
	public bool defaultValue = false;
	
	public UnityEvent onSetTrue;
	public UnityEvent onSetFalse;
	
	protected void Awake() {
		if (!PlayerPrefs.HasKey(prefKey)) Set(defaultValue);
	}
	
	protected void Start() {
		bool isTrue = PlayerPrefs.GetInt(prefKey, defaultValue ? 1 : 0) != 0;
		if (isTrue) onSetTrue.Invoke();
		else onSetFalse.Invoke();
		
		var toggle = GetComponent<Toggle>();
		if (toggle != null) toggle.isOn = isTrue;
	}

	public void Set(bool value) {
		if (value) SetTrue();
		else SetFalse();
	}

	public void SetTrue() {
		PlayerPrefs.SetInt(prefKey, 1);
		Debug.Log("Set " + prefKey + " to true");
		onSetTrue.Invoke();
	}
	
	public void SetFalse() {
		PlayerPrefs.SetInt(prefKey, 0);
		Debug.Log("Set " + prefKey + " to false");
		onSetFalse.Invoke();
	}
	
	public void Toggle() {
		if (PlayerPrefs.GetInt(prefKey, defaultValue ? 1 : 0) == 0) SetTrue();
		else SetFalse();
	}
}
