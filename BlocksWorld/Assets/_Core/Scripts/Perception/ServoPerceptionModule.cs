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

public class ServoPerceptionModule : ModuleBase
{
    string handRightOrientationKey = "user:jointOrientation:HandRight";
    string wristRightOrientationKey = "user:jointOrientation:WristRight";
    string handLeftOrientationKey = "user:jointOrientation:HandLeft";
    string wristLeftOrientationKey = "user:jointOrientation:WristLeft";
    string leftLabel = "user:armMotion:left";
    string rightLabel = "user:armMotion:right";
    Quaternion wristRight, handRight, wristLeft, handLeft;
    int windowSize = 10;
    List<float> handRightAngles = new List<float>();
    List<float> wristRightAngles = new List<float>();
    List<float> handLeftAngles = new List<float>();
    List<float> wristLeftAngles = new List<float>();
    System.Numerics.Vector2 leftLabelProb, rightLabelProb;

    List<string> LabelList = new List<string>() { "still", "servo" };


    protected override void Start()
    {
        base.Start();

        DataStore.Subscribe(handRightOrientationKey, Handler);
        DataStore.Subscribe(wristRightOrientationKey, Handler);
        DataStore.Subscribe(handLeftOrientationKey, Handler);
        DataStore.Subscribe(wristLeftOrientationKey, Handler);

        leftLabelProb = new System.Numerics.Vector2(0.0f, 0.0f);
        rightLabelProb = new System.Numerics.Vector2(0.0f, 0.0f);
    }

    void Handler(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }

    protected void Update()
    {
        

        bool rightArmExists = DataStore.HasValue(handRightOrientationKey) && DataStore.HasValue(wristRightOrientationKey);
        //Debug.Log(rightArmExists.ToString());
        bool leftArmExists = DataStore.HasValue(handLeftOrientationKey) && DataStore.HasValue(wristLeftOrientationKey);
        //Debug.Log(leftArmExists.ToString());

        System.Numerics.Vector2 tempRight, tempLeft;
        if (rightArmExists)
        {
            Vector3 axis;
            float angle;
            handRight = DataStore.GetQuaternionValue(handRightOrientationKey);
            handRight.ToAngleAxis(out angle, out axis);
            handRightAngles.Insert(0, angle);
            if (handRightAngles.Count > windowSize) handRightAngles.RemoveAt(windowSize);

            wristRight = DataStore.GetQuaternionValue(wristRightOrientationKey);
            wristRight.ToAngleAxis(out angle, out axis);
            wristRightAngles.Insert(0, angle);
            if (wristRightAngles.Count > windowSize) wristRightAngles.RemoveAt(windowSize);

            string s = handRightAngles.Max() + " " + handRightAngles.Min() + " " + wristRightAngles.Max() + " " + wristRightAngles.Min();
            if (handRightAngles.Max() - handRightAngles.Min() > 80 || wristRightAngles.Max() - wristRightAngles.Min() > 50)
            {
                //SetValue("user:armMotion:right", "servo", "based on handRight angle");
                tempRight = new System.Numerics.Vector2(0.0f, 1.0f);
            }
            else
            {
                //SetValue("user:armMotion:right", "still", "based on handRight angle");
                tempRight = new System.Numerics.Vector2(1.0f, 0.0f);
            }

            rightLabelProb = (rightLabelProb + tempRight) / 2;
            string rightPred = LabelList.ElementAt(rightLabelProb.X > rightLabelProb.Y ? 0 : 1);
            SetValue("user:armMotion:right", rightPred, "based on right arm joint angles");
        }
        if (leftArmExists)
        {
            Vector3 axis;
            float angle;
            handLeft = DataStore.GetQuaternionValue(handLeftOrientationKey);
            handLeft.ToAngleAxis(out angle, out axis);
            handLeftAngles.Insert(0, angle);
            if (handLeftAngles.Count > windowSize) handLeftAngles.RemoveAt(windowSize);

            wristLeft = DataStore.GetQuaternionValue(wristLeftOrientationKey);
            wristLeft.ToAngleAxis(out angle, out axis);
            wristLeftAngles.Insert(0, angle);
            if (wristLeftAngles.Count > windowSize) wristLeftAngles.RemoveAt(windowSize);

            string s = handLeftAngles.Max() + " " + handLeftAngles.Min() + " " + wristLeftAngles.Max() + " " + wristLeftAngles.Min();
            if (handLeftAngles.Max() - handLeftAngles.Min() > 80 || wristLeftAngles.Max() - wristLeftAngles.Min() > 80)
            {
                //SetValue("user:armMotion:left", "servo", "based on handLeft angle");
                tempLeft = new System.Numerics.Vector2(0.0f, 1.0f);
            }
            else
            {
                //SetValue("user:armMotion:left", "still", "based on handLeft angle");
                tempLeft = new System.Numerics.Vector2(1.0f, 0.0f);
            }

            leftLabelProb = (leftLabelProb + tempLeft) / 2;
            string leftPred = LabelList.ElementAt(leftLabelProb.X > leftLabelProb.Y ? 0 : 1);
            SetValue("user:armMotion:left", leftPred, "based on left arm joint angles");
        }
    }



}