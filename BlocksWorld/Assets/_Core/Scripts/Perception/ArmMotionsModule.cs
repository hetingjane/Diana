/*
This module simply starts/stops the skeleton python client for arm motions.

Writes:		
    user:arms:left:label
    user:arms:right:label

Reads: 
    From kinect directly (nothing from blackboard)
*/

using UnityEngine;
using System.Diagnostics;

public class ArmMotionsModule : ModuleBase
{
    Process process;
    bool started;

    protected override void Start()
    {
	    base.Start();
	    try {
	        process = Process.Start(new ProcessStartInfo
	        {
		        FileName = "python.exe",
	            Arguments = "External/Perception/skeleton_client.py",
	            UseShellExecute = true,
	            //RedirectStandardOutput = true,
	            //RedirectStandardError = true
	        });
	        started = true;
		    UnityEngine.Debug.Log("Started arm motions client");
	    } catch (System.Exception e) {
	    	UnityEngine.Debug.LogWarning("Error starting arm motions client: " + e);
	    	started = false;
	    }
	}

    protected void Update()
    {
	    if (started && process.HasExited)
        {
            UnityEngine.Debug.Log("arm motions client exited unexpectedly");
            started = false;
        }
    }

    private void OnApplicationQuit()
	{
		if (process != null) {
	        UnityEngine.Debug.Log("Closing arm motions client");
		    process.Close();
			UnityEngine.Debug.Log("arm motions client closed");
		}
    }
}