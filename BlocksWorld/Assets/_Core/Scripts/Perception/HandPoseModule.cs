/*
This module simply starts/stops the hand pose python client.

Writes:		
    user:hands:left:argmax
    user:hands:left:label
    user:hands:right:argmax
    user:hands:right:label

Reads: 
    From kinect directly (nothing from blackboard)
*/

using UnityEngine;

public class HandPoseModule : ModuleBase
{
	private ExternalProcess handPoseRecognizer;
	private bool endedExternally = false;

	protected override void Start()
	{
		base.Start();

		handPoseRecognizer = new ExternalProcess(
			pathToExecutable: "python.exe",
			arguments: "External/Perception/depth_client.py"
		);

		handPoseRecognizer.Hide = true;

		handPoseRecognizer.Start();

		if (handPoseRecognizer.HasStarted)
		{
			Debug.Log("Started hand pose client");
		}
		else
		{
			Debug.LogWarning("Error starting hand pose client: " + handPoseRecognizer.ErrorLog);
		}
	}

	protected void Update()
	{
		if (!endedExternally && handPoseRecognizer.HasExited)
		{
			Debug.LogError("Hand pose client exited unexpectedly: " + handPoseRecognizer.ErrorLog);
			endedExternally = true;
		}
	}

	private void OnApplicationQuit()
	{
		if (handPoseRecognizer.HasStarted && !handPoseRecognizer.HasExited)
		{
			handPoseRecognizer.Close();
			Debug.Log("Hand pose client closed");
		}
	}
}
