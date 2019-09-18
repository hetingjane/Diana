/*
This module simply starts/stops the skeleton python client for arm motions.

Writes:		
    user:arms:left:label
    user:arms:right:label

Reads: 
    From kinect directly (nothing from blackboard)
*/

using UnityEngine;
using System.Diagnostics;

public class GestureModule : ModuleBase
{
    string leftarmKey = "user:arms:left";
    string lefthandKey = "user:hands:left";
    string rightarmKey = "user:arms:right";
    string handPosition = "me:intent:handPosR";
    public float maxZ = 2.0f, minZ = 1.6f, maxX = 1.1f, minX = -1.1f, minY=1.1f, maxY=1.4f;
    protected override void Start()
    {
        base.Start();

        DataStore.Subscribe(leftarmKey, Handler);
    }

    void Handler(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }

    protected void Update()
    { 
        Vector3 v = DataStore.GetVector3Value(handPosition);
        if (DataStore.GetStringValue("me:intent:action")=="pickUp" && DataStore.GetStringValue(leftarmKey) == "la move right")
        {
            v.x -= 0.01f;
            v.x = Mathf.Clamp(v.x, minX, maxX);
            SetValue("me:intent:handPosR", v, "left hand push right");
        }

        else if (DataStore.GetStringValue(leftarmKey) == "la move down" || DataStore.GetStringValue(lefthandKey) == "thumbs up")
        {
            SetValue("me:intent:action", "setDown", comment);
        }
    }



}
