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

public class CommandsModule : ModuleBase
{
	[Tooltip("Container for objects the agent can see and manipulate")]
	public Transform manipulableObjects;
	
	protected override void Start() {
		Debug.Assert(manipulableObjects != null);	// we need this asigned
		
		base.Start();
		DataStore.Subscribe("user:communication", NoteUserCommunication);
	}

	void NoteUserCommunication(string key, DataStore.IValue value) {
		var comVal = value as CommunicationValue;
		if (comVal != null) {
			ComCommand cmd = comVal.val as ComCommand;
			if (cmd != null) HandleCommand(cmd);
		}
	}

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
			// For now we assume the direction to point is always where the user is pointing.
			SetValue("me:speech:intent", "OK.", comment);		
			SetValue("me:intent:lookAt", "userPoint", comment);
			break;
		case Action.Stop:
			SetValue("me:speech:intent", "OK.", comment);		
			SetValue("me:intent:lookAt", "", comment);
			SetValue("me:intent:pointAt", "", comment);
			SetValue("me:intent:action", "", comment);
			break;
		case Action.PickUp:
		case Action.Raise:
			// See if we can determine what object to pick up, based on spec.
			if (act.directObject == null) return;	// (user is probably not done speaking)
			obj = FindObjectFromSpec(act.directObject);
			if (obj == null) {
				SetValue("me:speech:intent", "I don't know what block you mean.", comment);
			} else {
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
			obj = FindObjectFromSpec(act.directObject);
			if (obj != null && obj.name != DataStore.GetStringValue("me:holding")) {
				SetValue("me:speech:intent", "I'm not holding that.", comment);
			} else {
				bool allGood = true;
				if (act.location == null) {
					// No location specified.
					SetValue("me:intent:targetName", "", comment);
					SetValue("me:intent:target", "", comment);
				} else {
					// Location specified.
					if (act.location.relation == LocationSpec.Relation.Indicated) {
						// User is saying "here" or "there" and should be pointing.
						if (!DataStore.GetBoolValue("user:isPointing")) {
							SetValue("me:speech:intent", "I don't know where you mean.", comment);
							allGood = false;
						} else {
							SetValue("me:intent:targetName", "", comment);
							SetValue("me:intent:target", DataStore.GetVector3Value("user:pointPos"), comment);
						}
					} else {
						// User specified an object to set it on.
						Transform relObj = FindObjectFromSpec(act.location.obj);
						if (relObj == null && act.location.obj != null) {
							SetValue("me:speech:intent", "I don't know where you mean.", comment);
							allGood = false;
						}
						SetValue("me:intent:target", relObj.position, comment);
						SetValue("me:intent:targetName", relObj == null ? "" : relObj.name, comment);
					}
				}
				if (allGood) {
					SetValue("me:speech:intent", "OK.", comment);		
					SetValue("me:intent:action", "setDown", comment);
				}
			}
			break;
		default:
			// Let's not say "I can't" to every unknown command.
			// At least not yet ... it gets pretty annoying.
			//SayICant(comment);
			break;
		}
	}
	
	Transform FindObjectFromSpec(Semantics.ObjSpec objSpec) {
		if (objSpec == null) return null;
		Debug.Log("Looking for object fitting: " + objSpec);
		
		Transform foundObject = null;
		
		if (objSpec.referredToAs == "it") {
			// If user says "it" while the agent is holding a block,
			// then let's assume they mean the held block.
			foundObject = BipedIKGrab.heldObject;	// unfortunate coupling... ToDo: improve this.
			if (foundObject != null) return foundObject;
			Debug.Log("Noted \"it\", but heldObject is null");
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
		Debug.Log("Found: " + foundObject);
		return foundObject;
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
