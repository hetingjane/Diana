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
        bool cameraSet = false;
		foreach (var cam in WebCamTexture.devices) {
			var opt = new TMP_Dropdown.OptionData();
			opt.text = cam.name;
			cameraDropdown.options.Add(opt);
            if (cam.name.Contains("4310")) //look for our preferred camera
            {
                curCamIdx  = cameraDropdown.options.Count - 1;
                cameraSet = true; //prevent camera from being changed to current
            }
            else if (cam.name == curCamName && !cameraSet)
            {
                curCamIdx = cameraDropdown.options.Count - 1;
            }
		}
		cameraDropdown.value = curCamIdx;
        NoteCameraOptionChanged(curCamIdx);
    }
	
	public void NoteCameraOptionChanged(int value) {
		string choice = cameraDropdown.options[value].text;
		PlayerPrefs.SetString("FaceCam", choice);
		onCameraChosen.Invoke(choice);
		Debug.Log("User chose face camera: " + choice);
	}
}
