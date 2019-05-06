/*
This component draws all the key/value pairs in the DataStore to the screen,
for development and debugging.  To use:

1. Attach this to a TextMeshProUGUI object you want it to print to.
2. Invoke its HandleValueChanged method from the DataStore's OnValueChanged event. 
*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class DataStoreDisplay : MonoBehaviour {

	TextMeshProUGUI outputText;
	
	bool needsUpdate = true;
	
	Dictionary<string, string> kvData;
	
	protected void Awake() {
		outputText = GetComponent<TextMeshProUGUI>();
		Debug.Assert(outputText != null);
		kvData = new Dictionary<string, string>();
	}
	
	protected void Update() {
		if (needsUpdate) UpdateNow();
	}
	
	public void HandleValueChanged(string key) {
		kvData[key] = DataStore.GetValue(key).ToString();
		needsUpdate = true;
	}
	
	void UpdateNow() {
		var keys = kvData.Keys.ToList();
		keys.Sort();
		
		var sb = new System.Text.StringBuilder();
		sb.Append("<margin-left=10em><line-indent=-10em>");
		foreach (string k in keys) {
			sb.Append(k);
			sb.Append(" = ");
			sb.Append(kvData[k]);
			sb.Append("\n");
		}
		outputText.text = sb.ToString();
	}
}
