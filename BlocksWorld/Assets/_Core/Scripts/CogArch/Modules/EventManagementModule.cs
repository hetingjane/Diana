/*
This script interfaces with the VoxSim event manager

Reads:      user:intent:event (StringValue, full logical string representation of event)
            user:intent:object (StringValue)
            user:intent:action (StringValue)
            user:intent:location (Vector3Value)
            user:intent:partialEvent (StringValue, currently composed event from available info)
            me:intent:action:isComplete (BoolValue)
Writes:     me:intent:action (StringValue)
            me:intent:targetName (StringValue, name of object that is theme of action)
            me:intent:target (Vector3Value)
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
        DataStore.Subscribe("me:intent:action:isComplete", EventDoneExecuting);

        AStarSearch.ComputedPath += GotPath;

        eventManager.EntityReferenced += EntityReferenced;
        eventManager.NonexistentEntityError += NonexistentReferent;
        //eventManager.QueueEmpty += EventDoneExecuting;

        SatisfactionTest.OnUnhandledArgument += TryPropWordHandling;
        eventManager.OnUnhandledArgument += TryPropWordHandling;
    }


    // Update is called once per frame
    void Update()
	{
		string graspStatus = DataStore.GetStringValue("me:intent:action");
        string rightArmMotion = DataStore.GetStringValue("me:actual:motion:rightArm");

        if (objectMovePath != null)
        {
        	if (graspStatus == "move")
        	{
	            if (rightArmMotion == "reached")
	            {
		            if (objectMovePath.Count > 1)
	                {
		                objectMovePath.RemoveAt(0);
		                
		                Debug.Log(string.Format("Setting me:intent:target to {0}; me:actual:handPosR is {1}",
	                        GlobalHelper.VectorToParsable(objectMovePath.ElementAt(0) - holdOffset),
	                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:actual:handPosR"))));
	                    SetValue("me:intent:target", objectMovePath.ElementAt(0) - holdOffset, string.Empty);
	                }
	                else
	                {
	                    objectMovePath = null;
	                }
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
            Debug.Log(string.Format("Trying event composition with new info: {0} ({1})",
                key, key == "user:intent:partialEvent" ? DataStore.GetStringValue("user:intent:partialEvent") :
                    key == "user:intent:action" ? DataStore.GetStringValue("user:intent:action") :
                    key == "user:intent:object" ? DataStore.GetStringValue("user:intent:object") :
                    key == "user:intent:location" ? GlobalHelper.VectorToParsable(DataStore.GetVector3Value("user:intent:location")) : "Null"));

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
            else if (!string.IsNullOrEmpty(eventStr))
            {
                if (!string.IsNullOrEmpty(objectStr))
                {
                    if (eventStr.Contains("{0}"))
                    {
                        eventStr = eventStr.Replace("{0}", objectStr);
                        SetValue("user:intent:partialEvent", eventStr, string.Empty);
                    }
                }

                if (locationPos != default)
                {
                    if (eventStr.Contains("{1}"))
                    {
                        eventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
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

            // if no variables left in the composed event string
            if (!string.IsNullOrEmpty(eventStr))
            {
                if (!Regex.IsMatch(eventStr, @"\{[0-1]+\}"))
                {
                	Debug.Log(string.Format("Composed object {0}, action {1}, and location {2} into event {3}",
                		objectStr, actionStr, GlobalHelper.VectorToParsable(locationPos), eventStr));
                    SetValue("user:intent:event", eventStr, string.Empty);
                }
                else
                {
                	if (key != "user:intent:partialEvent")
                	{
		                Debug.Log(string.Format("Partial event is now {0}", eventStr));

                        if (!Regex.IsMatch(eventStr, @"\{1\}\(.+\)"))
                        {
                            string dir = Regex.Match(eventStr, @"\{1\}\(.+\)").Value.Replace("{1}(", "").Replace(")", "");
                            InferTargetLocation(GlobalHelper.GetTopPredicate(eventStr), GameObject.Find(objectStr), dir);
                        }
	                }
                }
            }
        }
    }

    Vector3 InferTargetLocation(string pred, GameObject theme, string dir)
    {
        Vector3 loc = theme.transform.position;
        switch (pred)
        {
            case "slide":
                loc = CalcSlideTarget(theme, dir);
                break;

            case "servo":
                loc = CalcServoTarget(theme, dir);
                break;

            default:
                break;
        }

        return loc;
    }

    Vector3 CalcSlideTarget(GameObject theme, string dir)
    {
        Vector3 loc = theme.transform.position;

        switch (dir)
        {
            case "left":
                break;

            case "right":
                break;

            case "front":
                break;

            case "back":
                break;

            default:
                break;
        }

        return loc;
    }

    Vector3 CalcServoTarget(GameObject theme, string dir)
    {
        Vector3 loc = theme.transform.position;

        switch (dir)
        {
            case "left":
                break;

            case "right":
                break;

            case "front":
                break;

            case "back":
                break;

            default:
                break;
        }

        return loc;
    }

    public void EventDoneExecuting(string key, DataStore.IValue value) {
        if ((value as DataStore.BoolValue).val == true) {
            // if "me:intent:action:isComplete" is true
            SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:event"),
	            string.Format("Store user:intent:event ({0}) in user:intent:lastEvent in case Diana did something wrong",
	            	DataStore.GetStringValue("user:intent:event")));
            SetValue("user:intent:event",DataStore.StringValue.Empty,string.Empty);
            SetValue("user:intent:partialEvent",DataStore.StringValue.Empty,string.Empty);

            if (string.IsNullOrEmpty(DataStore.GetStringValue("me:holding"))) {
                SetValue("user:intent:object",DataStore.StringValue.Empty,string.Empty);
            }

            SetValue("user:intent:action",DataStore.StringValue.Empty,string.Empty);
            SetValue("user:intent:location",DataStore.Vector3Value.Zero,string.Empty);
        }
    }

    public void GotPath(object sender, EventArgs e)
    {
        objectMovePath = ((ComputedPathEventArgs)e).path;
        SetValue("me:intent:action", "move", string.Empty);
        SetValue("me:intent:target", objectMovePath.ElementAt(0) - holdOffset, string.Empty);
    }

    public void EntityReferenced(object sender, EventArgs e) {
        // if there's an event to go with this, proceed with the event
        //  otherwise, Diana should indicate the entity and prompt for more information

        if (((EventReferentArgs)e).Referent is string) {
            SetValue("user:intent:object", ((EventReferentArgs)e).Referent as string, string.Empty);
        }
    }

    public string TryPropWordHandling(string predStr) {
        Debug.Log(string.Format("VoxSim event manager hit an UnhandledArgument error with {0}!", predStr));

        string fillerList = string.Empty;

        // it might contain a prop-word
        if (predStr.Contains("{2}")) {
            fillerList = string.Join(",", grabbableBlocks.GetComponentsInChildren<Voxeme>().Where(v => v.isActiveAndEnabled).Select(
                o => GlobalHelper.GetMostImmediateParentVoxeme(o.gameObject).name));
        }

        return fillerList;
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
                    SetValue("me:intent:action", "grasp", string.Empty);
                    SetValue("me:intent:targetName", obj.name, string.Format("Grasping {0}",obj.name));
                    SetValue("me:intent:target", obj.transform.position - holdOffset, string.Empty);
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
                    SetValue("me:intent:action", "ungrasp", string.Empty);
                    SetValue("me:intent:target", obj.transform.position, 
                        string.Format("Ungrasping {0} at {1}", obj.name, GlobalHelper.VectorToParsable(obj.transform.position)));
                }
            }                    
        }
    }

    // IN: Objects
    // OUT: String
    public String THAT(object[] args) {
	    String objName = string.Empty;
	    //System.Random random = new System.Random ();

	    if (args[0] is GameObject) {
		    // assume all inputs are of same type
		    GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
			    .1f, args.Select(a => a as GameObject).ToList(), LayerMask.GetMask("Blocks"));
		    if (target != null) {
			    objName = target.name;
		    }
	    }
	    else if (args[0] is String) {
		    // assume all inputs are of same type
		    GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
			    .1f, args.Select(a => GameObject.Find(a as String)).ToList(), LayerMask.GetMask("Blocks"));
		    if (target != null) {
			    objName = target.name;
		    }
	    }

	    return objName;
    }

    // IN: Objects
    // OUT: String
    public String THIS(object[] args) {
	    String objName = string.Empty;
        //System.Random random = new System.Random ();

        if (args[0] is GameObject) {
	        // assume all inputs are of same type
	        GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
	        	.1f, args.Select(a => a as GameObject).ToList(), LayerMask.GetMask("Blocks"));
	        if (target != null) {
	        	objName = target.name;
	        }
        }
        else if (args[0] is String) {
        	// assume all inputs are of same type
        	GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
	        	.1f, args.Select(a => GameObject.Find(a as String)).ToList(), LayerMask.GetMask("Blocks"));
	        if (target != null) {
	        	objName = target.name;
	        }
        }

        return objName;
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
