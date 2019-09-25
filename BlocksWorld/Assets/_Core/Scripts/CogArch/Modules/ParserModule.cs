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

	[Tooltip("Semcor Words asset (statistical part-of-speech data)")]
	public TextAsset semcorWords;

	protected void Awake() {
		Debug.Assert(semcorWords != null);
		var timer = new System.Diagnostics.Stopwatch();
		timer.Start();
		PartOfSpeech.Init(semcorWords.text);
		timer.Stop();
		Debug.Log("PartOfSpeech.Init took " + timer.Elapsed.Seconds + " seconds");
	}
	
	protected override void Start() {
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
		SetValue("user:parse", st.TreeForm(), "parsed: " + input);

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
