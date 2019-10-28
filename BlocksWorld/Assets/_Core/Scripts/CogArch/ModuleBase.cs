/*	This is the base class for any cognitive module.

These derive from MonoBehaviour so that we can put them into the view
hierarchy, and easily configure/enable them via the Inspector.
Also, they can do their work via the standard Update method.
*/
using UnityEngine;

/// <summary>
/// Base class for Modules. Modules are defined by their ability to write
/// to <see cref="DataStore"/> and subscribe to key value changes in the
/// <see cref="DataStore"/>. Additionally, they provide out of the box 
/// integration with <see cref="ModuleDisplay"/> for easy visualization.
/// </summary>
public class ModuleBase : MonoBehaviour
{
    /// <summary>
    /// Comment set by the module for debugging purposes
    /// </summary>
    [Header("Debugging Output")]
    [Tooltip("Comment set by the module for debugging purposes")]
    public string comment;

    /// <summary>
    /// Optional display module for showing our state
    /// </summary>
	[Header("Object References")]
	[Tooltip("Optional display module for showing our state")]
	public ModuleDisplay display;

    /// <summary>
    /// Initializes the display module if set.
    /// Override this method by calling <c>base.Start()</c> first. Then, you can 
    /// either hook up the <see cref="ValueChanged(string)"/> method
    /// to <see cref="DataStore.onValueChanged"/> event to subscribe to any key changes.
    /// Alternatively, you may use <see cref="DataStore.Subscribe(string, DataStore.ChangeHandler)"/>
    /// to subscribe to specific key changes.
    /// </summary>
    /// <example> An example of overriding this in a subclass is given below:
    /// <code>
    /// // Need this to initialize ModuleDisplay in subclass
    /// base.Start();
    /// // Either subscirbe to all key changes
    /// DataStore.instance.onValueChanged.AddListener(ValueChanged);
    /// // Or subscribe to specific key changes
    /// DataStore.Subscribe("some:specific:key", NoteSomeSpecificKey);
    /// </code>
    /// </example>
    protected virtual void Start() {
		// Hook up the module display, if we have one
		if (display != null) display.Init(this);
	}

    /// <summary>
    /// Override this method to react to any key changes. However, this method will
    /// only be triggered if it is hooked up to the <see cref="DataStore.onValueChanged"/> event.
    /// </summary>
    /// <param name="key">The key whose value changed</param>
	protected virtual void ValueChanged(string key)
    {
	}

	/// <summary>
	/// Set a value in the data store, and also update our module display (if any).
	/// </summary>
	/// <param name="key">Key of interest</param>
	/// <param name="value">Value to store</param>
	/// <param name="comment">Comment explaining the change</param>
	protected void SetValue(string key, DataStore.IValue value, string comment)
    {
		this.comment = comment;
		if (DataStore.SetValue(key, value, this, comment) && display != null)
        {
			display.ShowUpdate(key, value, comment);
		}
	}

    /// <summary>
    /// Set a string value in the data store, and also update our module display (if any)
    /// </summary>
    /// <param name="key">Key of interest</param>
	/// <param name="value">Value to store</param>
	/// <param name="comment">Comment explaining the change</param>
    protected void SetValue(string key, string value, string comment)
    {
		SetValue(key, new DataStore.StringValue(value.Trim()), comment);
	}

    /// <summary>
    /// Set a boolean value in the data store, and also update our module display (if any)
    /// </summary>
    /// <param name="key">Key of interest</param>
	/// <param name="value">Value to store</param>
	/// <param name="comment">Comment explaining the change</param>
    protected void SetValue(string key, bool value, string comment)
    {
		SetValue(key, value ? DataStore.BoolValue.True : DataStore.BoolValue.False, comment);
	}

    /// <summary>
    /// Set a integer value in the data store, and also update our module display (if any)
    /// </summary>
    /// <param name="key">Key of interest</param>
	/// <param name="value">Value to store</param>
	/// <param name="comment">Comment explaining the change</param>
    protected void SetValue(string key, int value, string comment)
    {
		SetValue(key, new DataStore.IntValue(value), comment);
	}

    /// <summary>
    /// Set a Vector3 value in the data store, and also update our module display (if any)
    /// </summary>
    /// <param name="key">Key of interest</param>
	/// <param name="value">Value to store</param>
	/// <param name="comment">Comment explaining the change</param>
    protected void SetValue(string key, Vector3 value, string comment)
    {
		SetValue(key, new DataStore.Vector3Value(value), comment);
    }



    /// <summary>
    /// Set a float array value in the data store, and also update our module display (if any)
    /// </summary>
    /// <param name="key">Key of interest</param>
	/// <param name="value">Value to store</param>
	/// <param name="comment">Comment explaining the change</param>
    protected void SetValue(string key, float[] value, string comment)
    {
        SetValue(key, new DataStore.FloatArrayValue(value), comment);
    }

    /// <summary>
    /// Set a Quaternion value in the data store, and also update our module display (if any)
    /// </summary>
    /// <param name="key">Key of interest</param>
    /// <param name="value">Value to store</param>
    /// <param name="comment">Comment explaining the change</param>
    protected void SetValue(string key, Quaternion value, string comment)
    {
        SetValue(key, new DataStore.QuaternionValue(value), comment);
    }
}
