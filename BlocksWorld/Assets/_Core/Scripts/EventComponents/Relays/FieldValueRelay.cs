using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class FieldValueRelay : MonoBehaviour {
	
	public InputField field;
	public TMP_InputField tmpField;
	
	public StringEvent onInvokeWithText;
	public FloatEvent onInvokeWithFloat;
	public IntEvent onInvokeWithInt;
	
	public void Invoke() {
		if (field == null) field = GetComponent<InputField>();
		if (tmpField == null) tmpField = GetComponent<TMP_InputField>();
		if (field == null && tmpField == null) return;
		
		string text = (field == null ? tmpField.text : field.text);
		onInvokeWithText.Invoke(text);
		
		if (string.IsNullOrEmpty(text.Trim())) text = "0";
		
		float floatVal;
		if (float.TryParse(text, out floatVal)) onInvokeWithFloat.Invoke(floatVal);
		
		int intVal;
		if (int.TryParse(text, out intVal)) onInvokeWithInt.Invoke(intVal);
		
	}
}
