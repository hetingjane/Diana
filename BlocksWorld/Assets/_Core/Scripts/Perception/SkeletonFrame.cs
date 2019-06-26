/*
This module controls Diana's attention.

Reads:		user:isSpeaking (BoolValue)
			me:intent:action (StringValue)
Writes:		me:attending (StringValue)
			me:alertness (IntValue: 0 = comatose, 7 = normal, 10 = hyperexcited)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
//using Microsoft.Kinect;
using Perception.Kinect;
using Perception.Frames;

public class SkeletonFrame : ModuleBase
{

    [Header("Configuration")]
    [Tooltip("How long (in sec) before our attention starts to wander")]
    public float attentionWanderAfter = 10f;

    // Last time at which something grabbed our attention:
    float lastSalientTime;
    private Perception.Kinect.KinectSensor sensor;
    private MultiSourceFrameReader multiSourceFrameReader;
    private static double engageMin = 1.5;
    private static double engageMax = 5;

    protected override void Start()
    {
        base.Start();
        sensor = new Perception.Kinect.KinectSensor(Perception.Kinect.KinectSensor.FrameType.Body);
        sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
        DataStore.SetValue("We are executing skeleton module1", new DataStore.BoolValue(false), this, "");
    }


    protected void Update()
    {
        DataStore.SetValue("We are executing skeleton module2", new DataStore.BoolValue(false), this, "");
        //sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
        DataStore.SetValue("We are executing skeleton module4", new DataStore.BoolValue(false), this, "");
    }

    private void OnMultiSourceFrameArrived(object sender, Perception.Kinect.MultiSourceFrameArrivedEventArgs e)
    {
        if (e.BodyFrame != null)
        {
            var bodyFrame = new ClosestBodyFrame(e.BodyFrame, engageMin, engageMax);

            foreach(var joint in bodyFrame.Joints)
            {
                DataStore.SetValue("We are executing skeleton module2", new DataStore.BoolValue(true), this, "");
                var jointVectorValue = new DataStore.Vector3Value(new Vector3(joint.Value.Position.X, joint.Value.Position.Y, joint.Value.Position.Z));
                DataStore.SetValue("user:joint:" + joint.Key.ToString(), jointVectorValue, this, joint.Value.JointType.ToString());
                var isTracked = new DataStore.BoolValue(joint.Value.TrackingState == Windows.Kinect.TrackingState.Tracked);
                var isInferred = new DataStore.BoolValue(joint.Value.TrackingState == Windows.Kinect.TrackingState.Inferred);
                DataStore.SetValue("user:joint:tracked:" + joint.Key.ToString(), isTracked, this, "");
                DataStore.SetValue("user:joint:inferred:" + joint.Key.ToString(), isInferred, this, "");
            }
        }
    }
}
