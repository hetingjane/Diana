/*
This module watches for (parsed) pleasantries and conversational control
phrases from the user, and responds appropriately.

Reads:		user:parse (WordValue)
Writes:		me:speech:intent (StringValue)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PleasantriesModule : ModuleBase {

	protected override void Start() {
		base.Start();
		DataStore.Subscribe("user:parse", NoteUserParse);
	}

	void NoteUserParse(string key, DataStore.IValue value) {
		var wordValue = value as ParserModule.WordValue;
		if (wordValue != null) HandleInput(wordValue.val);
	}

	void HandleInput(ParserModule.Word parseTree) {
		string head = parseTree.text.ToUpper();
		string fullTree = parseTree.ToString();
		
		// ToDo: some smarter (probably tree-based) pattern matching and
		// intent extraction.  For now, I'll just use some very simple
		// hard-coded patterns to get the prototype up and running.
		
		if (head == "HELLO" || head == "HI" || fullTree == "[DIANA dep[HELLO]]") {
			HandleHello();
			return;
		}
		
		if (fullTree == "[WHAT cop[IS] nsubj[NAME nmod:poss[YOUR]]]"
		 || fullTree == "[WHAT cop['S] nsubj[NAME nmod:poss[YOUR]]]") {
			HandleAskName();
			return;
		}
		
		if (fullTree.StartsWith("[ARE advmod[HOW] nsubj[YOU]")
		 || fullTree.StartsWith("[DOING advmod[HOW] aux[ARE]")) {
			HandleHowAreYou();
			return;
		}

		if (head == "PLEASED" || head == "HAPPY" || head == "GLAD") {
			HandlePleasedToMeetYou();
			return;
		}

		if (head == "THANK" || head == "THANKS") {
			HandleThanks();
			return;
		}

		// open/close eyes -- just for the Monday demo (commands like these really
		// belong in another module, and we should be checking more of the parse tree!)
		if (head == "CLOSE" || fullTree.StartsWith("[EYES advmod[CLOSE]")) {
			SetValue("me:intent:eyesClosed", true, "told to close eyes");
			return;
		}
		if (head == "OPEN" || fullTree.StartsWith("[EYES amod[OPEN]")) {
			SetValue("me:intent:eyesClosed", false, "told to open eyes");
			return;
		}
		
		comment = "Unhandled: " + fullTree;
	}

	void HandleHello() {
		SetValue("me:speech:intent", "Hello!", "responding to Hello");
	}
	
	void HandleAskName() {
		SetValue("me:speech:intent", "My name is Diana.", "was asked my name");
	}

	void HandleHowAreYou() {
		SetValue("me:speech:intent", "I am functioning within normal parameters.", "asked how I am");
	}

	void HandlePleasedToMeetYou() {
		SetValue("me:speech:intent", "The pleasure is all mine.", "pleased to meet you");
	}
	
	void HandleThanks() {
		SetValue("me:speech:intent", "You're welcome.", "was thanked");
	}
}
