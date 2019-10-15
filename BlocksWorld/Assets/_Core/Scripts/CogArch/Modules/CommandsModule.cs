/*
This module watches for (parsed and grokked) commands, and reacts
appropriately (by setting me:intent values on the blackboard).

Reads:		user:communication (Communication)
Writes:		me:intent:eyesClosed (BoolValue)
			me:speech:intent (StringValue)
			me:intent:pointAt (StringValue, name of thing to point at)
			me:intent:action (StringValue: "point", "pickUp", "setDown")
			me:intent:target (Vector3Value)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Semantics;
using CWCNLP;

using VoxSimPlatform.Vox;

public class CommandsModule : ModuleBase
{
	[Tooltip("Container for objects the agent can see and manipulate")]
	public Transform manipulableObjects;
	
	List<ComCommand> pendingCommand = new List<ComCommand>();
	string pendingGrabTarget;
	float pendingTime;	// if nonzero, time at which to do the next command
	
	protected override void Start() {
		Debug.Assert(manipulableObjects != null);	// we need this asigned
		
		base.Start();
		DataStore.Subscribe("user:communication", NoteUserCommunication);
		
		DataStore.Subscribe("me:holding", NoteHolding);
	}

	protected void Update() {
		if (pendingCommand.Count > 0 && pendingTime > 0 && Time.time > pendingTime) {
			Debug.Log("It's about time to continue with " + pendingCommand[0]);
			HandleCommand(pendingCommand[0]);
			pendingCommand.RemoveAt(0);
			if (pendingCommand.Count > 0) {
				Debug.Log("Still pending: " + pendingCommand[0]);
				pendingTime = Time.time + 2;
			}
		}
	}

	void NoteUserCommunication(string key, DataStore.IValue value) {
        //if (DataStore.GetBoolValue("user:isInteracting")) {
    		var comVal = value as CommunicationValue;
    		if (comVal == null) return;
    		
    		// if we're standing by, then ignore anything that's not a direct address.
    		if (DataStore.GetBoolValue("me:standingBy")) {
    			if (!comVal.val.directAddress) return;
    			DataStore.SetValue("me:standingBy", new DataStore.BoolValue(false), this, "noted direct address");
    		}
    				
    		ComCommand cmd = comVal.val as ComCommand;
			if (cmd != null) HandleCommand(cmd);
			ComEmote emote = comVal.val as ComEmote;
		if (emote != null) HandleEmote(emote);
        //}
	}

	void NoteHolding(string key, DataStore.IValue value) {
		//if (value.ToString() == pendingGrabTarget && pendingCommand.Count > 0 != null) {
		//	Debug.Log("OK, we got it!  Time to continue with " + pendingCommand[0]);
		//	HandleCommand(pendingCommand[pendingCommand.Count-1]);
		//	pendingCommand.RemoveAt(pendingCommand.Count-1);
		//}
	}

	// me:holding = same as me:intent:targetName (i.e. GreenBlock),
	// and me:actual:motion:rightArm = reached

	void HandleCommand(ComCommand cmd) {
		ActionSpec act = cmd.action;
		if (act == null) return;
		Transform obj;
		string comment = "handling " + act;
		switch (act.action) {
		case Action.Close:
			if (IsAgentsEyes(act.directObject)) {
				SetValue("me:intent:eyesClosed", true, comment);
			} else {
				SayICant(comment);
			}
			break;
		case Action.Open:
			if (IsAgentsEyes(act.directObject)) {
				SetValue("me:intent:eyesClosed", false, comment);
			} else {
				SayICant(comment);
			}
			break;
		case Action.Point:
			if (act.location != null && act.location.obj != null	) {
				// Point at the indicated object.
				obj = FindObjectFromSpec(act.location.obj);
				if (obj == null) {
					SetValue("me:speech:intent", "I can't find that.", comment);
				} else {
					SetValue("me:speech:intent", "OK.", comment);		
					SetValue("me:intent:action", "point", comment);
					SetValue("me:intent:pointAt", obj.name, comment);					
					SetValue("me:intent:target", obj.position, comment);
				}
			} else {
				// If no object, we assume the direction to point is always where the user is pointing.
				SetValue("me:speech:intent", "OK.", comment);		
				SetValue("me:intent:pointAt", "userPoint", comment);
			}
			break;
		case Action.Look:
			Debug.Log("<color=green>Look object: " + act.directObject + "</color>");
			if (act.directObject == null) {
				// For now, if not otherwise specified, we assume the direction 
				// to point is whereever the user is pointing.
				SetValue("me:speech:intent", "OK.", comment);		
				SetValue("me:intent:lookAt", "userPoint", comment);
			} else if (act.directObject.referredToAs == "me") {
				SetValue("me:intent:lookAt", "user", comment);
			} else {
				obj = FindObjectFromSpec(act.directObject);
				if (obj == null) {
					SetValue("me:speech:intent", "I don't understand what block you mean.", comment);
				} else {
					SetValue("me:intent:lookAt", obj.name, comment);
					SetValue("me:speech:intent", "OK.", comment);		
				}
			}
			break;
		case Action.Stop:
			SetValue("me:speech:intent", "OK.", comment);		
			SetValue("me:intent:lookAt", "", comment);
			SetValue("me:intent:pointAt", "", comment);
			SetValue("me:intent:action", "", comment);
			break;
		case Action.Continue:
			SetValue("me:speech:intent", "OK.", comment);
			GrabPlaceModule.paused = false;
			break;
        case Action.PickUp:
        case Action.Raise:
            // See if we can determine what object to pick up, based on spec.
            if (act.directObject == null) return;   // (user is probably not done speaking)
            obj = FindObjectFromSpec(act.directObject);
            if (obj == null)
            {
                SetValue("me:speech:intent", "I don't know what block you mean.", comment);
            }
            else
            {
                SetValue("me:speech:intent", "OK.", comment);
                SetValue("me:intent:action", "pickUp", comment);
                SetValue("me:intent:targetName", obj.name, comment);
                SetValue("me:intent:target", obj.position, comment);
            }
            break;
        case Action.SetDown:
        case Action.Put:
            // For now we'll assume we're being told to set down whatever we're holding.
            // But let's verify anyway.
            GrabPlaceModule.paused = false;
            obj = FindObjectFromSpec(act.directObject);
	        if (false && obj != null && obj.name != DataStore.GetStringValue("me:holding")) {
                SetValue("me:speech:intent", "I'm not holding that.", comment);
            }
            else
            {
                bool allGood = true;
                if (act.location == null)
                {
                    // No location specified.
                    SetValue("me:intent:targetName", "", comment);
                    SetValue("me:intent:target", "", comment);
                }
                else
                {
                    // Location specified.
                    if (act.location.relation == LocationSpec.Relation.Indicated)
                    {
                        // User is saying "here" or "there" and should be pointing.
                        if (!DataStore.GetBoolValue("user:isPointing"))
                        {
                            SetValue("me:speech:intent", "I don't know where you mean.", comment);
                            allGood = false;
                        }
                        else
                        {
                            SetValue("me:intent:targetName", "", comment);
                            SetValue("me:intent:target", DataStore.GetVector3Value("user:pointPos"), comment);
                        }
                    }
                    else
                    {
                        // User specified an object to set it on.
                        Transform relObj = FindObjectFromSpec(act.location.obj);
                        if (relObj == null && act.location.obj != null)
                        {
                            SetValue("me:speech:intent", "I don't know where you mean.", comment);
                            allGood = false;
                        }
                        if (relObj != null)
                        {
                            SetValue("me:intent:target", relObj.position, comment);
                            SetValue("me:intent:targetName", relObj.name, comment);
                        }
                    }
                }
	            if (obj == null && string.IsNullOrEmpty(DataStore.GetStringValue("me:holding"))) {
                	// We're told to put something somewhere, but haven't been told
                	// what.  Pick something.
                	var spec = new ObjSpec();
                	spec.referredToAs = "it";
                	obj = FindObjectFromSpec(spec);
                	Debug.Log("Got obj: " + obj);
	                SetValue("me:speech:intent", "OK.", comment);
	                SetValue("me:intent:action", "pickUp", comment);
	                SetValue("me:intent:targetName", obj.name, comment);
	                SetValue("me:intent:target", obj.position, comment);
	                pendingCommand.Add(cmd);
	                pendingGrabTarget = obj.name;
		            pendingTime = Time.time + 4;
		            Debug.Log("Added pending: " + cmd);
	                return;
                }
                if (allGood)
                {
                    SetValue("me:speech:intent", "OK.", comment);
                    SetValue("me:intent:action", "setDown", comment);
                }
            }
            break;
        case Action.StandBy:
			SetValue("me:standingBy", true, comment);
			break;
		case Action.Identify:
			if (act.directObject != null && act.directObject.ToString().ToLower() == "[name of you]") {
				string name = DataStore.GetStringValue("me:name");
				if (string.IsNullOrEmpty(name)) {
					SetValue("me:speech:intent", "I don't have a name.", "me:name not set");			
				} else {
					SetValue("me:speech:intent", "My name is " + name + ".", "was asked my name");
				}
				return;
			}
			obj = FindObjectFromSpec(act.directObject);
			if (obj == null) {
				SetValue("me:speech:intent", "I'm not sure what you are referring to.", comment);
			} else {
				// Convert the CamelCase object name into more natural speech.
				// (reference: https://stackoverflow.com/questions/155303)
				string name = obj.name;
				name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
				SetValue("me:speech:intent", "That is the " + name + ".", comment);
			}
			break;
		default:
			// Let's not say "I can't" to every unknown command.
			// At least not yet ... it gets pretty annoying.
			//SayICant(comment);
			break;
		}
	}
	
	void HandleEmote(ComEmote emote) {
		Debug.Log("Handling an emote: " + emote);
		foreach (int i in emote.parse.ChildrenOf(-1)) {
			if (emote.parse.partOfSpeech[i] == PartOfSpeech.NN) {
				ObjSpec obj = Grok.GrokObject(emote.parse, i);
				Debug.Log("Found object: " + obj);
				if (obj == null) return;
				SetValue("me:intent:targetName", "", comment);
				SetValue("me:intent:target", "", comment);
				SetValue("me:speech:intent", "OK.", comment);
				SetValue("me:intent:action", "setDown", comment);
 				
				foreach (var com in pendingCommand) {
					Debug.Log("Updating " + com + " to refer to " +obj);
					com.action.directObject = obj;
				}
 				
				var act = new ActionSpec();
				act.action = Action.PickUp;
				act.directObject = obj;
				var cmd = new ComCommand(emote.originalText, emote.parse, act);
				pendingCommand.Insert(0, cmd);
				pendingTime = Time.time + 1;
				Debug.Log("Inserting pending command: " + cmd);
				pendingGrabTarget = "";
				GrabPlaceModule.paused = false;
			}
		}
	}
	
	// Todo: This should return a Voxeme
    //  Voxemes allow robust filtering of objects by property
	Transform FindObjectFromSpec(Semantics.ObjSpec objSpec) {
		if (objSpec == null) return null;
		Debug.Log("Looking for object fitting: " + objSpec);
		
		Transform foundObject = null;
		
		if (objSpec.referredToAs == "it") {
			// If user says "it" while the agent is holding a block,
			// then let's assume they mean the held block.
			foundObject = null;
			if (GrabPlaceModule.heldObject != null) foundObject = GrabPlaceModule.heldObject.transform;	// unfortunate coupling... ToDo: improve this.
			if (foundObject != null) return foundObject;
			Debug.Log("Noted \"it\", but heldObject is null");
			// In this case, just guess at a block.
		}
		
		bool userIsPointing = DataStore.GetBoolValue("user:isPointing");
		Vector3 pointPos = DataStore.GetVector3Value("user:pointPos");
		float bestDistance = 1f;	// ignore anything more than 1 meter away
		
		for (int i=0; i<manipulableObjects.childCount; i++) {
			Transform candidate = manipulableObjects.GetChild(i);
			Renderer r = candidate.GetComponentInChildren<Renderer>();
			if (r == null) continue;
			string matName = r.sharedMaterial.name.ToLower();
			if (objSpec.color != null && matName == objSpec.color.ToString().ToLower()) {
				// Looks like a good match!
				foundObject = candidate;
				break;
			}
			if (objSpec.plurality == Plurality.Singular && objSpec.color == Semantics.Color.Unspecified) {
				// If no color was specified, but the user has indicated a single
				// item and is pointing, then pick the block closest to the point position.
				float dist = Vector3.Distance(candidate.position, pointPos);
				if (dist < bestDistance) {
					Debug.Log(candidate.name + " looks good at " + dist + " m away");
					foundObject = candidate;
					bestDistance = dist;
				} else {
					Debug.Log(candidate.name + " is no good, it's " + dist + " m away");
				}
			} else {
				Debug.Log("Not looking for points because plurality=" + objSpec.plurality + " and color=" + objSpec.color);
			}
		}
		if (foundObject == null) {
			// Still no idea wtf the user's talking about?  Pick a default block.
			foundObject	= manipulableObjects.GetChild(0);
			Debug.Log("Picking " + foundObject + " pretty much at random");
		}
		Debug.Log("Found: " + foundObject);
		return foundObject == null ? null : foundObject.transform;
	}
	
	void SayICant(string comment) {
		SetValue("me:speech:intent", "I can't.", comment);		
	}
	
	bool IsAgentsEyes(ObjSpec obj) {
		if (obj.referredToAs == "eyes" && obj.owner == Owner.You) {
			return true;
		}
		return false;
	}
}
