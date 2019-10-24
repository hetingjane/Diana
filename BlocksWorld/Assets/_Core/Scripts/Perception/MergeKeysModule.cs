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

public class MergeKeysModule : ModuleBase
{
    string armsMotionsLeftKey = "user:armMotion:left";
    string armsMotionsRightKey = "user:armMotion:right";
    string armsLeftKey = "user:arms:left";
    string armsRightKey = "user:arms:right";
    
    System.Numerics.Vector2 leftLabelProb, rightLabelProb;

    List<string> LabelList = new List<string>() { "still", "servo" };


    protected override void Start()
    {
        base.Start();

        DataStore.Subscribe(armsMotionsLeftKey, Handler);
        DataStore.Subscribe(armsMotionsRightKey, Handler);
        DataStore.Subscribe(armsLeftKey, Handler);
        DataStore.Subscribe(armsRightKey, Handler);
    }

    void Handler(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }

    protected void Update()
    {
        string rightArmGesture = DataStore.GetStringValue(armsRightKey);
        string rightArmServo = DataStore.GetStringValue(armsMotionsRightKey);
        if (rightArmGesture != "still" && rightArmGesture != "servo")
        {
            SetValue("user:armGesture:right", rightArmGesture, "based on handRight angle");
        }
        else
        {
            SetValue("user:armGesture:right", rightArmServo, "right still from servo Module");
        }


        string leftArmGesture = DataStore.GetStringValue(armsLeftKey);
        string leftArmServo = DataStore.GetStringValue(armsMotionsLeftKey);
        if (leftArmGesture != "still" && leftArmGesture != "servo")
        {
            SetValue("user:armGesture:left", leftArmGesture, "based on leftArm gesture");
        }
        else
        {
            SetValue("user:armGesture:left", leftArmServo, "left still from servo Module");
        }
    }



}