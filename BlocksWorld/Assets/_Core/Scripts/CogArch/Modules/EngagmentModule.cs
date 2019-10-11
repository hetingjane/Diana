/*
This module checks for engagement based on spine position

Writes:		user:isEngaged = bool


Reads:      user:joint:SpineBase


May further update based on head/body orientation  
*/

using System.Collections;
using System.Collections.Generic;
using System;
using MathNet.Numerics;
using UnityEngine;

public class EngagmentModule : ModuleBase
{
    string spineBaseKey = "user:joint:SpineBase";
    public float maxZ = 2.0f, minZ = 1.6f, maxX = 0.8f, minX = -0.8f; 
    protected override void Start()
    {
        base.Start();

        DataStore.Subscribe(spineBaseKey, Handler);
    }

    void Handler(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }

    protected void Update()
    {
        bool spineBaseExists = DataStore.HasValue(spineBaseKey);
        Vector3 spineBasePoint;
        if (spineBaseExists)
        {
            //Debug.Log(DataStore.GetVector3Value(spineBaseKey).ToString());
            spineBasePoint = DataStore.GetVector3Value(spineBaseKey);
            if (isValidPosition(spineBasePoint))
            {
                DataStore.SetValue("user:isEngaged", new DataStore.BoolValue(true), this, "The user is in the engagment zone");
            }
            else
            {
                DataStore.SetValue("user:isEngaged", new DataStore.BoolValue(false), this, "The user is not in the engagment zone");
            }
        }
    }

    private bool isValidPosition(Vector3 spineBasePoint)
    {
        return spineBasePoint.x > minX && spineBasePoint.x < maxX && spineBasePoint.z > minZ && spineBasePoint.z < maxZ;
    }


}