using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChildTextRelay : MonoBehaviour {

	public StringEvent onInvoked;
	
	public void Invoke() {
		Text text = GetComponentInChildren<Text>();
		if (text != null) onInvoked.Invoke(text.text);
		
		TextMeshProUGUI tmPro = GetComponentInChildren<TextMeshProUGUI>();
		if (tmPro != null) onInvoked.Invoke(tmPro.text);
	}
}
