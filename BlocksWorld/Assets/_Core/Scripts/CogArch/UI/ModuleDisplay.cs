/*
This script is for a UI component that displays the state of a module,
for visualization/debugging purposes.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModuleDisplay : MonoBehaviour {

	[Header("Object References")]
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI valueUpdateText;
	public TextMeshProUGUI commentText;
	
	[Header("Configuration")]
	public Color highlightColor = Color.white;
	public float highlightDuration = 0.5f;
	
	Graphic g;
	Color normalColor;
	float lastHighlightTime = -999;
	
	public void Init(ModuleBase owner) {
		nameText.text = owner.name;
		valueUpdateText.text = null;
		commentText.text = null;
	}
	
	public void ShowUpdate(string key, DataStore.IValue value, string comment) {
		if (key != null && value != null) {
			valueUpdateText.text = key + " := " + value.ToString();
		}
		commentText.text = "\"" + comment + "\"";
		lastHighlightTime = Time.time;
	}
	
	protected void Update() {
		if (g == null) {
			g = GetComponent<Graphic>();
			normalColor = g.color;
		}
		
		float t = (Time.time - lastHighlightTime) / highlightDuration;
		t = Mathf.SmoothStep(0, 1, t);
		if (t >= 1) g.color = normalColor;
		else g.color = Color.Lerp(highlightColor, normalColor, t);
	}
}
