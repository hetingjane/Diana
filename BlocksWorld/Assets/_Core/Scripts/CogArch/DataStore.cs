/*
This is the central data store or "blackboard" of the cognitive architecture.
It basically wraps a Dictionary of key/value pairs.

It derives from MonoBehaviour so it can be configured and provide events in the Inspector.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStore : MonoBehaviour {

	// IValue: interface for any object that can be stored in our DataStore.
	public interface IValue {
		// Convert to a string in human-readable form.
		string ToString();
		
		// Return whether this value is equivalent to another.
		bool Equals(IValue other);
		
		// Return whether our value should be considered "empty"
		// for the sake of DataStore.HasValue.
		bool IsEmpty();
	}

	// StringValue: data storage of a string.
	public class StringValue : IValue {
		public string val;
		public StringValue(string inVal) { this.val = inVal; }
		public string ToString() { return val; }
		public bool Equals(IValue other) { return other is StringValue && val == ((StringValue)other).val; }
		public bool IsEmpty() { return string.IsNullOrEmpty(val); }
	}

	// BoolValue: data storage of a boolean value.
	public class BoolValue : IValue {
		public bool val;
		public BoolValue(bool inVal) { this.val = inVal; }
		public string ToString() { return val ? "True" : "False"; }
		public bool Equals(IValue other) { return other is BoolValue && val == ((BoolValue)other).val; }
		public bool IsEmpty() { return false; }
		
		// For convenience (and efficiency), we define a couple of static 
		// instances that represent True and False.
		public static BoolValue True = new BoolValue(true);
		public static BoolValue False = new BoolValue(false);		
	}
	
	// IntValue: data storage of an integer value.
	public class IntValue : IValue {
		public int val;
		public IntValue(int inVal) { this.val = inVal; }
		public string ToString() { return val.ToString(); }
		public bool Equals(IValue other) { return other is IntValue && val == ((IntValue)other).val; }
		public bool IsEmpty() { return false; }
		
		// Also for convenience, here are a couple of static instances
		// that represent 0 and 1.  Use them when it's convenient.
		public static IntValue Zero = new IntValue(0);
		public static IntValue One = new IntValue(1);		
	}
	
	// Singleton instance of the DataStore class.
	public static DataStore instance { get; private set; }
	
	[Header("Configuration")]
	[Tooltip("Whether to log all value changes with Debug.Log")]
	public bool logChanges = false;
	
	[Header("Events")]
	// fires when any value is changed, with the key:
	public StringEvent onValueChanged;
	
	// Delegate used when subscribing to a specific key; receives both key and value changed
	public delegate void ChangeHandler(string key, IValue value);
	Dictionary<string, List<ChangeHandler>> subscriptions = new Dictionary<string, List<ChangeHandler>>();
	
	// Here's the actual data storage:
	Dictionary<string, IValue> store = new Dictionary<string, IValue>();
	
	protected void Awake() {
		instance = this;
	}

	/// <summary>
	/// Subscribe to receive a callback whenever the value for a specific key is changed.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="handler">handler method to invoke when that value is changed</param>
	public static void Subscribe(string key, ChangeHandler handler) {
		List<ChangeHandler> subscribers;
		if (!instance.subscriptions.TryGetValue(key, out subscribers)) {
			subscribers = new List<ChangeHandler>();
			instance.subscriptions[key] = subscribers;
		}
		subscribers.Add(handler);
	}

	/// <summary>
	/// Set the value associated with a key.  Note that if the value is unchanged
	/// from its previous value, this method does nothing.
	/// 
	/// Also note that this method currently must be called only on the main thread.
	/// (ToDo: make it safe to call from subthreads.)
	/// </summary>
	/// <param name="key">string key</param>
	/// <param name="value">new value</param>
	/// <param name="module">module causing this change</param>
	/// <param name="comment">commment from that module, for logging/debugging</param>
	/// <returns>true if the value was changed, false if it was unchanged</returns>
	public static bool SetValue(string key, IValue value, ModuleBase module, string comment) {
		IValue oldVal;
		if (instance.store.TryGetValue(key, out oldVal) && oldVal.Equals(value)) return false;	// no change
		
		// Store the data
		instance.store[key] = value;
		
		// Log the changes for debugging purposes (if desired)
		if (instance.logChanges) {
			Debug.Log(string.Format("{0} := {1} ({2}: {3})", key, value.ToString(), module.name, comment), module);
		}
		
		// Invoke the general value-changed event handler
		instance.onValueChanged.Invoke(key);
		
		// Notify subscribers to this specific key (if any)
		List<ChangeHandler> subscribers;
		if (instance.subscriptions.TryGetValue(key, out subscribers)) {
			foreach (var handler in subscribers) handler(key, value);
		}
		
		return true;
	}

	/// <summary>
	/// Get the IValue reference associated with a given key.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <returns>value for that key, or null if key is not found</returns>
	public static IValue GetValue(string key) {
		IValue value;
		if (instance.store.TryGetValue(key, out value)) return value;
		return null;
	}
	
	/// <summary>
	/// Get a boolean value associated with a given key.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="defaultValue">value to return if key is not found (or not boolean)</param>
	/// <returns>value for that key, or defaultValue</returns>
	public static bool GetBoolValue(string key, bool defaultValue=false) {
		IValue value;
		if (!instance.store.TryGetValue(key, out value)) return defaultValue;
		if (value is BoolValue) return ((BoolValue)value).val;
		return defaultValue;
	}
	
	/// <summary>
	/// Get an integer value associated with a given key.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="defaultValue">value to return if key is not found (or not boolean)</param>
	/// <returns>value for that key, or defaultValue</returns>
	public static int GetIntValue(string key, int defaultValue=0) {
		IValue value;
		if (!instance.store.TryGetValue(key, out value)) return defaultValue;
		if (value is IntValue) return ((IntValue)value).val;
		return defaultValue;
	}
	
	/// <summary>
	/// Return whether the given key exists in this DataStore, AND
	/// has a non-empty value.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <returns>true if key is found; false otherwise</returns>
	public static bool HasValue(string key) {
		IValue value;
		if (!instance.store.TryGetValue(key, out value)) return false;
		return !value.IsEmpty();
	}

}
