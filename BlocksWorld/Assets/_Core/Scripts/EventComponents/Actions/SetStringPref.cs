using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class SetStringPref : MonoBehaviour
{
	public string prefKey = "SomePref";
	public string defaultValue = "";
	
	public StringEvent onSet;
	
	protected void Awake() {
		if (!PlayerPrefs.HasKey(prefKey)) Set(defaultValue);
	}
	
	protected void Start() {
		string value = PlayerPrefs.GetString(prefKey, defaultValue);
		onSet.Invoke(value);

		var field = GetComponent<TMP_InputField>();
		if (field != null) field.text = value;
		
		var text = GetComponent<TMP_Text>();
		if (text != null) text.text = value;

		var ugui = GetComponent<TextMeshProUGUI>();
		if (ugui != null) ugui.text = value;
	}

	public void Set(string value) {
		PlayerPrefs.SetString(prefKey, value);
		onSet.Invoke(value);
	}

}
