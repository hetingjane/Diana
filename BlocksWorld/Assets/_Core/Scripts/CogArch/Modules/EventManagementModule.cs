/*
This script interfaces with the VoxSim event manager

Reads:      user:intent:event (StringValue, full logical string representation of event)
            user:intent:object (StringValue)
            user:intent:action (StringValue)
            user:intent:location (Vector3Value)
            user:intent:partialEvent (StringValue, currently composed event from available info)
            user:intent:append:event (StringValue, same as user:intent:event,
                but intended to be appended to the end of the event queue)
            user:intent:append:action (StringValue, same as user:intent:action,
                but intended for the composed event to be appended to the end of the event queue)
            user:intent:append:partialEvent (StringValue, same as user:intent:partialEvent,
                but intended for the composed event to be appended to the end of the event queue)          
            me:intent:action:isComplete (BoolValue)
            me:checkServo (BoolValue)
Writes:     me:intent:action (StringValue)
            me:intent:targetName (StringValue, name of object that is theme of action)
            me:intent:target (Vector3Value)
            me:checkServo (BoolValue)
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
using VoxSimPlatform.SpatialReasoning.QSR;
using VoxSimPlatform.Vox;

public class EventManagementModule : ModuleBase
{
    public EventManager eventManager;

    public VoxMLLibrary voxmlLibrary;

    /// <summary>
    /// Reference to the manipulable objects in the scene.
    /// Only these will be searched when an object is referred by name.
    /// </summary>
    public Transform grabbableBlocks;

    /// <summary>
    /// Reference to the default surface in the scene.
    /// (i.e., the table)
    /// </summary>
	public GameObject demoSurface;
    
	/// <summary>
	/// Reference to Diana's hand.
	/// </summary>
	//public Transform hand;

    public float servoSpeed = 0.05f;

    private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);

    List<Vector3> objectMovePath = null;

    // corresponding predicate to each intent direction name
    Dictionary<string, string> directionPreds = new Dictionary<string, string>()
    {
        { "left", "left" },
        { "right", "right" },
        { "front", "in_front" },
        { "back", "behind" }
    };

    // how each intent direction name is referred to in speech
    Dictionary<string, string> directionLabels = new Dictionary<string, string>()
    {
        { "left", "left of" },
        { "right", "right of" },
        { "front", "in front of" },
        { "back", "behind" }
    };

    // corresponding world-space vector to each intent direction name
    Dictionary<string, Vector3> directionVectors = new Dictionary<string, Vector3>()
    {
        { "left", Vector3.left },
        { "right", Vector3.right },
        { "front", Vector3.forward },
        { "back", Vector3.back },
        { "up", Vector3.up },
        { "down", Vector3.down }
    };

    // the opposite of each direction
    Dictionary<string, string> oppositeDir = new Dictionary<string, string>()
    {
        { "left", "right" },
        { "right", "left" },
        { "front", "back" },
        { "back", "front" },
        { "up", "down" },
        { "down", "up" }
    };

    Dictionary<string, string> relativeDir = new Dictionary<string, string>()
    {
        { "left", "left" },
        { "right", "right" },
        { "front", "back" },
        { "back", "front" },
        { "up", "up" },
        { "down", "down" }
    };

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        DataStore.Subscribe("user:intent:event", PromptEvent);
        DataStore.Subscribe("user:intent:object", TryEventComposition);
        DataStore.Subscribe("user:intent:action", TryEventComposition);
        DataStore.Subscribe("user:intent:location", TryEventComposition);
        DataStore.Subscribe("user:intent:partialEvent", TryEventComposition);
        DataStore.Subscribe("user:intent:append:event", AppendEvent);
        DataStore.Subscribe("user:intent:append:action", TryEventComposition);
        DataStore.Subscribe("user:intent:append:partialEvent", TryEventComposition);
        DataStore.Subscribe("me:intent:action:isComplete", EventDoneExecuting);
        DataStore.Subscribe("me:isCheckingServo", CheckServoStatus);

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
            
            if (!DataStore.GetBoolValue("me:isUndoing"))
            {
                if (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(eventStr),voxmlLibrary) == "programs")
                {
                    Debug.Log(string.Format("Setting last event {0}", DataStore.GetStringValue("user:intent:event")));
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:event"),
                        string.Format("Store user:intent:event ({0}) in user:intent:lastEvent in case Diana did something wrong",
                            DataStore.GetStringValue("user:intent:event")));
                }
            }
                
            try
            {
                if (DataStore.GetBoolValue("me:isUndoing"))
                {
                    Debug.Log("Clearing events");
                    eventManager.ClearEvents();
                }

                SetValue("me:speech:intent", "OK.", string.Empty);
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

    void AppendEvent(string key, DataStore.IValue value)
    {
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            string eventStr = value.ToString().Trim();
            if (string.IsNullOrEmpty(eventStr)) return;

            try
            {
                eventManager.InsertEvent("", eventManager.events.Count);
                eventManager.InsertEvent(eventStr, eventManager.events.Count);
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
                    key == "user:intent:location" ? GlobalHelper.VectorToParsable(DataStore.GetVector3Value("user:intent:location")) : 
                    key == "user:intent:append:partialEvent" ? DataStore.GetStringValue("user:intent:append:partialEvent") : 
                    key == "user:intent:append:action" ? DataStore.GetStringValue("user:intent:append:action") : "Null"));

            string eventStr = DataStore.GetStringValue("user:intent:partialEvent");
            string actionStr = DataStore.GetStringValue("user:intent:action");

            string appendEventStr = DataStore.GetStringValue("user:intent:append:partialEvent");
            string appendActionStr = DataStore.GetStringValue("user:intent:append:action");

            string objectStr = DataStore.GetStringValue("user:intent:object");
            Vector3 locationPos = DataStore.GetVector3Value("user:intent:location");

            string lastTheme = DataStore.GetStringValue("me:lastTheme");

            if (key == "user:intent:object")
            {
                if (!string.IsNullOrEmpty(objectStr))
                {
                    if ((!string.IsNullOrEmpty(eventStr)) && (eventStr.Contains("{0}")) &&
                        (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(eventStr),voxmlLibrary) == "programs"))
                    {
                        eventStr = eventStr.Replace("{0}", objectStr);
                        SetValue("user:intent:partialEvent", eventStr, string.Empty);
                    }
                    else if ((!string.IsNullOrEmpty(eventStr)) && (eventStr.Contains("{0}")) &&
                        (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(eventStr),voxmlLibrary) == "programs"))
                    {
                        appendEventStr = appendEventStr.Replace("{0}", objectStr);
                        SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                    }
                    else if (!string.IsNullOrEmpty(actionStr))
                    {
                        if (actionStr.Contains("{0}"))
                        {
                            eventStr = actionStr.Replace("{0}", objectStr);
                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                        }
                    }
                    else if (!string.IsNullOrEmpty(appendActionStr))
                    {
                        if (appendActionStr.Contains("{0}"))
                        {
                            appendEventStr = appendActionStr.Replace("{0}", objectStr);
                            SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                        }
                    }
                    else if (locationPos != default)
                    {
                        GameObject theme = GameObject.Find(objectStr);
                        Vector3 targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(theme).extents.y, locationPos.z);
                        if (!GlobalHelper.ContainingObjects(targetLoc).Contains(theme))
                        {
                            eventStr = "put({0},{1})".Replace("{0}", objectStr).Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                        }
                    }
                }
            } 
            else if (key == "user:intent:action")
            {
	            if (actionStr.StartsWith("slide")) {
		            if (!string.IsNullOrEmpty(eventStr)) {
	                    if (GlobalHelper.GetTopPredicate(eventStr) == "servo") {
	                        eventManager.ClearEvents();
	                    }
		            }
                }

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
                    else if (!string.IsNullOrEmpty(lastTheme))
                    {
                        if (actionStr.Contains("{0}"))
                        {
                            eventStr = actionStr.Replace("{0}", lastTheme);
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
            }
            else if (key == "user:intent:location")
            {
                if (locationPos != default)
                {
                    if (!string.IsNullOrEmpty(eventStr))
                    {
                        if (eventStr.Contains("{1}"))
                        {
                            eventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                        }
                    }
                    else if (!string.IsNullOrEmpty(appendEventStr))
                    {
                        if (appendEventStr.Contains("{1}"))
                        {
                            appendEventStr = appendEventStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                            SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                        }
                    }
                    else if (!string.IsNullOrEmpty(actionStr))
                    {
                        if (actionStr.Contains("{1}"))
                        {
                            eventStr = actionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                        }
                    }
                    else if (!string.IsNullOrEmpty(appendActionStr))
                    {
                        if (appendActionStr.Contains("{1}"))
                        {
                            appendEventStr = appendActionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                            SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                        }
                    }
                    else if (!string.IsNullOrEmpty(objectStr))
                    {
                        GameObject theme = GameObject.Find(objectStr);
                        Vector3 targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(theme).extents.y, locationPos.z);
                        if (!GlobalHelper.ContainingObjects(targetLoc).Contains(theme))
                        {
                            eventStr = "put({0},{1})".Replace("{0}", objectStr).Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                        }
                    }
                }
            } 
            else if (key == "user:intent:partialEvent")
            {
                if (!string.IsNullOrEmpty(eventStr))
                {
                    // if no variables left in the composed event string and no empty parens
                    if (!Regex.IsMatch(eventStr, @"\{[0-1]+\}") && !Regex.IsMatch(eventStr, @"\(\)"))
                    {
                        // if parens match and > 0
                        if (eventStr.Count(f => f == '(') == eventStr.Count(f => f == ')') &&
                            (eventStr.Count(f => f == '(') + eventStr.Count(f => f == ')') > 0))
                        {
                            Debug.Log(string.Format("Composed object {0}, action {1}, and location {2} into event {3}",
    	                        objectStr, actionStr, GlobalHelper.VectorToParsable(locationPos), eventStr));
                                
    	                    if (!string.IsNullOrEmpty(objectStr)) {
    	                        SetValue("me:lastTheme",objectStr,string.Empty);
    	                        SetValue("me:lastThemePos",GameObject.Find(objectStr).transform.position,string.Empty);
    	                    }
    	                    
    	                	SetValue("user:intent:event", eventStr, string.Empty);
                        }
                    }
                    else
                    {
                        Debug.Log(string.Format("Partial event is now {0}", eventStr));

                        if (Regex.IsMatch(eventStr, @"\{1\}\(.+\)(?=\))"))
                        {
                            string match = Regex.Match(eventStr, @"\{1\}\(.+\)(?=\))").Value;
                            string dir = match.Replace("{1}(", "").Replace(")", "");
                            Vector3 targetPos = InferTargetLocation(GlobalHelper.GetTopPredicate(eventStr), GameObject.Find(objectStr), dir);
                            Debug.Log(GlobalHelper.VectorToParsable(targetPos));
                            Debug.Log(match);

                            SetValue("user:intent:partialEvent", 
                                eventStr.Replace(match, GlobalHelper.VectorToParsable(targetPos)), string.Empty);
                        }
                        else if (eventStr.Contains("{0}"))
                        {
                        	if (!string.IsNullOrEmpty(objectStr))
                        	{
	                        	eventStr = eventStr.Replace("{0}", objectStr);
	                        	SetValue("user:intent:partialEvent", eventStr, string.Empty);
	                        }
                            else if (!string.IsNullOrEmpty(lastTheme))
                            {
                                eventStr = actionStr.Replace("{0}", lastTheme);
                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                            }
                        }
                        else if (eventStr.Contains("{1}"))
                        {
                            if (locationPos != default)
                            {
                                Vector3 targetLoc = locationPos;

                                if (!string.IsNullOrEmpty(objectStr))
                                {
                                    targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(GameObject.Find(objectStr)).extents.y, locationPos.z);
                                }

                                eventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                            }
                            else if (!string.IsNullOrEmpty(objectStr))
                            {
                                eventStr = eventStr.Replace("{1}", string.Format("on({0})",objectStr));
                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                            }
                        }
                    }
                }
            }
            else if (key == "user:intent:append:action")
            { 
                if (!string.IsNullOrEmpty(appendActionStr))
                {
                    if (!string.IsNullOrEmpty(objectStr))
                    {
                        if (appendActionStr.Contains("{0}"))
                        {
                            appendEventStr = appendActionStr.Replace("{0}", objectStr);
                        }
                    }

                    if (locationPos != default)
                    {
                        if (appendActionStr.Contains("{1}"))
                        {
                            appendEventStr = appendActionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                        }
                    }

	                SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                }
            }
            else if (key == "user:intent:append:partialEvent")
            {
                // if no variables left in the composed event string
                if (!string.IsNullOrEmpty(appendEventStr))
                {
                    if (!Regex.IsMatch(appendEventStr, @"\{[0-1]+\}"))
                    {
                        Debug.Log(string.Format("Composed object {0}, action {1}, and location {2} into event {3}",
                            objectStr, appendActionStr, GlobalHelper.VectorToParsable(locationPos), appendEventStr));
                        SetValue("user:intent:append:event", appendEventStr, string.Empty);
                    }
                    else
                    {
                        Debug.Log(string.Format("Partial event is now {0}", appendEventStr));

                        if (Regex.IsMatch(appendEventStr, @"\{1\}\(.+\)(?=\))"))
                        {
                            string match = Regex.Match(appendEventStr, @"\{1\}\(.+\)(?=\))").Value;
                            string dir = match.Replace("{1}(", "").Replace(")", "");
                            Vector3 targetPos = InferTargetLocation(GlobalHelper.GetTopPredicate(appendEventStr), GameObject.Find(objectStr), dir);
                            Debug.Log(GlobalHelper.VectorToParsable(targetPos));
                            Debug.Log(match);

                            SetValue("user:intent:append:partialEvent", 
                                appendEventStr.Replace(match, GlobalHelper.VectorToParsable(targetPos)), string.Empty);
                        }
                        else if (appendEventStr.Contains("{0}"))
                        {
                            if (!string.IsNullOrEmpty(objectStr))
                            {
                                appendEventStr = appendEventStr.Replace("{0}", objectStr);
                                SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                            }
                            else if (!string.IsNullOrEmpty(lastTheme))
                            {
                                eventStr = actionStr.Replace("{0}", lastTheme);
                                SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                            }
                        }
                        else if (appendEventStr.Contains("{1}"))
                        {
                            if (locationPos != default)
                            {
                                Vector3 targetLoc = locationPos;

                                if (!string.IsNullOrEmpty(objectStr))
                                {
                                    targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(GameObject.Find(objectStr)).extents.y, locationPos.z);
                                }

                                appendEventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                                SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                            }
                            else if (!string.IsNullOrEmpty(objectStr))
                            {
                                eventStr = eventStr.Replace("{1}", string.Format("on({0})",objectStr));
                                SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                            }
                        }
                    }
                }
            }
        }
    }

    void CheckServoStatus(string key, DataStore.IValue value)
    {
        bool val = (value as DataStore.BoolValue).val;

        // if me:checkServo is true, and me:actual:motion:rightArm is "reached"
        if ((val) && (DataStore.GetStringValue("me:actual:motion:rightArm") == "reached"))
        {
            if (eventManager.events.Count == 0) {
                SetValue("user:intent:append:partialEvent", string.Empty, string.Empty);

                if (DataStore.GetBoolValue("user:intent:isServoLeft")) {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(left))", string.Empty);
                }
                else if (DataStore.GetBoolValue("user:intent:isServoRight")) {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(right))", string.Empty);
                }
                else if (DataStore.GetBoolValue("user:intent:isServoFront")) {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(front))", string.Empty);
                }
                else if (DataStore.GetBoolValue("user:intent:isServoBack")) {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(back))", string.Empty);
                }
            }
        }

        SetValue("me:isCheckingServo", false, string.Empty);
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

        List<GameObject> options = new List<GameObject>();
        GameObject choice = null;

        foreach (Transform child in grabbableBlocks)
        {
            options.Add(child.gameObject);
        }

        switch (dir)
        {
            case "left":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.5f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();
                choice = options.OrderByDescending(o =>
                    Vector3.Dot((o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme).extents,
                        directionVectors[dir]);
                }
                else
                {
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    loc = new Vector3(
                        RandomHelper.RandomFloat(
                            (theme.transform.position + Vector3.Scale(themeBounds.extents, directionVectors[oppositeDir[dir]])).x,
                            surfaceBounds.max.x, 0),
                        theme.transform.position.y,
                        theme.transform.position.z);
                }
                break;

            case "right":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.5f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();
                choice = options.OrderByDescending(o =>
                    Vector3.Dot((o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme).extents,
                        directionVectors[dir]);
                }
                else
                {
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    loc = new Vector3(
                        RandomHelper.RandomFloat(surfaceBounds.min.x,
                            (theme.transform.position + Vector3.Scale(themeBounds.extents, directionVectors[oppositeDir[dir]])).x, 0),
                        theme.transform.position.y,
                        theme.transform.position.z);
                }
                break;

            case "front":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.5f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();
                Debug.Log("front: " + options.Count + " options");
                choice = options.OrderByDescending(o =>
                    Vector3.Dot((o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme).extents,
                        directionVectors[dir]);
                }
                else
                {
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    loc = new Vector3(theme.transform.position.x,
                        theme.transform.position.y,
                        RandomHelper.RandomFloat(surfaceBounds.min.z,
                            (theme.transform.position + Vector3.Scale(themeBounds.extents, directionVectors[oppositeDir[dir]])).z, 0));
                }
                break;

            case "back":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.5f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();
                choice = options.OrderByDescending(o =>
                    Vector3.Dot((o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme).extents,
                        directionVectors[dir]);
                }
                else
                {
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    loc = new Vector3(theme.transform.position.x,
                        theme.transform.position.y,
                        RandomHelper.RandomFloat(
                            (theme.transform.position + Vector3.Scale(themeBounds.extents, directionVectors[oppositeDir[dir]])).z,
                            surfaceBounds.max.z, 0));
                }
                break;

            default:
                break;
        }

        return loc;
    }

    Vector3 CalcServoTarget(GameObject theme, string dir)
    {
        Vector3 loc = theme.transform.position + (directionVectors[oppositeDir[dir]] * servoSpeed);

        Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);

        Bounds projectedBounds = new Bounds(loc,themeBounds.size);
        foreach (Transform test in grabbableBlocks) {
            if (test.gameObject != theme.gameObject) {
                if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)) &&
                    !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject))) {
                    if ((dir == "left") || (dir == "right")) {
                        loc = new Vector3(
                            (test.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(test.gameObject).extents,
                            directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme.gameObject).extents,
                            directionVectors[dir])).x,loc.y,loc.z);
                    }
                    else if ((dir == "front") || (dir == "back")) {
                        loc = new Vector3(loc.x,loc.y,
                            (test.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(test.gameObject).extents,
                            directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme.gameObject).extents,
                            directionVectors[dir])).z);
                    }
                }
            }
        }

        return loc;
    }

    void EventDoneExecuting(string key, DataStore.IValue value) {
        if ((value as DataStore.BoolValue).val == true) {
            // if "me:intent:action:isComplete" is true
            if (DataStore.GetStringValue("me:intent:action") == "unreach") {
                SetValue("user:intent:event",DataStore.StringValue.Empty,string.Empty);
                SetValue("user:intent:partialEvent",DataStore.StringValue.Empty,string.Empty);

                if (string.IsNullOrEmpty(DataStore.GetStringValue("me:holding"))) {
                    SetValue("user:intent:object",DataStore.StringValue.Empty,string.Empty);
                }

                SetValue("user:intent:action",DataStore.StringValue.Empty,string.Empty);
                SetValue("user:intent:location",DataStore.Vector3Value.Zero,string.Empty);

                SetValue("user:intent:append:partialEvent",DataStore.StringValue.Empty,string.Empty);
                SetValue("user:intent:append:action",DataStore.StringValue.Empty,string.Empty);

	            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:lastEvent"))) {
	                if (DataStore.GetStringValue("user:intent:lastEvent").StartsWith("servo")) {
	                    SetValue("me:intent:target","user",string.Empty);
	                }
	            }

                SetValue("me:isUndoing",false,string.Empty);
            }
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
            if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object"))) {
                SetValue("user:intent:object", ((EventReferentArgs)e).Referent as string, string.Empty);
            }

            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:event"))) {
                // currently executing an event
                //if (DataStore.GetStringValue("me:lastTheme") != DataStore.GetStringValue("user:intent:object")) {
                    // "pick up the yellow block"
                    // nevermind
                    // "put the yellow block on the green block"
                    // nevermind
                    // FU Diana
                    string objectStr = DataStore.GetStringValue("user:intent:object");
                    SetValue("me:lastTheme",objectStr,string.Empty);
                    SetValue("me:lastThemePos",GameObject.Find(objectStr).transform.position,string.Empty);
                //}
            }
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
                    SetValue("me:emotion", "confusion", string.Empty);
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
            SetValue("me:emotion", "confusion", string.Empty);
        }

        if (string.IsNullOrEmpty(DataStore.GetStringValue("me:holding"))) {
            SetValue("user:intent:object",DataStore.StringValue.Empty,string.Empty);
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
                    
	                //if (obj.transform.parent != hand)
	                //{
		                RiggingHelper.UnRig(obj, obj.transform.parent.gameObject);	                	
	                //}

                    SetValue("me:intent:targetName", obj.name, string.Format("Grasping {0}",obj.name));
                    SetValue("me:intent:target", obj.transform.position - holdOffset, string.Empty);
                    SetValue("me:intent:action", "hold", string.Empty);
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
                    SetValue("me:intent:action", "release", string.Empty);
                    SetValue("me:intent:action", "unreach", string.Empty);
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

        // still didn't find one
        if (objName == string.Empty) {
            // it might be the same as the current object intent
            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object"))) {
                objName = DataStore.GetStringValue("user:intent:object");
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

        // still didn't find one
        if (objName == string.Empty) {
            // it might be the same as the current object intent
            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object"))) {
                objName = DataStore.GetStringValue("user:intent:object");
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
