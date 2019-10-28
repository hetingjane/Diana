/*
This module handles skeletion

Writes:		user:isEngaged = bool


Reads:      user:joint:SpineBase


May further update based on head/body orientation  
*/

using System.Collections;
using System.Collections.Generic;
using System;
using MathNet.Numerics;
using UnityEngine;
using System.Linq;

public class ArmMotionModule : ModuleBase
{
    string handRightOrientationKey = "user:jointOrientation:HandRight";
    string wristRightOrientationKey = "user:jointOrientation:WristRight";
    Quaternion wristRight, handRight;
    int windowSize = 10;
    List<float> handRightAngles = new List<float>();
    List<float> wristRightAngles = new List<float>();
    List<Vector3> handRightAxis = new List<Vector3>();
    List<Vector3> wristRightAxis = new List<Vector3>();

    protected override void Start()
    {
        base.Start();

        DataStore.Subscribe(handRightOrientationKey, Handler);
        DataStore.Subscribe(wristRightOrientationKey, Handler);
    }

    void Handler(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }

    protected void Update()
    {
        Vector3 axis;
        float angle;

        bool rightArmExists = DataStore.HasValue(handRightOrientationKey) && DataStore.HasValue(wristRightOrientationKey);
        Debug.Log(rightArmExists.ToString());
        if (rightArmExists)
        {
            handRight = DataStore.GetQuaternionValue(handRightOrientationKey);
            handRight.ToAngleAxis(out angle, out axis);
            handRightAngles.Insert(0, angle);
            if (handRightAngles.Count > windowSize) handRightAngles.RemoveAt(windowSize);

            wristRight = DataStore.GetQuaternionValue(wristRightOrientationKey);
            wristRight.ToAngleAxis(out angle, out axis);
            wristRightAngles.Insert(0, angle);
            if (wristRightAngles.Count > windowSize) wristRightAngles.RemoveAt(windowSize);

            string s = handRightAngles.Max() + " " + handRightAngles.Min() + " " + wristRightAngles.Max() + " " + wristRightAngles.Min();
            if (handRightAngles.Max() - handRightAngles.Min() > 100 || wristRightAngles.Max() - wristRightAngles.Min() > 100)
            {
                DataStore.SetValue("user:armMotion:right", new DataStore.StringValue("servo"+s), this, "based on handRight angle");
            }
            else
            {
                DataStore.SetValue("user:armMotion:right", new DataStore.StringValue("still"+s), this, "based on handRight angle");
            }

            
        }
    }



}