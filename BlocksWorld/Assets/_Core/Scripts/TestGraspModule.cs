using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

public class TestGraspModule : MonoBehaviour
{
	public Transform manipulableObjectsRoot;

	public bool doReachRandomObject;
	public bool doMoveRandomLocation;
	public bool doUnreach;
	public bool doHold;
	public bool doRelease;

	/// <summary>
	/// Offset of the block's position (geometrical center in world coordinates) w.r.t hand bone.
	/// Hand bone position + hold offset = block position
	/// (Block) Position - hold offset = Hand bone position 
	/// </summary>
	private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);

	// Start is called before the first frame update
	void Start()
    {
		doReachRandomObject = false;
		doMoveRandomLocation = false;
		doUnreach = false;

		manipulableObjects = new GameObject[manipulableObjectsRoot.childCount];
		for (int i=0; i<manipulableObjects.Length; i++)
		{
			manipulableObjects[i] = manipulableObjectsRoot.GetChild(i).gameObject;
		}

		targetBlock = GetRandomObject();
    }

	private Vector3 GetRandomLocation()
	{
		var x = Random.Range(-.45f, .45f);
		var y = Random.Range(1.1f, 2f);
		var z = Random.Range(-.25f, .25f);
		return new Vector3(x, y, z);
	}

	private GameObject[] manipulableObjects;

	private GameObject targetBlock;

	private GameObject GetRandomObject()
	{
		return manipulableObjects[Random.Range(0, manipulableObjects.Length)];
	}

    // Update is called once per frame
    void Update()
    {
		doReachRandomObject = doReachRandomObject || Input.GetKeyDown(KeyCode.J);
		doMoveRandomLocation = doMoveRandomLocation || Input.GetKeyDown(KeyCode.K);
		doUnreach = doUnreach || Input.GetKeyDown(KeyCode.L);
		doHold = doHold || Input.GetKeyDown(KeyCode.Semicolon);
		doRelease = doRelease || Input.GetKeyDown(KeyCode.Quote);

		if (doReachRandomObject)
		{
			targetBlock = GetRandomObject();
			DataStore.SetValue("me:intent:action", new DataStore.StringValue("reach"), null, "");
			DataStore.SetValue("me:intent:targetName", new DataStore.StringValue(targetBlock.name), null, "");
			// Either by name or location, we successfully resolved the target object
			// Get bounds of the Voxeme geometry
			var bounds = GlobalHelper.GetObjectWorldSize(targetBlock.gameObject);

			// The default set down position is the same as the initial position of the block
			// in case the user backs out of the action
			var setDownPos = bounds.center + Vector3.down * bounds.extents.y;

			// Set the reach target to be within grabbing distance to the target
			var curReachTarget = setDownPos + Vector3.up * bounds.size.y - holdOffset;

			DataStore.SetValue("me:intent:target", new DataStore.Vector3Value(curReachTarget), null, "");
			doReachRandomObject = false;
		}

		else if (doMoveRandomLocation)
		{
			DataStore.SetValue("me:intent:action", new DataStore.StringValue("move"), null, "");
			Vector3 v = GetRandomLocation();
			DataStore.SetValue("me:intent:target", new DataStore.Vector3Value(v), null, "");
			doMoveRandomLocation = false;
		}

		else if (doUnreach)
		{
			DataStore.SetValue("me:intent:action", new DataStore.StringValue("unreach"), null, "");
			doUnreach = false;
		}
		else if (doHold)
		{
			DataStore.SetValue("me:intent:action", new DataStore.StringValue("hold"), null, "");
			doHold = false;
		}
		else if (doRelease)
		{
			DataStore.SetValue("me:intent:action", new DataStore.StringValue("release"), null, "");
			doRelease = false;
		}
    }

	private void OnDrawGizmos()
	{
		if (DataStore.instance != null)
		{
			if (DataStore.GetStringValue("me:intent:action") == "move")
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(DataStore.GetVector3Value("me:intent:target"), 0.01f);
			}
		}
	}
}
