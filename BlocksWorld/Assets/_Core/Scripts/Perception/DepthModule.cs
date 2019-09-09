/*
This module will be responsible for gathering depth frames as well as depth masks and sending them to CANet.  


Writes:		

Reads: 

*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using Perception.Kinect;
using Perception.Frames;

public class DepthModule : ModuleBase
{
    private Perception.Kinect.KinectSensor sensor;

    private Perception.Frames.DepthFrame depthFrame;
    private Windows.Kinect.DepthFrame underlyingDepthFrame;

    private static double engageMin = 0;
    private static double engageMax = 5;

    string handLeftKey = "user:joint:HandLeft";
    string handRightKey = "user:joint:HandRight";
    string timestampKey = "user:joint:timestamp";
    string engagedKey = "user:engaged";

    protected override void Start()
    {
        base.Start();
        sensor = new Perception.Kinect.KinectSensor(Perception.Kinect.KinectSensor.FrameType.Depth);

        DataStore.Subscribe(handRightKey, Handler);
        DataStore.Subscribe(handLeftKey, Handler);
        DataStore.Subscribe(timestampKey, Handler);
        DataStore.Subscribe(engagedKey, Handler);


        //This code sets the function to be executed on the arribal of a frame
        sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
    }

    void Handler(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }

    protected void Update()
    {

    }

    private void OnMultiSourceFrameArrived(object sender, Perception.Kinect.MultiSourceFrameArrivedEventArgs e)
    {
        if (e.DepthFrame != null)
        {
            depthFrame = new Perception.Frames.DepthFrame(e.DepthFrame);

            if(DataStore.HasValue(handRightKey))
            {
                // get right hand position in depth space
            }
            if(DataStore.HasValue(handLeftKey))
            {
                // get left hand position in depth space
            }

        }
    }

    private Vector3 GetHandPosition(string temp)
    {
        return new Vector3();
    }


    private void OnApplicationQuit()
    {
        sensor.Close();
    }
}