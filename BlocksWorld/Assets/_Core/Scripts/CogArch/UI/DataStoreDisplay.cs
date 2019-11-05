/*
This component draws all the key/value pairs in the DataStore to the screen,
for development and debugging.  To use:

1. Attach this to a TextMeshProUGUI object you want it to print to.
2. Invoke its HandleValueChanged method from the DataStore's OnValueChanged event. 
*/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

public class DataStoreDisplay : MonoBehaviour {

	[Header("Filter")]
	
	[Tooltip("Glob patterns for keys to display, e.g. user.*; or blank for all keys")]
	public string keysToShow;
	
	[Tooltip("Glob patterns for keys to hide, e.g. user.*; or blank to hide none")]
	public string keysToHide = "user:joint:*";

	TextMeshProUGUI outputText;
	
	bool needsUpdate = true;
	
	Regex keysToShowPat;
	string _keysToShow;		// value of keysToShow when keysToShowPat was last updated
	
	Regex keysToHidePat;
	string _keysToHide;		// value of keysToHide when keysToHidePat was last updated
	
	Dictionary<string, string> kvData = new Dictionary<string, string>();
	
	protected void Awake() {
		outputText = GetComponent<TextMeshProUGUI>();
		Debug.Assert(outputText != null);
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
			if (ShouldShow(k)) {
                sb.Append(k);
                sb.Append(" = ");
                sb.Append(kvData[k]);
                sb.Append("\n");
            }
		}
		outputText.text = sb.ToString();
	}
	
	/// <summary>
	/// Determine whether we should show the given key, based on our 
	/// keysToShow and keysToHide values.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	bool ShouldShow(string key) {
		if (!string.IsNullOrEmpty(keysToShow)) {
			// If it doesn't match some pattern in keysToShow, then don't show it.
			if (keysToShow != _keysToShow) {
				try {
					keysToShowPat = GlobPatternToRegex(keysToShow);
					_keysToShow = keysToShow;
				} catch {}
			}
			if (!keysToShowPat.IsMatch(key)) return false;
		}
		
		if (!string.IsNullOrEmpty(keysToHide)) {
			// If it does match any pattern in keysToHide, then don't show it.
			if (keysToHide != _keysToHide) {
				try {
					keysToHidePat = GlobPatternToRegex(keysToHide);
					_keysToHide = keysToHide;
				} catch {}
			}
			if (keysToHidePat.IsMatch(key)) return false;			
		}
		
		// Otherwise, show it!
		return true;
	}
	
	/// <summary>
	/// Convert a glob pattern to a regular expression.
	/// Example:   "foo:*;bar:baz:*"    ->	  "^(foo:.*|bar:baz:.*)$"
	/// </summary>
	/// <param name="pattern"></param>
	/// <returns></returns>
	Regex GlobPatternToRegex(string pattern) {
		string pat = pattern.Replace(".", "\\.");
		pat = pat.Replace(";", "|");
		pat = pat.Replace("*", ".*");
		pat = pat.Replace("?", ".");
		pat = "^(" + pat + ")$";		// (anchor at start and end)
		Debug.Log("Translating glob \"" + pattern + "\" to Regex: " + pat);
		Regex re = new Regex(pat);
		return re;
	}
}
