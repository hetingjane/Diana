/*
This module simply starts/stops the skeleton python client for arm motions.

Writes:		
    user:arms:left:label
    user:arms:right:label

Reads: 
    From kinect directly (nothing from blackboard)
*/

using UnityEngine;

public class ArmMotionsModule : ModuleBase
{
	private ExternalProcess amrMotionsRecognizer;
	private bool endedExternally = false;

	protected override void Start()
	{
		base.Start();

		amrMotionsRecognizer = new ExternalProcess(
			pathToExecutable: "python.exe",
			arguments: "External/Perception/skeleton_client.py"
		);

		amrMotionsRecognizer.Hide = true;

		amrMotionsRecognizer.Start();

		if (amrMotionsRecognizer.HasStarted)
		{
			Debug.Log("Started arm motions client");
		}
		else
		{
			Debug.LogWarning("Error starting arm motions client: " + amrMotionsRecognizer.ErrorLog);
		}
	}

	protected void Update()
	{
		if (!endedExternally && amrMotionsRecognizer.HasExited)
		{
			Debug.LogError("Arm motions client exited unexpectedly: " + amrMotionsRecognizer.ErrorLog);
			endedExternally = true;
		}
	}

	private void OnApplicationQuit()
	{
		if (amrMotionsRecognizer.HasStarted && !amrMotionsRecognizer.HasExited)
		{
			amrMotionsRecognizer.Close();
			Debug.Log("Arm motions client closed");
		}
	}
}