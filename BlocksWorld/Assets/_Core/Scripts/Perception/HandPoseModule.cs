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
using System.Diagnostics;

public class HandPoseModule : ModuleBase
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
	            Arguments = "External/HandPose/depth_client.py",
	            UseShellExecute = true,
	            //RedirectStandardOutput = true,
	            //RedirectStandardError = true
	        });
	        started = true;
		    UnityEngine.Debug.Log("Started hand pose client");
	    } catch (System.Exception e) {
	    	UnityEngine.Debug.LogWarning("Error starting hand pose client: " + e);
	    	started = false;
	    }
	}

    protected void Update()
    {
	    if (started && process.HasExited)
        {
            UnityEngine.Debug.Log("Hand pose client exited unexpectedly");
            started = false;
        }
    }

    private void OnApplicationQuit()
	{
		if (process != null) {
	        UnityEngine.Debug.Log("Closing hand pose client");
		    process.Close();
			UnityEngine.Debug.Log("Hand pose client closed");
		}
    }
}