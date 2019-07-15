/*
This module captures joint position from Kinect Skeleton frame and posts to the blackboard.

Writes:		user:joint:<jointType> = Vector3
			user:joint:tracked:<jointType> = Boolean
            user:joint:inferred:<jointType> = Boolean
*/

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Windows.Kinect;
using Perception.Kinect;
using Perception.Frames;
using Affdex;

public class AffectModule : ModuleBase
{
    private Perception.Kinect.KinectSensor sensor;
    private MultiSourceFrameReader multiSourceFrameReader;
    private static double engageMin = 0;
    private static double engageMax = 5;


    private int i = 0;
    Stopwatch sw = new Stopwatch();
    private double totalTime = 0.0;


    Detector detector;
    Affdex.Frame frame;
    protected override void Start()
    {
        base.Start();
        sw.Start();
        sensor = new Perception.Kinect.KinectSensor(Perception.Kinect.KinectSensor.FrameType.Color);

        //This code sets the function to be executed on the arribal of a frame
        sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
        detector = GetComponent<Detector>();
        //SetValue("user:joy", 0, "initialization");
        //SetValue("user:sadness", 0, "initialization");
    }


    private void OnMultiSourceFrameArrived(object sender, Perception.Kinect.MultiSourceFrameArrivedEventArgs e)
    {
        
        if (e.ColorFrame != null)
        {
            //UnityEngine.Debug.LogError("frame received!");
            sw.Stop();
            totalTime += sw.Elapsed.TotalSeconds;
            DataStore.SetValue("Time:per:frame", new DataStore.StringValue((++i / totalTime).ToString()), this, sw.Elapsed.ToString());
            sw.Restart();
            //var closestBodyFrame = new ClosestBodyFrame(e.BodyFrame, engageMin, engageMax);

            //var headColorFrame = new HeadColorFrame(e.ColorFrame, closestBodyFrame);
            var colorFrame = new Perception.Frames.ColorFrame(e.ColorFrame);
            byte[] data = colorFrame.colorData;
            var colorArray = new Color32[data.Length / 4];
            for (var i = 0; i < data.Length; i += 4)
            {
                var color = new Color32(data[i + 0], data[i + 1], data[i + 2], data[i + 3]);
                colorArray[i / 4] = color;
            }
            //Color32[] data = colorFrame.GetBytes();

            //Debug.LogError(Time.realtimeSinceStartup);
            //UnityEngine.Debug.LogError(colorArray.GetValue(0));
            //colorFrame.Timestamp
            frame = new Affdex.Frame(colorArray, colorFrame.Width, colorFrame.Height, Time.realtimeSinceStartup);
            //UnityEngine.Debug.LogError("rgba"+frame.rgba.GetValue(0));
            if (detector.IsRunning)
            {
                detector.ProcessFrame(frame);
            }
        }
        //else { UnityEngine.Debug.LogError("frame null!"); }
    }


}
