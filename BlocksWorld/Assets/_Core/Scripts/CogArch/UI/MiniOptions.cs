/*
This script drives the "mini options" canvas that lets us turn on/off various
debugging displays, and configure the system (such as picking the camera to
be used for Affectiva).

*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MiniOptions : MonoBehaviour
{
	public TMP_Dropdown cameraDropdown;
	
	public StringEvent onCameraChosen;
	
	void Start() {
		cameraDropdown.options.Clear();
		string curCamName = PlayerPrefs.GetString("FaceCam");
		int curCamIdx = 0;
		foreach (var cam in WebCamTexture.devices) {
			var opt = new TMP_Dropdown.OptionData();
			opt.text = cam.name;
			cameraDropdown.options.Add(opt);
			if (cam.name == curCamName) curCamIdx = cameraDropdown.options.Count;
		}
		cameraDropdown.value = -1;
		cameraDropdown.value = curCamIdx;
		//NoteCameraOptionChanged(curCamIdx);
	}
	
	public void NoteCameraOptionChanged(int value) {
		if (value < 0) return;
		string choice = cameraDropdown.options[value].text;
		PlayerPrefs.SetString("FaceCam", choice);
		onCameraChosen.Invoke(choice);
		Debug.Log("User chose face camera: " + choice);
	}
}
