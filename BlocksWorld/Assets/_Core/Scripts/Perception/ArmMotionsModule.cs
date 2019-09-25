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
	private ExternalProcess armMotionsRecognizer;
	private bool endedExternally = false;

	protected override void Start()
	{
		base.Start();

		string python = PlayerPrefs.GetString("pythonPath", "python.exe");
		if (string.IsNullOrEmpty(python)) {
			Debug.Log("Python path is empty; skipping " + gameObject.name);
			endedExternally = true;
			return;
		}

		armMotionsRecognizer = new ExternalProcess(
			pathToExecutable: python,
			arguments: "External/Perception/skeleton_client.py"
		);

		armMotionsRecognizer.Hide = true;

		armMotionsRecognizer.Start();

		if (armMotionsRecognizer.HasStarted)
		{
			Debug.Log("Started arm motions client");
		}
		else
		{
			Debug.LogWarning("Error starting arm motions client: " + armMotionsRecognizer.ErrorLog);
		}
	}

	protected void Update()
	{
		if (!endedExternally && armMotionsRecognizer.HasExited)
		{
			Debug.LogError("Arm motions client exited unexpectedly: " + armMotionsRecognizer.ErrorLog);
			endedExternally = true;
		}
	}

	private void OnApplicationQuit()
	{
		if (armMotionsRecognizer != null && armMotionsRecognizer.HasStarted && !armMotionsRecognizer.HasExited)
		{
			armMotionsRecognizer.Close();
			Debug.Log("Arm motions client closed");
		}
	}
}