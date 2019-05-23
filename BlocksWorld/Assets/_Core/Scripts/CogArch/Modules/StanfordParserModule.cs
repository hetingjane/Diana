/*
This module watches for user:speech, and invokes the StanfordNLP parser
to convert that into a parse tree.

Reads:		user:speech (StringValue)
Writes:		user:parse (WordValue)
*/
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Net;
using System.Threading;
using SimpleJSON;

public class StanfordParserModule : ModuleBase {
	
	public class Word {
		public string text;					// actual text of the word
		public string dependencyRelation;	// how this word is related to its parent
		public List<Word> children;			// dependent words
		
		public Word(string text, string dependencyRelation=null) {
			this.text = text;
			this.dependencyRelation = dependencyRelation;
			children = new List<Word>();
		}

		public override string ToString() {
			return AsTreeString();
		}

		protected string AsTreeString(string depLabel=null, int maxDepth=10) {
			string s = depLabel + "[" + text.ToUpper();
			if (maxDepth > 0) {
				foreach (Word kid in children) {
					s += " " + kid.AsTreeString(kid.dependencyRelation, maxDepth-1);
				}
			}
			return s + "]";
		}
		
		public bool Equals(Word other) {
			if (text != other.text) return false;
			if (children.Count != other.children.Count) return false;
			for (int i=0; i<children.Count; i++) {
				if (!children[i].Equals(other.children[i])) return false;
			}
			return true;
		}
	}
	
	public class WordValue : DataStore.IValue {
		public Word val;
		public WordValue(Word inVal) { this.val = inVal; }
		public override string ToString() { return val.ToString(); }
		public bool Equals(DataStore.IValue other) { return other is WordValue && val.Equals(((WordValue)other).val); }
		public bool IsEmpty() { return val == null; }
	}


	protected override void Start() {
		base.Start();
		DataStore.Subscribe("user:speech", NoteUserSpeech);
	}

	string currentInput;
	Word newParse;
	
	void NoteUserSpeech(string key, DataStore.IValue value) {
		// Parse the user's speech.
		string speech = value.ToString();
		if (!string.IsNullOrEmpty(speech)) UpdateParse(speech);
		else SetValue("user:parse", "", "speech is empty");
	}	
	
	void UpdateParse(string input) {
		currentInput = input;
		var thread = new Thread(new ThreadStart(RequestParse));
		thread.Start();
	}
	
	void RequestParse() {
		// (ToDo: find a proper URL encoding solution!)
		string input = currentInput;
		if (input.ToLower().StartsWith("diana,")) input = input.Substring(6).TrimStart();
		if (input.ToLower().StartsWith("please")) input = input.Substring(6).TrimStart();
		input = input.Replace(" ", "+");
		input = input.Replace("%", "%25");
		input = input.Replace("&", "%26");
		input = input.Replace("?", "%3F");
		string dataStr = "outputFormat=json&Process=Submit&input=" + input;
		byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
		
		// Also ToDo: find an async method, or move this into a thread
		WebRequest request = WebRequest.Create("http://nlp.stanford.edu:8080/corenlp/process");
		request.Method = WebRequestMethods.Http.Post;
		request.ContentType = "application/x-www-form-urlencoded";
		request.ContentLength = data.Length;
		using (var stream = request.GetRequestStream()) {
			stream.Write(data, 0, data.Length);
		}
		var response = (HttpWebResponse)request.GetResponse();
		var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();
		
		int startPos = responseString.IndexOf("<pre>") + 5;
		int endPos = responseString.IndexOf("</pre>");
		string json = responseString.Substring(startPos, endPos - startPos);
		json = json.Replace("&nbsp;", " ").Replace("&quot;", "\"").Replace("&amp;", "&");
		JSONNode root = JSON.Parse(json);

		var sentenceList = root["sentences"];
		if (sentenceList.Count > 0) {
			JSONNode tokens = sentenceList[0]["tokens"];
			JSONNode dependencies = sentenceList[0]["basic-dependencies"];
			Word tree = JsonToTree(tokens as JSONArray, dependencies as JSONArray);
			newParse = tree;	// (store the data for the main thread)
		}
	}
	
	protected Word JsonToTree(JSONArray tokens, JSONArray dependencies) {
		List<Word> words = new List<Word>(tokens.Count);
		foreach (JSONNode n in tokens) {
			words.Add(new Word(n["word"]));
		}
		Word root = null;
		foreach (JSONNode dep in dependencies) {
			int parentIdx = dep["governor"].AsInt;
			int childIdx = dep["dependent"].AsInt;
			Word child = words[childIdx-1];
			child.dependencyRelation = dep["dep"];
			if (parentIdx > 0) words[parentIdx-1].children.Add(child);
			if (child.dependencyRelation == "ROOT") root = child;
		}
		return root;
	}
	
	
	protected void Update() {
		// Because DataStore.SetValue currently must be called from the main thread,
		// we store the parse in our web request thread, and actually set it here.
		Word p = newParse;
		if (p != null) {
			newParse = null;
			SetValue("user:parse", new WordValue(p), "got parse from StanfordNLP");
		}
	}
}
