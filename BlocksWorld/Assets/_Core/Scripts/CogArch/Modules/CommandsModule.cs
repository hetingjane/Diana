/*
This module watches for (parsed and grokked) commands, and reacts
appropriately (by setting me:intent values on the blackboard).

Reads:		user:communication (Communication)
Writes:		me:intent:eyesClosed (BoolValue)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Semantics;

public class CommandsModule : ModuleBase
{
	protected override void Start() {
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
		default:
			SayICant(comment);
			break;
		}
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
