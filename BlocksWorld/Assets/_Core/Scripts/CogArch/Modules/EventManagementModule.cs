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

    /// <summary>
    /// Reference to the manipulable objects in the scene.
    /// Only these will be searched when an object is referred by name.
    /// </summary>
    public Transform grabbableBlocks;

    // need to keep the same as GrabPlaceModule's value but don't want to couple them
    private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);

    List<Vector3> objectMovePath = null;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        DataStore.Subscribe("user:intent:event", PromptEvent);
        DataStore.Subscribe("user:intent:object", TryEventComposition);
        DataStore.Subscribe("user:intent:action", TryEventComposition);
        DataStore.Subscribe("user:intent:location", TryEventComposition);
        DataStore.Subscribe("user:intent:partialEvent", TryEventComposition);

        AStarSearch.ComputedPath += GotPath;

        eventManager.EntityReferenced += EntityReferenced;
        eventManager.NonexistentEntityError += NonexistentReferent;
        eventManager.QueueEmpty += EventDoneExecuting;

        eventManager.OnUnhandledArgument += TryAnaphorHandling;
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

            try
            {
                eventManager.InsertEvent("", 0);
                eventManager.InsertEvent(eventStr, 1);
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException)
                {
                    Debug.LogWarning(string.Format("VoxSim event manager couldn't handle \"{0}\"", eventStr));
                }
            }
        }
    }

    void TryEventComposition(string key, DataStore.IValue value)
    {
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            Debug.Log("Trying event composition");
            string eventStr = DataStore.GetStringValue("user:intent:partialEvent");
            string actionStr = DataStore.GetStringValue("user:intent:action");
            string objectStr = DataStore.GetStringValue("user:intent:object");
            Vector3 locationPos = DataStore.GetVector3Value("user:intent:location");

	        if (!string.IsNullOrEmpty(actionStr))
            {
		        if (!string.IsNullOrEmpty(objectStr))
                {
                    if (actionStr.Contains("{0}"))
                    {
                        eventStr = actionStr.Replace("{0}", objectStr);
                        SetValue("user:intent:partialEvent", eventStr, string.Empty);
                    }
                }

                if (locationPos != default)
                {
                    if (actionStr.Contains("{1}"))
                    {
                        eventStr = actionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                        SetValue("user:intent:partialEvent", eventStr, string.Empty);
                    }
                }
            }
            else
            {
	            if ((!string.IsNullOrEmpty(objectStr)) && (locationPos != default))
                {
                    GameObject theme = GameObject.Find(objectStr);
		            Vector3 targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(theme).extents.y, locationPos.z);
		            if (!GlobalHelper.ContainingObjects(targetLoc).Contains(theme))
                    {
                        eventStr = "put({0},{1})".Replace("{0}", objectStr).Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                        SetValue("user:intent:partialEvent", eventStr, string.Empty);
                    }
                }
                else
                {

                }
            }

	        if (!string.IsNullOrEmpty(eventStr))
	        {
	            if (eventStr.Contains("{2}"))
	            {
	                
	            }
	        }

            // if no variables left in the composed event string
	        if (!string.IsNullOrEmpty(eventStr))
	        {
		        if (!Regex.IsMatch(eventStr, @"\{[0-1]+\}"))
		        {
			        SetValue("user:intent:event", eventStr, string.Empty);
		        }
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

    public string TryAnaphorHandling(string predStr) {
	    Debug.Log(string.Format("VoxSim event manager hit an UnhandledArgument error with {0}!", predStr));

        string anaphorList = string.Empty;

        // it might contain an anaphor
        if (predStr.Contains("{2}")) {
            anaphorList = string.Join(",", grabbableBlocks.GetComponentsInChildren<Voxeme>().Where(v => v.isActiveAndEnabled).Select(
                o => GlobalHelper.GetMostImmediateParentVoxeme(o.gameObject).name));
        }

        return anaphorList;
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

    public void EventDoneExecuting(object sender, EventArgs e) {
	    SetValue("user:intent:event",DataStore.StringValue.Empty,string.Empty);
	    SetValue("user:intent:partialEvent",DataStore.StringValue.Empty,string.Empty);

        if (string.IsNullOrEmpty(DataStore.GetStringValue("me:holding"))) {
	        SetValue("user:intent:object",DataStore.StringValue.Empty,string.Empty);
        }

	    SetValue("user:intent:action",DataStore.StringValue.Empty,string.Empty);
	    SetValue("user:intent:location",DataStore.Vector3Value.Zero,string.Empty);
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

    // IN: Objects
    // OUT: String
    public String THAT(object[] args) {
        List<String> objNames = new List<String>();
        //System.Random random = new System.Random ();

        if (args[0] is GameObject) {
            // assume all inputs are of same type
            //int index = random.Next(args.Length);
            for (int index = 0; index < args.Length; index++) {
                if (args[index] is GameObject) {
                    objNames.Add((args[index] as GameObject).name);
                }
            }
        }

        return string.Join(",", objNames.ToArray());
    }

    // IN: Objects
    // OUT: String
    public String THIS(object[] args) {
        Debug.Log("Diana's World: THIS");
        List<String> objNames = new List<String>();
        //System.Random random = new System.Random ();

        if (args[0] is GameObject) {
            // assume all inputs are of same type
            //int index = random.Next(args.Length);
            for (int index = 0; index < args.Length; index++) {
                if (args[index] is GameObject) {
                    objNames.Add((args[index] as GameObject).name);
                }
            }
        }

        return string.Join(",", objNames.ToArray());
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
