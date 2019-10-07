/*
This script interfaces with the VoxSim event manager

Reads:      I don't know yet
Writes:     me:intent:action (StringValue)
            me:intent:targetName (StringValue, name of object that is theme of action)
*/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using VoxSimPlatform.Core;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Global;
using VoxSimPlatform.Pathfinding;

public class EventManagerTestModule : ModuleBase
{
    public EventManager eventManager;

    // need to keep the same as GrabPlaceModule's value but don't want to couple them
    private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);

    List<Vector3> objectMovePath = null;

    // Start is called before the first frame update
    void Start()
    {
        AStarSearch.ComputedPath += GotPath;
    }

    // Update is called once per frame
    void Update()
    {
        string rightArmMotion = DataStore.GetStringValue("me:rightArm:motion");
        Vector3 actualHandPosR = DataStore.GetVector3Value("me:actual:handPosR");

        if (objectMovePath != null)
        {
            if (rightArmMotion == "reached")
            {
                objectMovePath.RemoveAt(0);

                if (objectMovePath.Count > 0)
                {
                    Debug.Log(string.Format("Setting me:intent:handPosR to {0}; me:actual:handPosR is {1}",
                        GlobalHelper.VectorToParsable(objectMovePath.ElementAt(0) - holdOffset),
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:actual:handPosR"))));
                    SetValue("me:intent:handPosR", objectMovePath.ElementAt(0) - holdOffset, string.Empty);
                }
                else
                {
                    objectMovePath = null;
                }
            }
        }
    }

    public void GotPath(object sender, EventArgs e) {
        objectMovePath = ((ComputedPathEventArgs)e).path;
        SetValue("me:intent:handPosR", objectMovePath.ElementAt(0) - holdOffset, string.Empty);
    }

    public void GRASP(object[] args)
    {
        if (args[args.Length - 1] is bool)
        {
            if ((bool) args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    GameObject obj = (args[0] as GameObject);
                    RiggingHelper.UnRig(obj, obj.transform.parent.gameObject);
                    SetValue("me:intent:action", "pickUp", string.Empty);
                    SetValue("me:intent:targetName", obj.name, string.Format("Grasping {0}",obj.name));
                }
            }                    
        }
    }

    public void UNGRASP(object[] args)
    {
        if (args[args.Length - 1] is bool)
        {
            if ((bool) args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    GameObject obj = (args[0] as GameObject);
                    SetValue("me:intent:action", "setDown", string.Empty);
                    SetValue("me:intent:target", obj.transform.position, 
                        string.Format("Ungrasping {0} at {1}", obj.name, GlobalHelper.VectorToParsable(obj.transform.position)));
                }
            }                    
        }
    }

    public bool IsSatisfied(string test) {
        bool satisfied = false;

        Hashtable predArgs = GlobalHelper.ParsePredicate(test);
        string predString = "";
        string[] argsStrings = null;

        foreach (DictionaryEntry entry in predArgs) {
            predString = (string) entry.Key;
            argsStrings = ((string) entry.Value).Split(',');
        }

        if (predString == "grasp") {
            GameObject theme = GameObject.Find(argsStrings[0] as string);

            if (theme != null) {
                if (DataStore.GetStringValue("me:holding") == theme.name) {
                    satisfied = true;
                }
            }
        }
        else if (predString == "ungrasp") {
            GameObject theme = GameObject.Find(argsStrings[0] as string);

            if (theme != null) {
                if (DataStore.GetStringValue("me:holding") != theme.name) {
                    satisfied = true;
                }
            }
        }

        return satisfied;
    }
}
