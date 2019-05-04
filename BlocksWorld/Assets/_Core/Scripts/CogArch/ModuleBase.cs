/*	This is the base class for any cognitive module.

These derive from MonoBehaviour so that we can put them into the view
hierarchy, and easily configure/enable them via the Inspector.
Also, they can do their work via the standard Update method.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleBase : MonoBehaviour {

	[Header("Debugging Output")]

	[Tooltip("Comment set by the module for debugging purposes")]
	public string comment;

	[Header("Object References")]
	[Tooltip("Optional display module for showing our state")]
	public ModuleDisplay display;
	
	protected virtual void Start() {
		// Hook up the module display, if we have one
		if (display != null) display.Init(this);

		// Note: subclasses will usually want to override Start, call base.Start,
		// and then do either:
		// 1. DataStore.instance.onValueChanged.AddListener(ValueChanged);
		// ...or...
		// 2. DataStore.Subscribe("some:specific:key", NoteSomeSpecificKey);
	}

	protected virtual void ValueChanged(string key) {
		// Subclasses can override this to react to any key changes.
		// But in that case, be sure to hook this up to the onValueChanged
		// event of the data store.
	}

	/// <summary>
	/// Set a value in the data store, and also update our module display (if any).
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="value">value to store</param>
	/// <param name="comment">comment explaining the change</param>
	protected void SetValue(string key, DataStore.IValue value, string comment) {
		this.comment = comment;
		if (DataStore.SetValue(key, value, this, comment) && display != null) {
			display.ShowUpdate(key, value, comment);
		}
	}

	/// Set a string value in the data store, and also update our module display (if any).
	protected void SetValue(string key, string value, string comment) {
		SetValue(key, new DataStore.StringValue(value), comment);
	}
	
	/// Set a boolean value in the data store, and also update our module display (if any).
	protected void SetValue(string key, bool value, string comment) {
		SetValue(key, value ? DataStore.BoolValue.True : DataStore.BoolValue.False, comment);
	}
	
	/// Set a integer value in the data store, and also update our module display (if any).
	protected void SetValue(string key, int value, string comment) {
		SetValue(key, new DataStore.IntValue(value), comment);
	}
	
	/// Set a Vector3 value in the data store, and also update our module display (if any).
	protected void SetValue(string key, Vector3 value, string comment) {
		SetValue(key, new DataStore.Vector3Value(value), comment);
	}
	
}
