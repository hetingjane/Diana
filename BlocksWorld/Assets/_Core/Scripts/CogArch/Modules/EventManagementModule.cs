/*
This script interfaces with the VoxSim event manager

Reads:      user:intent:event (StringValue, full logical string representation of event)
            user:intent:object (StringValue)
            user:intent:action (StringValue)
            user:intent:location (Vector3Value)
Writes:     me:intent:action (StringValue)
            me:intent:targetName (StringValue, name of object that is theme of action)
            me:intent:handPosR
*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Pathfinding;
using VoxSimPlatform.Vox;

public class EventManagementModule : ModuleBase
{
    public EventManager eventManager;

    // need to keep the same as GrabPlaceModule's value but don't want to couple them
    private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);

    List<Vector3> objectMovePath = null;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        DataStore.Subscribe("user:intent:event", PromptEvent);

        AStarSearch.ComputedPath += GotPath;

        eventManager.EntityReferenced += EntityReferenced;
        eventManager.NonexistentEntityError += NonexistentReferent;
    }

    // Update is called once per frame
    void Update()
    {
        string rightArmMotion = DataStore.GetStringValue("me:rightArm:motion");

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

    void PromptEvent(string key, DataStore.IValue value)
    {
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            string eventStr = value.ToString().Trim();
            if (string.IsNullOrEmpty(eventStr)) return;

            string pred = GlobalHelper.GetTopPredicate(eventStr);
            if (eventManager.voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) &&
                eventManager.voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs") {

                SetValue("user:intent:action", pred, string.Empty);

                eventManager.InsertEvent("", 0);
                eventManager.InsertEvent(eventStr, 1);
            }
        }
    }

    public void GotPath(object sender, EventArgs e)
    {
        objectMovePath = ((ComputedPathEventArgs)e).path;
        SetValue("me:intent:handPosR", objectMovePath.ElementAt(0) - holdOffset, string.Empty);
    }

    public void EntityReferenced(object sender, EventArgs e) {
        // if there's an event to go with this, proceed with the event
        //  otherwise, Diana should indicate the entity and prompt for more information

        if (((EventReferentArgs)e).Referent is string) {
            SetValue("user:intent:object", ((EventReferentArgs)e).Referent as string, string.Empty);
        }
    }
        
    public void NonexistentReferent(object sender, EventArgs e) {
        Debug.Log(((EventReferentArgs) e).Referent is Pair<string, List<object>>);
        if (((EventReferentArgs) e).Referent is Pair<string, List<object>>) {
            // pair of predicate and object list 
            // (present type - common type of object list, of absent attribute - predicate)
            string pred = ((Pair<string, List<object>>) ((EventReferentArgs) e).Referent).Item1;
            List<object> objs = ((Pair<string, List<object>>) ((EventReferentArgs) e).Referent).Item2;
            Debug.Log(objs.Count);
            if (objs.Count > 0) {
                if (!objs.Any(o => (o == null) || (o.GetType() != typeof(GameObject)))) {
                    // if all objects are game objects
                    Debug.Log(string.Format("{0} {1} does not exist!", pred,
                        (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred));
                    string responseStr = string.Format("There is no {0} {1} here.", pred,
	                    (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred);
                    SetValue("me:speech:intent", responseStr, string.Empty);
                }
            }
        }
        else if (((EventReferentArgs) e).Referent is string) {
            // absent object type - string
            if (Regex.IsMatch(((EventReferentArgs) e).Referent as string, @"\{.\}")) {
                return;
            }

            string responseStr = string.Format("There is no {0} here.", ((EventReferentArgs)e).Referent as string);
            SetValue("me:speech:intent", responseStr, string.Empty);
        }
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
