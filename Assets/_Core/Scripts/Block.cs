using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour {

	public Vector3 Position
    {
        get { return gameObject.transform.position; }
    }

    public Vector3 Rotation
    {
        get { return gameObject.transform.eulerAngles; }
    }

    public GameObject Handle
    {
        get {
            var t = transform.Find("Handle");
            if (t != null)
                return t.gameObject;
            else
                return null;
        }
    }
}
