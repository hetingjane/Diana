/*
This module captures joint position from Kinect Skeleton frame and posts to the blackboard.

Writes:		user:joint:<jointType> = Vector3
			user:joint:tracked:<jointType> = Boolean
            user:joint:inferred:<jointType> = Boolean
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using Perception.Kinect;
using Perception.Frames;

public class SkeletonFrame : ModuleBase
{
    private Perception.Kinect.KinectSensor sensor;
    private MultiSourceFrameReader multiSourceFrameReader;
    private static double engageMin = 0;
    private static double engageMax = 5;

    protected override void Start()
    {
        base.Start();
        sensor = new Perception.Kinect.KinectSensor(Perception.Kinect.KinectSensor.FrameType.Body);
        
        //This code sets the function to be executed on the arribal of a frame
        sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
    }


    protected void Update()
    {

    }

    private void OnMultiSourceFrameArrived(object sender, Perception.Kinect.MultiSourceFrameArrivedEventArgs e)
    {
        if (e.BodyFrame != null)
        {
            var bodyFrame = new ClosestBodyFrame(e.BodyFrame, engageMin, engageMax);

            foreach(var joint in bodyFrame.Joints)
            {
                var jointVectorValue = new DataStore.Vector3Value(new Vector3(joint.Value.Position.X, joint.Value.Position.Y, joint.Value.Position.Z));
                DataStore.SetValue("user:joint:" + joint.Key.ToString(), jointVectorValue, this, joint.Value.JointType.ToString());
                //var isTracked = new DataStore.BoolValue(joint.Value.TrackingState == Windows.Kinect.TrackingState.Tracked);
                //var isInferred = new DataStore.BoolValue(joint.Value.TrackingState == Windows.Kinect.TrackingState.Inferred);
                //DataStore.SetValue("user:joint:tracked:" + joint.Key.ToString(), isTracked, this, "");
                //DataStore.SetValue("user:joint:inferred:" + joint.Key.ToString(), isInferred, this, "");
            }
            DataStore.SetValue("user:joint:timestamp", new DataStore.StringValue(e.Timestamp.ToString()), this, e.Timestamp.ToString());
            DataStore.SetValue("user:engaged", new DataStore.BoolValue(bodyFrame.Engaged), this, bodyFrame.Engaged.ToString());
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Closing sensor");
	    if (sensor != null) sensor.Close();
        Debug.Log("Closed the sensor");
    }
}
