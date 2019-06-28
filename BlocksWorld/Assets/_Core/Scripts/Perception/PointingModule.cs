/*
This module computes pointing position based on joint locations and writes location to black bloard.

Writes:		user:joint:<jointType> = Vector3
			user:joint:tracked:<jointType> = Boolean
            user:joint:inferred:<jointType> = Boolean
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
//using Microsoft.Kinect;
using Perception.Kinect;
using Perception.Frames;

public class PointingModule : ModuleBase
{

    string handTipLeftKey = "user:joint:HandTipLeft";
    string shoulderLeftKey = "user:joint:ShoulderLeft";
    string handTipRightKey = "user:joint:HandTipRight";
    string shoulderRightKey = "user:joint:ShoulderRight";

    bool leftPoint = false;
    bool rightPoint = false;

    int windowSize = 5;

    List<Vector3> handTipLeftVal = new List<Vector3>();
    List<Vector3> shoulderLeftVal = new List<Vector3>();
    List<Vector3> handTipRightVal = new List<Vector3>();
    List<Vector3> shoulderRightVal = new List<Vector3>();

    protected override void Start()
    {
        base.Start();
        DataStore.Subscribe(handTipLeftKey, NoteScreenOrDeskMode);
        DataStore.Subscribe(shoulderLeftKey, NoteScreenOrDeskMode);

        DataStore.Subscribe(handTipRightKey, NoteScreenOrDeskMode);
        DataStore.Subscribe(shoulderRightKey, NoteScreenOrDeskMode);
    }

    void NoteScreenOrDeskMode(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }
    protected void Update()
    {
        Vector3 avgHandTipLeft, avgShoulderLeft, avgHandTipRight, avgShoulderRight, rightPointPos, leftPointPos;

        if (DataStore.HasValue(handTipLeftKey) && DataStore.HasValue(shoulderLeftKey)) leftPoint = true;
        if (DataStore.HasValue(handTipRightKey) && DataStore.HasValue(shoulderRightKey)) rightPoint = true;

        if (leftPoint)
        {
            handTipLeftVal.Insert(0, DataStore.GetVector3Value(handTipLeftKey));
            shoulderLeftVal.Insert(0,DataStore.GetVector3Value(shoulderLeftKey));

            if (handTipLeftVal.Count > windowSize) handTipLeftVal.RemoveAt(windowSize);
            if (shoulderLeftVal.Count > windowSize) shoulderLeftVal.RemoveAt(windowSize);

            avgHandTipLeft = AverageJoint(handTipLeftVal);
            avgShoulderLeft = AverageJoint(shoulderLeftVal);

            leftPointPos = CalcCoordinates(avgHandTipLeft, avgShoulderLeft);
            
            if (leftPointPos.x != -float.MaxValue)
                DataStore.SetValue("user:pointpos:left", new DataStore.Vector3Value(leftPointPos), this, leftPointPos.ToString());
        }

        if (rightPoint)
        {
            handTipRightVal.Insert(0, DataStore.GetVector3Value(handTipRightKey));
            shoulderRightVal.Insert(0, DataStore.GetVector3Value(shoulderRightKey));

            if (handTipRightVal.Count > windowSize) handTipRightVal.RemoveAt(windowSize);
            if (shoulderRightVal.Count > windowSize) shoulderRightVal.RemoveAt(windowSize);

            avgHandTipRight = AverageJoint(handTipRightVal);
            avgShoulderRight = AverageJoint(shoulderRightVal);

            rightPointPos = CalcCoordinates(avgHandTipRight, avgShoulderRight);

            if(rightPointPos.x != -float.MaxValue)
                DataStore.SetValue("user:pointpos:right", new DataStore.Vector3Value(rightPointPos), this, rightPointPos.ToString());
        }
    }

    // smoothing will likely help but will come later
    private void SmoothJoint(List<Vector3> joint)
    {
        if (joint.Count == windowSize)
        {

        }
    }

    private Vector3 AverageJoint(List<Vector3> jointList)
    {
        Vector3 averageJointPos = new Vector3(0.0f, 0.0f, 0.0f);

        foreach(Vector3 joint in jointList)
        {
            averageJointPos += joint;
        }
        return averageJointPos / windowSize;
    }

    private Vector3 CalcCoordinates(Vector3 handTip, Vector3 shoulder)
    {
        Vector3 direction = new Vector3(handTip.x - shoulder.x, handTip.y - shoulder.y, handTip.z - shoulder.z);
        float t, x, y;

        t = (0 - shoulder.z) / direction.z;

        if (t < 0.0f) return new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        x = shoulder.x + t * direction.x;
        y = shoulder.y + t * direction.y;

        return new Vector3(x, y, 0);
    }


}