/*
This module watches for user:speech, and uses our custom NLP parsing/grokking
code to place a Communication (interpretation of the user's input) on the blackboard.

Reads:		user:speech (StringValue)
Writes:		user:communication (Communication)
*/
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using CWCNLP;
using Semantics;

public class ParserModule : ModuleBase {

	public TextAsset semcorWords;

	protected override void Start() {
		Debug.Assert(semcorWords != null);
		PartOfSpeech.Init(semcorWords.text);
		base.Start();
		DataStore.Subscribe("user:speech", NoteUserSpeech);
	}

	void NoteUserSpeech(string key, DataStore.IValue value) {
		string input = value.ToString().Trim();
		if (string.IsNullOrEmpty(input)) return;
		
		// Initialize a parser with the raw input
		var parser = new Parser();
		ParseState st = parser.InitState(input);
		
		// Apply parsing rules until we can do no more
		int rule;
		while ((rule = parser.NextStep(st)) > 0) {
			//Print(st.ToString() + " {" + rule + "}");
		}

		// Now, attempt to grok the input (convert it to a Communication)
		Communication comm = Grok.GrokInput(input, st);
		
		SetValue("user:communication", new CommunicationValue(comm), comm.ToString());
	}


}

//
// CommunicationValue: wrapper for a Communication in the data store.
//
public class CommunicationValue : DataStore.IValue {
	public Communication val;
	public CommunicationValue(Communication inVal) { this.val = inVal; }
	public override string ToString() { return val.ToString(); }
	public bool Equals(DataStore.IValue other) { return other is CommunicationValue && val.Equals(((CommunicationValue)other).val); }
	public bool IsEmpty() { return val == null; }
}
