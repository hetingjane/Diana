/*
This is the central data store or "blackboard" of the cognitive architecture.
It basically wraps a Dictionary of key/value pairs.

It derives from MonoBehaviour so it can be configured and provide events in the Inspector.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
		public override string ToString() { return val; }
		public bool Equals(IValue other) { return other is StringValue && val == ((StringValue)other).val; }
		public bool IsEmpty() { return string.IsNullOrEmpty(val); }

		public readonly static StringValue Empty = new StringValue("");
	}

	// BoolValue: data storage of a boolean value.
	public class BoolValue : IValue {
		public bool val;
		public BoolValue(bool inVal) { this.val = inVal; }
		public override string ToString() { return val ? "True" : "False"; }
		public bool Equals(IValue other) { return other is BoolValue && val == ((BoolValue)other).val; }
		public bool IsEmpty() { return false; }
		
		// For convenience (and efficiency), we define a couple of static 
		// instances that represent True and False.
		public static BoolValue True = new BoolValue(true);
		public static BoolValue False = new BoolValue(false);
    }

    // IntValue: data storage of an integer value.
    public class IntValue : IValue
    {
        public int val;
        public IntValue(int inVal) { this.val = inVal; }
        public override string ToString() { return val.ToString(); }
        public bool Equals(IValue other) { return other is IntValue && val == ((IntValue)other).val; }
        public bool IsEmpty() { return false; }

        // Also for convenience, here are a couple of static instances
        // that represent 0 and 1.  Use them when it's convenient.
        public static IntValue Zero = new IntValue(0);
        public static IntValue One = new IntValue(1);
    }

    // IntValue: data storage of an integer value.
    public class FloatValue : IValue
    {
        public float val;
        public FloatValue(float inVal) { this.val = inVal; }
        public override string ToString() { return val.ToString(); }
        public bool Equals(IValue other) { return other is IntValue && val == ((IntValue)other).val; }
        public bool IsEmpty() { return false; }

        // Also for convenience, here are a couple of static instances
        // that represent 0 and 1.  Use them when it's convenient.
        public static FloatValue Zero = new FloatValue(0);
        public static FloatValue One = new FloatValue(1);
    }

    // Vector3Value: data storage of a Vector3 (i.e. an x,y,z 3-tuple).
    public class Vector3Value : IValue {
		public Vector3 val;
		public Vector3Value(Vector3 inVal) { this.val = inVal; }
		public override string ToString() { return val.ToString(); }
		public bool Equals(IValue other) { return other is Vector3Value && val == ((Vector3Value)other).val; }
		public bool IsEmpty() { return false; }
		
		// Also for convenience, here are a couple of static instances
		// that represent 0,0,0 and 1,1,1.  Use them when it's convenient.
		public static Vector3Value Zero = new Vector3Value(Vector3.zero);
		public static Vector3Value One = new Vector3Value(Vector3.one);
	}

    // FloatArrayValue: data storage of an arbitrary length 32-bit float array
    public class FloatArrayValue : IValue
    {
        public float[] val;
        public FloatArrayValue(float[] inVal) { this.val = inVal; }
        public override string ToString() { return string.Join(",", val); }
        public bool Equals(IValue other) { return other is FloatArrayValue && val == ((FloatArrayValue)other).val; }
        public bool IsEmpty() { return false; }
    }

    // QuaternionValue: data storage of a Quaternion (i.e. an x,y,z,w 4-tuple).
    public class QuaternionValue : IValue
    {
        public Quaternion val;
        public QuaternionValue(Quaternion inVal) { this.val = inVal; }
        public override string ToString() { return val.eulerAngles.ToString();  }
        public bool Equals(IValue other) { return other is QuaternionValue && val == ((QuaternionValue)other).val; }
        public bool IsEmpty() { return false; }

        // Also for convenience, here are a couple of static instances
        // that represent 0,0,0,0 and 1,1,1,1.  Use them when it's convenient.
        public static QuaternionValue Zero = new QuaternionValue(new Quaternion(0.0f, 0.0f, 0.0f, 0.0f));
        public static QuaternionValue One = new QuaternionValue(new Quaternion(1.0f, 1.0f, 1.0f, 1.0f));
    }
    // Singleton instance of the DataStore class.
    public static DataStore instance { get; private set; }
	
	[Header("Configuration")]
	[Tooltip("Whether to log all value changes with Debug.Log")]
	public bool logWithDebugLog = false;
	[Tooltip("Whether to log all value changes to a \"DataStore.log\" file")]
	public bool logToFile = false;

	[Header("Events")]
	// fires when any value is changed, with the key:
	public StringEvent onValueChanged;
		
	// Delegate used when subscribing to a specific key; receives both key and value changed
	public delegate void ChangeHandler(string key, IValue value);
	Dictionary<string, List<ChangeHandler>> subscriptions = new Dictionary<string, List<ChangeHandler>>();
	
	// Here's the actual data storage:
	Dictionary<string, IValue> store = new Dictionary<string, IValue>();
	Dictionary<string, DateTime> changeTime = new Dictionary<string, DateTime>();

	// Stream to the log file
	System.IO.StreamWriter logFileStream;
	
	static bool didRunUnitTests = false;
	
	protected void Awake() {
		// Because this is a MonoBehavior, we can't unit test it at static
		// initialization time (as most of our unit tests do).  Instead we
		// must do it here, from Awake.  Careful to only do it once.
		if (!didRunUnitTests) {
			didRunUnitTests = true;
			QA.UnitTest.RunUnitTest(new UnitTest());
		}
		
		// become the standard (singleton-ish) instance
		instance = this;		
	}

	protected void Update() {
		// Flush our log file once per frame
		if (logFileStream != null) {
			try {
				logFileStream.FlushAsync();
			} catch (InvalidOperationException e) {				
			}
		}
	}
	
	protected void OnDestroy() {
		if (logFileStream != null) logFileStream.Close();
	}

	//--------------------------------------------------------------------------------
	#region Instance Interface
	// Usage note: Most code should never be calling these instance methods; that's
	// why they have the ugly "I" prefix.  They're here mainly to support unit-testing,
	// which can't safely use the static methods but instead makes a throw-away
	// instance just for that purpose.
	//
	// All other code, please use the static methods.
	
	/// <summary>
	/// Subscribe to receive a callback whenever the value for a specific key is changed.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="handler">handler method to invoke when that value is changed</param>
	public void ISubscribe(string key, ChangeHandler handler) {
		List<ChangeHandler> subscribers;
		if (!subscriptions.TryGetValue(key, out subscribers)) {
			subscribers = new List<ChangeHandler>();
			subscriptions[key] = subscribers;
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
	public bool ISetValue(string key, IValue value, ModuleBase module, string comment) {
		IValue oldVal;
		if (store.TryGetValue(key, out oldVal) && oldVal.Equals(value)) return false;	// no change
		
		// Store the data (and the change time)
		var now = DateTime.Now;
		store[key] = value;
		changeTime[key] = now;
		
		// Log the changes for debugging purposes (if desired)
		if (logWithDebugLog) {
            if ((value != null) && (module != null)) {
    			Debug.Log(string.Format("{0} := {1} ({2}: {3})", key, value.ToString(), module.name, comment), module);
            }
		}
		if (logToFile) {
			if (logFileStream == null) logFileStream = System.IO.File.AppendText("DataStore.log");
			string line = string.Format("[{0}] {1} := {2} ({3}: {4})",
				now.ToString("yyyy-MM-dd HH:mm:ss"),
				key, value.ToString(), module?.name ?? "", comment);
			// under some circumstances the file may be busy, causing this to throw an error;
			// in that case, we'd rather lose a line of logging than cause problems for the app
			try {
				logFileStream.WriteLine(line);
			} catch (InvalidOperationException e) {				
			}
		}
		
		// Invoke the general value-changed event handler
		if (onValueChanged != null) onValueChanged.Invoke(key);
		
		// Notify subscribers to this specific key (if any)
		List<ChangeHandler> subscribers;
		if (subscriptions.TryGetValue(key, out subscribers)) {
			foreach (var handler in subscribers) handler(key, value);
		}
		
		return true;
	}
	
	/// <summary>
	/// Get the IValue reference associated with a given key.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <returns>value for that key, or null if key is not found</returns>
	public IValue IGetValue(string key) {
		IValue value;
		if (store.TryGetValue(key, out value)) return value;
		return null;
	}

	/// <summary>
	/// Get a boolean value associated with a given key.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="defaultValue">value to return if key is not found (or not boolean)</param>
	/// <returns>value for that key, or defaultValue</returns>
	public bool IGetBoolValue(string key, bool defaultValue=false) {
		IValue value;
		if (!store.TryGetValue(key, out value)) return defaultValue;
		if (value is BoolValue) return ((BoolValue)value).val;
		return defaultValue;
	}

	/// <summary>
	/// Get a string value associated with a given key (converting from other types).
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="defaultValue">value to return if key is not found</param>
	/// <returns>value for that key, or defaultValue</returns>
	public string IGetStringValue(string key, string defaultValue=null) {
		IValue value;
		if (!store.TryGetValue(key, out value)) return defaultValue;
		return value.ToString();
    }

    /// <summary>
    /// Get an integer value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not integer)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public int IGetIntValue(string key, int defaultValue = 0)
    {
        IValue value;
        if (!store.TryGetValue(key, out value)) return defaultValue;
        if (value is IntValue) return ((IntValue)value).val;
        return defaultValue;
    }

    /// <summary>
    /// Get an integer value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not float)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public float IGetFloatValue(string key, float defaultValue = 0)
    {
        IValue value;
        if (!store.TryGetValue(key, out value)) return defaultValue;
        if (value is FloatValue) return ((FloatValue)value).val;
        return defaultValue;
    }

    /// <summary>
    /// Get a Vector3 value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not Vector3)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public Vector3 IGetVector3Value(string key, Vector3 defaultValue = default(Vector3))
    {
        IValue value;
        if (!store.TryGetValue(key, out value)) return defaultValue;
        if (value is Vector3Value) return ((Vector3Value)value).val;
        return defaultValue;
    }

    /// <summary>
    /// Get a FloatArray value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not Vector3)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public float[] IGetFloatArrayValue(string key, float[] defaultValue = default(float[]))
    {
        IValue value;
        if (!store.TryGetValue(key, out value)) return defaultValue;
        if (value is FloatArrayValue) return ((FloatArrayValue)value).val;
        return defaultValue;
    }

    /// <summary>
    /// Get a Quaternion value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not Quaternion)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public Quaternion IGetQuaternionValue(string key, Quaternion defaultValue = default(Quaternion))
    {
        IValue value;
        if (!store.TryGetValue(key, out value)) return defaultValue;
        if (value is QuaternionValue) return ((QuaternionValue)value).val;
        return defaultValue;
    }

    /// <summary>
    /// Return whether the given key exists in this DataStore, AND
    /// has a non-empty value.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <returns>true if key is found; false otherwise</returns>
    public bool IHasValue(string key) {
		IValue value;
		if (!store.TryGetValue(key, out value)) return false;
		return !value.IsEmpty();
	}

	/// <summary>
	/// Remove a key from the blackboard.
	/// </summary>
	/// <param name="key"></param>
	public void IClearValue(string key) {
		if (store.ContainsKey(key)) store.Remove(key);
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Static Interface
	
	/// <summary>
	/// Subscribe to receive a callback whenever the value for a specific key is changed.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="handler">handler method to invoke when that value is changed</param>
	public static void Subscribe(string key, ChangeHandler handler) {
		instance.ISubscribe(key, handler);
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
		return instance.ISetValue(key, value, module, comment);
	}
    /// <summary>
	/// Set a string denotes the emotion associated with a key.  Note that if the string is unchanged
	/// from its previous string, this method does nothing.
	/// 
	/// Also note that this method currently must be called only on the main thread.
	/// (ToDo: make it safe to call from subthreads.)
	/// </summary>
	/// <param name="key">string key</param>
	/// <param name="value">new string value</param>
	/// <param name="module">module causing this change</param>
	/// <param name="comment">commment from that module, for logging/debugging</param>
	/// <returns>true if the value was changed, false if it was unchanged</returns>
    public static bool SetStringValue(string key, StringValue value, ModuleBase module, string comment)
    {
        return instance.ISetValue(key, value, module, comment);
    }
    /// <summary>
    /// Get the IValue reference associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <returns>value for that key, or null if key is not found</returns>
    public static IValue GetValue(string key) {
		return instance.IGetValue(key);
	}

	/// <summary>
	/// Get a boolean value associated with a given key.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="defaultValue">value to return if key is not found (or not boolean)</param>
	/// <returns>value for that key, or defaultValue</returns>
	public static bool GetBoolValue(string key, bool defaultValue=false) {
		return instance.IGetBoolValue(key, defaultValue);
	}
	
	/// <summary>
	/// Get a string value associated with a given key (converting from other types).
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <param name="defaultValue">value to return if key is not found</param>
	/// <returns>value for that key, or defaultValue</returns>
	public static string GetStringValue(string key, string defaultValue=null) {
		return instance.IGetStringValue(key, defaultValue);
    }

    /// <summary>
    /// Get an integer value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not integer)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public static int GetIntValue(string key, int defaultValue = 0)
    {
        return instance.IGetIntValue(key, defaultValue);
    }

    /// <summary>
    /// Get an integer value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not float)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public static float GetFloatValue(string key, float defaultValue = 0)
    {
        return instance.IGetFloatValue(key, defaultValue);
    }

    /// <summary>
    /// Get a Vector3 value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not Vector3)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public static Vector3 GetVector3Value(string key, Vector3 defaultValue = default(Vector3))
    {
        return instance.IGetVector3Value(key, defaultValue);
    }

    /// <summary>
    /// Get a float array value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not FloatArray)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public static float[] GetFloatArrayValue(string key, float[] defaultValue = default(float[]))
    {
        return instance.IGetFloatArrayValue(key, defaultValue);
    }


    /// <summary>
    /// Get a Quaternion value associated with a given key.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <param name="defaultValue">value to return if key is not found (or not Quaternion)</param>
    /// <returns>value for that key, or defaultValue</returns>
    public static Quaternion GetQuaternionValue(string key, Quaternion defaultValue = default(Quaternion))
    {
        return instance.IGetQuaternionValue(key, defaultValue);
    }

    /// <summary>
    /// Return whether the given key exists in this DataStore, AND
    /// has a non-empty value.
    /// </summary>
    /// <param name="key">key of interest</param>
    /// <returns>true if key is found; false otherwise</returns>
    public static bool HasValue(string key) {
		return instance.IHasValue(key);
    }
	
	
	/// <summary>
	/// Clear (erase) a value from the blackboard.
	/// </summary>
	/// <param name="key"></param>
	public static void ClearValue(string key) {
		instance.IClearValue(key);
	}

	/// <summary>
	/// Get the date/time at which the given key last changed value.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <returns>date/time of last change; undefined if key has never been set</returns>
	public static DateTime GetLastChangeTime(string key) {
		DateTime dt;
		instance.changeTime.TryGetValue(key, out dt);
		return dt;
	}
	
	/// <summary>
	/// Get the number of seconds since the key was last changed.
	/// If the key has never been set, this returns Mathf.NegativeInfinity.
	/// </summary>
	/// <param name="key">key of interest</param>
	/// <returns>number of seconds since the key last changed value</returns>
	public static float GetSecondsSinceLastChange(string key) {
		DateTime dt;
		if (instance.changeTime.TryGetValue(key, out dt)) {
			TimeSpan span = DateTime.Now - dt;
			return span.Milliseconds * 0.001f;
		}
		return Mathf.NegativeInfinity;
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Unit Test
	
	public class UnitTest : QA.UnitTest {
		string lastChangedKey = null;
		DataStore.IValue lastChangedValue;
		int changeCount = 0;
		
		void NoteChange(string key, DataStore.IValue value) {
			lastChangedKey = key;
			lastChangedValue = value;
			changeCount++;
		}
		
		protected override void Run() {
			GameObject temp = new GameObject("temp (for DataStore unit test)");
			DataStore ds = temp.AddComponent<DataStore>();
			ds.logToFile = ds.logWithDebugLog = false;
			ds.ISubscribe("foo", NoteChange);
			ds.ISubscribe("bar", NoteChange);
			
			AssertFalse(ds.IHasValue("foo"));
			ds.ISetValue("foo", new IntValue(42), null, "test1");
			AssertTrue(ds.IHasValue("foo"));
			AssertEqual(42, ds.IGetIntValue("foo"));
			AssertEqual(changeCount, 1);
			AssertEqual(lastChangedKey, "foo");
			Assert(lastChangedValue is IntValue, "wrong value type");
			AssertEqual(42, ((IntValue)lastChangedValue).val);
			
			AssertFalse(ds.IHasValue("bar"));
			ds.ISetValue("bar", new StringValue("Hello world!"), null, "test2");
			AssertTrue(ds.IHasValue("bar"));
			AssertEqual("Hello world!", ds.IGetValue("bar").ToString());
			AssertEqual(changeCount, 2);
			AssertEqual(lastChangedKey, "bar");
			Assert(lastChangedValue is StringValue, "wrong value type");
			AssertEqual("Hello world!", ((StringValue)lastChangedValue).val);
			
			AssertFalse(ds.IHasValue("baz"));
			ds.ISetValue("baz", new Vector3Value(Vector3.forward), null, "test3");
			AssertTrue(ds.IHasValue("baz"));
			AssertEqual(Vector3.forward, ds.IGetVector3Value("baz"));
			AssertEqual(changeCount, 2);		// (since we didn't subscribe to "baz")
			AssertEqual(lastChangedKey, "bar");
			Destroy(temp);
		}
		
	}
	
	#endregion  // Unit Test

}
