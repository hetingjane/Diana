using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppVersionTrigger : MonoBehaviour {

	public StringEvent onAppVersion;
	
	void Start () {
		onAppVersion.Invoke(Application.version);
	}

}
