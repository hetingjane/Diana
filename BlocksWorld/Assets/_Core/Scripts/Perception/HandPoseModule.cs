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
        process = Process.Start(new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "External/HandPose/depth_client.py",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        started = true;
        UnityEngine.Debug.Log("Started hand pose client");
    }

    protected void Update()
    {
        if (process.HasExited && started)
        {
            string message = process.StandardOutput.ReadToEnd();
            UnityEngine.Debug.Log("Hand pose client exited unexpectedly, see output:" + message);
            string error = process.StandardError.ReadToEnd();
            UnityEngine.Debug.LogError("Hand pose client error message:\n" + error);
            started = false;
        }
    }

    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("Closing hand pose client");
        process.Close();
        UnityEngine.Debug.Log("Hand pose client closed");
    }
}