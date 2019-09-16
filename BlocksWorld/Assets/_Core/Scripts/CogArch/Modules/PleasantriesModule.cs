/*
This module watches for (parsed) pleasantries and conversational control
phrases from the user, and responds appropriately.

Reads:		user:communication (Communication)
Writes:		me:speech:intent (StringValue)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Semantics;

public class PleasantriesModule : ModuleBase {

	protected override void Start() {
		base.Start();
		DataStore.Subscribe("user:communication", NoteUserCommunication);
		//Invoke("TestSpeech", 0.5f);
	}

	void TestSpeech() {
		SetValue("me:speech:intent", "System ready.", "test message for debugging");
	}

	void NoteUserCommunication(string key, DataStore.IValue value) {
		var comVal = value as CommunicationValue;
		if (comVal == null) return;
		
		// if we're standing by, then ignore anything that's not a direct address.
		if (DataStore.GetBoolValue("me:standingBy")) {
			if (!comVal.val.directAddress) return;
			// Note that we do NOT change me:standingBy here.  That's the 
			// responsibility of CommandsModule.  Doing it in both places
			// causes grief.
		}

		HandleInput(comVal.val);
	}

	void HandleInput(Communication comm) {

		string comment = "responding to " + comm;
		if (comm is ComPhatic) {
			switch ((comm as ComPhatic).type) {
			case ComPhatic.Type.Goodbye:
				SetValue("me:speech:intent", "Good-bye.", comment);
				break;
			case ComPhatic.Type.Greeting:
				SetValue("me:speech:intent", "Hello.", comment);
				break;
			case ComPhatic.Type.ThankYou:
				SetValue("me:speech:intent", "You're welcome.", comment);
				break;
			default:
				// other phatic communications (you're welcome, I see, etc.)
				// require no response.
				return;
			}
			
		}

		//string head = parseTree.text.ToUpper();
		//string fullTree = parseTree.ToString();
		
		//// ToDo: some smarter (probably tree-based) pattern matching and
		//// intent extraction.  For now, I'll just use some very simple
		//// hard-coded patterns to get the prototype up and running.
		
		//if (head == "HELLO" || head == "HI" || fullTree == "[DIANA dep[HELLO]]") {
		//	HandleHello();
		//	return;
		//}
		
		//if (fullTree == "[WHAT cop[IS] nsubj[NAME nmod:poss[YOUR]]]"
		// || fullTree == "[WHAT cop['S] nsubj[NAME nmod:poss[YOUR]]]") {
		//	HandleAskName();
		//	return;
		//}
		
		//if (fullTree.StartsWith("[ARE advmod[HOW] nsubj[YOU]")
		// || fullTree.StartsWith("[DOING advmod[HOW] aux[ARE]")) {
		//	HandleHowAreYou();
		//	return;
		//}

		//if (head == "PLEASED" || head == "HAPPY" || head == "GLAD") {
		//	HandlePleasedToMeetYou();
		//	return;
		//}

		//if (head == "THANK" || head == "THANKS") {
		//	HandleThanks();
		//	return;
		//}

		//// open/close eyes -- just for the Monday demo (commands like these really
		//// belong in another module, and we should be checking more of the parse tree!)
		//if (head == "CLOSE" || fullTree.StartsWith("[EYES advmod[CLOSE]")) {
		//	SetValue("me:intent:eyesClosed", true, "told to close eyes");
		//	return;
		//}
		//if (head == "OPEN" || fullTree.StartsWith("[EYES amod[OPEN]")) {
		//	SetValue("me:intent:eyesClosed", false, "told to open eyes");
		//	return;
		//}
		
		//comment = "Unhandled: " + fullTree;
	}

	void HandleHello() {
		SetValue("me:speech:intent", "Hello!", "responding to Hello");
	}
	
	void HandleHowAreYou() {
		SetValue("me:speech:intent", "I am functioning within normal parameters.", "asked how I am");
	}

	void HandlePleasedToMeetYou() {
		SetValue("me:speech:intent", "The pleasure is all mine.", "pleased to meet you");
	}
	
}
