/*
This module captures joint position from Kinect Skeleton frame and posts to the blackboard.

Writes:		user:joint:<jointType> = Vector3
			user:joint:tracked:<jointType> = Boolean
            user:joint:inferred:<jointType> = Boolean
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Windows.Kinect;
using Perception.Kinect;
using Perception.Frames;

public class SkeletonModule : ModuleBase
{
    private Perception.Kinect.KinectSensor sensor;
    private MultiSourceFrameReader multiSourceFrameReader;
    private static double engageMin = 0;
    private static double engageMax = 5;
    string filename = @"u:\myCSU\cwc\skeletonOutput.csv";
    StreamWriter sw;
    bool firstIter = true;

    protected override void Start()
    {
        
        //sw = File.CreateText(filename);
        

	    base.Start();
	    try {
		    sensor = new Perception.Kinect.KinectSensor(Perception.Kinect.KinectSensor.FrameType.Body);
	    } catch (System.DllNotFoundException e) {
	    	Debug.LogWarning("Kinect DLL not available; SkeletonFrame module disabled");
	    	gameObject.SetActive(false);
	    	return;
	    }
        
        //This code sets the function to be executed on the arribal of a frame
        sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
    }


    protected void Update()
    {

    }

    private void OnMultiSourceFrameArrived(object sender, Perception.Kinect.MultiSourceFrameArrivedEventArgs e)
    {
        //Debug.Log("Frame has arrived");
        if (e.BodyFrame != null)
        {
            var bodyFrame = new ClosestBodyFrame(e.BodyFrame, engageMin, engageMax);

           

            foreach(var joint in bodyFrame.Joints)
            {
                var jointVectorValue = new DataStore.Vector3Value(new Vector3(joint.Value.Position.X, joint.Value.Position.Y, joint.Value.Position.Z));
                DataStore.SetValue("user:joint:" + joint.Key.ToString(), jointVectorValue, this, joint.Value.JointType.ToString());

                var jointOrientationValue = bodyFrame.JointOrientations[joint.Key];
                var q = new Quaternion(jointOrientationValue.Orientation.X, jointOrientationValue.Orientation.Y, jointOrientationValue.Orientation.Z, jointOrientationValue.Orientation.W);
                //Vector3 axis;
                //float angle;
                //q.ToAngleAxis(out angle, out axis);
                string name = joint.Key.ToString().ToLower();
                if(name.Contains("wrist") || name.Contains("hand"))
                {
                    SetValue("user:jointOrientation:" + joint.Key, q, "from kinect body frame");
                }
                //var isTracked = new DataStore.BoolValue(joint.Value.TrackingState == Windows.Kinect.TrackingState.Tracked);
                //var isInferred = new DataStore.BoolValue(joint.Value.TrackingState == Windows.Kinect.TrackingState.Inferred);
                //DataStore.SetValue("user:joint:tracked:" + joint.Key.ToString(), isTracked, this, "");
                //DataStore.SetValue("user:joint:inferred:" + joint.Key.ToString(), isInferred, this, "");

                //if(firstIter)
                //{
                //    sw.Write(joint.Key + "_x" + "," + 
                //        joint.Key + "_y" + "," + 
                //        joint.Key + "_z" +"," + 
                //        joint.Key + "_angle" + "," + 
                //        joint.Key + "_axis_x" + "," +
                //        joint.Key + "_axis_y" + "," +
                //        joint.Key + "_axis_z" + "," 
                //        );
                //
                //}
                //else
                //{
                //
                //    sw.Write(jointVectorValue.val.x + "," + 
                //        jointVectorValue.val.y + "," + 
                //        jointVectorValue.val.z + "," +
                //        angle + "," + 
                //        axis.x + "," + 
                //        axis.y + "," + 
                //        axis.z + ",");
                //}
            }
            firstIter = false;
            //sw.Write("\n");
            DataStore.SetValue("user:joint:timestamp", new DataStore.StringValue(e.Timestamp.ToString()), this, e.Timestamp.ToString());
           
        }
        else
        {
        //    Debug.Log("But frame is null");
        }
       
    }

    private void OnApplicationQuit()
    {
        //sw.Close();
        Debug.Log("Closing sensor");
	    if (sensor != null) sensor.Close();
        Debug.Log("Closed the sensor");
    }
}
