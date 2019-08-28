/*
This module is responsible for looking up likely parts of speech for any given word.
Tags are compatible with the Brown corpus:
https://en.wikipedia.org/wiki/Brown_Corpus#Part-of-speech_tags_used
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CWCNLP
{
	public static class PartOfSpeech {
		public const string NN = "NN";		// noun
		public const string JJ = "JJ";		// adjective
		public const string CD = "CD";		// ordinal number (one, two, etc.)
		public const string CC = "CC";		// conjunction (and, or)
		public const string IN = "IN";		// preposition
		public const string RB = "RB";		// adverb
		public const string DT = "DT";		// determinant
		public const string NP = "NP";		// proper noun
		public const string VB = "VB";		// verb
		public const string RP = "RP";		// adverb particle (off, up)
		public const string WRB = "WRB";	// WH-adverb (where)
		public const string PPS = "PPS";	// singular pronoun (it, one)
		public const string UH = "UH";		// interjections and greetings

		
		// There are a bunch of others, but these will do for now.
		
		struct PosEntry {
			public string pos;			// NN, JJ, etc.
			public float frequency;		// in the range [0,1]
		}
		
		struct SetPhrase {
			public string orig;		// thank you
			public string replace;	// thank_you
		}
		
		// mapping of words to lists of POS entries
		static Dictionary<string, List<PosEntry>> dictionary;
		
		// set phrases in our dictionary, without and with underscores
		// (e.g. "thank you" -> "thank_you")
		static List<SetPhrase> setPhrases;
		
		static void AddEntry(string word, string data) {
			var list = new List<PosEntry>();
			foreach (string item in data.Split(new char[]{';'})) {
				string[] parts = item.Split(new char[]{'='});
				var entry = new PosEntry();
				entry.pos = parts[0];
				float.TryParse(parts[1], out entry.frequency);
				list.Add(entry);
			}
			dictionary[word] = list;		
		}
		
		static void AddSetPhrase(string orig, string pat, string data=null) {
			var sp = new SetPhrase();
			sp.orig = orig;
			sp.replace = pat;
			setPhrases.Add(sp);
			if (data != null) AddEntry(pat, data);
		}
		
		static void RemoveSetPhrase(string orig) {
			int idx = setPhrases.FindIndex(x => x.orig == orig);
			if (idx >= 0) {
				dictionary.Remove(setPhrases[idx].replace);
				setPhrases.RemoveAt(idx);
			}
		}
		
		static PartOfSpeech() {
			dictionary = new Dictionary<string, List<PosEntry>>();
			setPhrases = new List<SetPhrase>();
		}
		
		public static void Init(string semcoreWords) {
			
			// Read dictionary data and set phrases from the SEMCOR corpus
			string[] lines = semcoreWords.Split(new char[]{'\r','\n'});
			foreach (string line in lines) {
				int colonPos = line.IndexOf(':');
				if (colonPos < 0) continue;
				string word = line.Substring(0, colonPos);
				string data = line.Substring(colonPos+1);
				AddEntry(word, data);
				
				// Note some exceptions coded into the following, where SEMCOR just
				// has dumb set phrases like "on_the" that only make things harder.
				if (word.IndexOf('_') >= 0 && word.IndexOf('(') < 0 && word.IndexOf(')') < 0
					&& !word.EndsWith("_the")) {
					AddSetPhrase(word.Replace('_', ' '), word);
				}
			}
			
			// Replace ones that we just flat-out disagree with (like "no" being 
			// overwhelmingly a DT, while "yes" is an UH).
			AddEntry("no", "UH=0.6;DT=0.3;NN=0.1");
			AddEntry("No", "UH=0.6;DT=0.3;NN=0.1");
			AddEntry("how_many", "WQL=1");
			AddEntry("that", "DT=1");
			AddEntry("plus", "CC=1");
			AddEntry("minus", "CC=1");
			AddEntry("times", "CC=1");
			AddEntry("divided_by", "CC=1");
			AddEntry("hi", "UH=1");
			AddEntry("bye", "UH=1");
			AddEntry("close", "VB=0.6;JJ=0.3;RB=0.1");
			AddEntry("Close", "VB=0.9;RB=0.1");
			AddEntry("open", "VB=0.6;JJ=0.3;RB=0.1");
			AddEntry("Open", "VB=0.9;RB=0.1");
			AddEntry("point", "VB=0.6;NN=0.4");
			AddEntry("Point", "VB=0.9;NN=0.1");
			AddEntry("Pick", "VB=0.8;NN=0.2");
			AddEntry("thank_you", "VB=1");
			AddEntry("Thank_you", "VB=1");
			RemoveSetPhrase("two times");
			RemoveSetPhrase("three times");
			RemoveSetPhrase("four times");
			RemoveSetPhrase("five times");
			RemoveSetPhrase("six times");
			RemoveSetPhrase("seven times");
			RemoveSetPhrase("eight times");
			RemoveSetPhrase("nine times");
			RemoveSetPhrase("ten times");
			RemoveSetPhrase("the blue");
			
			// And add some additional set phrases they missed
			AddSetPhrase("to the right of", "right_of", "IN=1");
			AddSetPhrase("to the left of", "left_of", "IN=1");
			AddSetPhrase("right of", "right_of", "IN=1");
			AddSetPhrase("left of", "left_of", "IN=1");
			AddSetPhrase("north of", "north_of", "IN=1");
			AddSetPhrase("south of", "south_of", "IN=1");
			AddSetPhrase("east of", "east_of", "IN=1");
			AddSetPhrase("west of", "west_of", "IN=1");
			AddSetPhrase("northeast of", "northeast_of", "IN=1");
			AddSetPhrase("southeast of", "southeast_of", "IN=1");
			AddSetPhrase("northwest of", "northwest_of", "IN=1");
			AddSetPhrase("southwest of", "southwest_of", "IN=1");
			AddSetPhrase("divided by", "divided_by", "CC=1");
			AddSetPhrase("multiplied by", "multiplied_by", "CC=1");
			AddSetPhrase("pick up", "pick_up", "VB=0.7;NN=0.3");
			AddSetPhrase("Pick up", "pick_up", "VB=0.9;NN=0.1");
			AddSetPhrase("over there", "over_there", "RB=1.000");
			
			// Sort the set phrases by length, longest ones first.
			setPhrases.Sort((a,b) => (b.orig.Length.CompareTo(a.orig.Length)));
			
			Console.WriteLine("Read " + dictionary.Count + " dictionary entries");
		}
		
		public static string CollapseSetPhrases(string text) {
			// replace our set phrases
			foreach (var sp in setPhrases) {
				//string s = text;
				text = System.Text.RegularExpressions.Regex.Replace(text, "\\b" + sp.orig + "\\b", sp.replace);
				//if (text != s) Console.WriteLine("Applied: " + sp.orig + " --> " + sp.replace);
			}
			// special replacements for math operators (which don't work above due to RegEx limitations)
			text = text.Replace("+", " plus ");
			text = text.Replace("-", " minus ");
			text = text.Replace("*", " times ");
			text = text.Replace("/", " divided_by ");
			// and compress space runs
			text = System.Text.RegularExpressions.Regex.Replace(text, " +", " ");
			
			return text;
		}
		
		public static string PrimaryPOS(string word) {
			float floatVal;
			if (float.TryParse(word, out floatVal)) return PartOfSpeech.CD;
			
			List<PosEntry> entryList;
			if (!dictionary.TryGetValue(word, out entryList)) return null;
			switch (entryList[0].pos) {
				case "EX":		// existential "there"
					return PartOfSpeech.RB;
				case "NNS":		// plural noun
				case "PRP":		// pronoun(?)
					return PartOfSpeech.NN;
				case "PRP$":	// possessive pronoun (e.g. "my")
					return PartOfSpeech.JJ;
				default:
					return entryList[0].pos;
			}
		}
	}
}
