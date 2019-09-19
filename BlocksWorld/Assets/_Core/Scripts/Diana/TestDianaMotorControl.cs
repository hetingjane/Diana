using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDianaMotorControl : MonoBehaviour
{
	public Transform target;

	public bool followTarget;

    // Start is called before the first frame update
    void Start()
    {
		followTarget = false;
    }

    // Update is called once per frame
    void Update()
    {
		if (target != null && followTarget)
		{
			DataStore.SetValue("me:intent:handPosR", new DataStore.Vector3Value(target.position), null, "");
			followTarget = false;
		}
    }
}
