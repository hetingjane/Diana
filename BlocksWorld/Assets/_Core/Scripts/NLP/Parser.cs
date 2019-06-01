/*
This class takes an English string, breaks it into words, and then works out
a dependency tree by identifying which word each word modifies.

It also provides some utilities for converting such a dependency tree to compact
string form and vice versa, etc.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace CWCNLP
{
	public class ParseState {
		// "ROOT" followed by the text of each word
		public string[] words;
		
		// part of speech currently assigned to each word, or null if unassigned
		public string[] partOfSpeech;
		
		// dependencies identified so far:
		// -1 means unassigned,
		// any other value is the word this one modifies
		public int[] dependencies;
		
		// score: higher is better
		public int score;
		
		/// <summary>
		/// Clone this state, readying it to be the next step in search space.
		/// </summary>
		/// <returns>The clone.</returns>
		public ParseState Clone() {
			ParseState result = new ParseState();
			result.words = words;		// OK to share these
			int count = words.Length;
			result.partOfSpeech = new string[count];
			Array.Copy(partOfSpeech, result.partOfSpeech, count);
			result.dependencies = new int[count];
			Array.Copy(dependencies, result.dependencies, count);
			result.score = score;
			return result;
		}
		
		public void Init(string input) {
			List<string> inputWords = new List<string>();
			foreach (string w in input.Split(new char[]{' ', ',', '.', '?', '!'})) {
				var w2 = w.Trim();
				if (!string.IsNullOrEmpty(w2)) inputWords.Add(w2);
			}
			Preprocess(inputWords);
			this.words = inputWords.ToArray();
			this.partOfSpeech = new string[words.Length];
			
			this.dependencies = new int[words.Length];
			for (int i=0; i<dependencies.Length; i++) {
				dependencies[i] = -1;
			}
			
			// Let's initialize by assigning the primary POS for each word.
			for (int i=0; i<words.Length; i++) {
				partOfSpeech[i] = PartOfSpeech.PrimaryPOS(words[i]);
			}

			CalculateScore();
		}
		
		/// <summary>
		/// Preprocess the given words, replacing some contractions with their
		/// extended versions, correcting obvious misspellings, etc.
		/// </summary>
		/// <param name="words"></param>
		void Preprocess(List<string> words) {
			for (int i=0; i<words.Count; i++) {
				if (words[i] == "that's") {
					words[i] = "that";
					words.Insert(i+1, "is");
				}
			}
		}
		
		public void CalculateScore() {
			// For now, at least, we'll just add up
			// how many words have dependency assigned.
			int result = 0;
			foreach (int d in dependencies) if (d >= 0) result++;
			score = result;
		}
		
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			for (int i=0; i<words.Length; i++) {
				if (i > 0) sb.Append(" ");
				sb.Append(words[i]);
				sb.Append("(");
				if (partOfSpeech[i] != null) {
					sb.Append(partOfSpeech[i]);
					sb.Append(":");
				}
				sb.Append(dependencies[i].ToString());
				sb.Append(")");
			}
			sb.Append("]");

			sb.Append("(Score=");
			sb.Append(score.ToString());
			sb.Append(")");

			return sb.ToString();
		}
		
		public List<int> ChildrenOf(int index) {
			List<int> result = null;
			for (int i=0; i<dependencies.Length; i++) {
				if (dependencies[i] != index) continue;
				if (result == null) result = new List<int>();
				result.Add(i);
			}
			return result;
		}
		
		public bool HasChildren(int index) {
			for (int i=0; i<dependencies.Length; i++) {
				if (dependencies[i] == index) return true;
			}
			return false;			
		}
		
		void TreeForm(int baseIndex, StringBuilder sb) {
			if (baseIndex >= 0) sb.Append(partOfSpeech[baseIndex]);
			sb.Append("[");
			bool first = true;
			for (int i=0; i<dependencies.Length; i++) {
				if (i == baseIndex) {
					if (!first) sb.Append(" ");
					sb.Append(words[i]);
					first = false;
				}
				if (dependencies[i] == baseIndex) {
					if (!first) sb.Append(" ");
					TreeForm(i, sb);
					first = false;
				}
			}
			sb.Append("]");
		}

		public string TreeForm(int baseIndex = -1) {
			var sb = new StringBuilder();
			TreeForm(baseIndex, sb);
			return sb.ToString();
		}
	}

	public class Parser {
		public Parser() {
		}
		
		public ParseState InitState(string input) {
			ParseState st = new ParseState();
			st.Init(input);
			return st;
		}
		
		public static ParseState Parse(string input) {
			string s = PartOfSpeech.CollapseSetPhrases(input);
			var parser = new Parser();
			var st = parser.InitState(s);
			
			int rule;
			while ((rule = parser.NextStep(st)) > 0) {
			}
			return st;
		}
		
		/// <summary>
		/// Figure out a good next step to improve the state.
		/// </summary>
		/// <returns>ID number of rule applied, or 0 if none</returns>
		/// <param name="st">state to improve</param>
		public int NextStep(ParseState st) {
			int maxi = st.words.Length - 1;
				
			// Any noun before another noun modifies it (e.g. "shell script")
			// and is really acting as an adjective.
			// (Work back to front.)
			for (int i=maxi-1; i>=0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if (st.partOfSpeech[i] == PartOfSpeech.NN &&
						st.partOfSpeech[i+1] == PartOfSpeech.NN) {
					st.dependencies[i] = i+1;
					st.partOfSpeech[i] = PartOfSpeech.JJ;
					st.CalculateScore();
					return 1;
				}
			}

			// The word "to" before a verb is a helping verb; but
			// before a determinant or noun equivalent, it's a preposition.
			for (int i=maxi-1; i>=0; i--) {
				if (st.dependencies[i] >= 0 || st.partOfSpeech[i] != "TO") continue;
				if (st.partOfSpeech[i+1] == PartOfSpeech.VB || st.partOfSpeech[i+1] == PartOfSpeech.RB) {
					st.dependencies[i] = i+1;
					st.CalculateScore();
					return 2;
				} else if (st.partOfSpeech[i+1] == PartOfSpeech.DT
						|| st.partOfSpeech[i+1] == PartOfSpeech.NP
						|| st.partOfSpeech[i+1] == PartOfSpeech.NN) {
					st.partOfSpeech[i] = PartOfSpeech.IN;
					st.CalculateScore();
					return 2;
				}
			}
			
			// Any adjective or number before a noun or pronoun modifies that thing.
			// (Work back to front.)
			for (int i=maxi-1; i>=0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if ((st.partOfSpeech[i] == PartOfSpeech.JJ ||
						st.partOfSpeech[i] == PartOfSpeech.CD) && (
						st.partOfSpeech[i+1] == PartOfSpeech.NN ||
						st.partOfSpeech[i+1] == PartOfSpeech.PPS)) {
					st.dependencies[i] = i+1;
					st.CalculateScore();
					return 3;
				}
			}
			
			// Any adjective or number before another one, modifies the same thing.
			for (int i=maxi-1; i>=0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if ((st.partOfSpeech[i] == PartOfSpeech.JJ ||
						st.partOfSpeech[i] == PartOfSpeech.CD) &&
						(st.partOfSpeech[i+1] == PartOfSpeech.JJ ||
						st.partOfSpeech[i+1] == PartOfSpeech.CD) &&
						st.dependencies[i+1] >= 0) {
					st.dependencies[i] = st.dependencies[i+1];
					st.CalculateScore();
					return 4;
				}
			}
			
			// A determinant modifies the closest noun or pronoun.
			int closestNN = -1;
			for (int i=maxi-1; i>=0; i--) {
				if (st.partOfSpeech[i+1] == PartOfSpeech.NN || st.partOfSpeech[i+1] == PartOfSpeech.PPS) closestNN = i+1;
				if (st.dependencies[i] >= 0) continue;
				if (st.partOfSpeech[i] == PartOfSpeech.DT && closestNN >= 0) {
					st.dependencies[i] = closestNN;
					st.CalculateScore();
					return 5;
				}
			}
			
			// Math stuff: CD (ordinal number) modifies CC (operator/conjunction) before or after;
			// CC's modify each other.  Doesn't really get opeorator precedence right at this point.
			for (int i=1; i<maxi; i++) {
				if (st.partOfSpeech[i-1] == PartOfSpeech.CD
						&& st.partOfSpeech[i+1] == PartOfSpeech.CD) {
					bool didAny = false;
					if (st.words[i] == "times" || st.words[i] == "divided_by") {
						if (st.dependencies[i-1] < 0) {
							st.dependencies[i-1] = i;
							didAny = true;
						}
						if (st.dependencies[i+1] < 0) {
							st.dependencies[i+1] = i;
							didAny = true;
						}
					} else if (st.words[i] == "plus" || st.words[i] == "minus") {
						if (st.dependencies[i-1] < 0) {
							st.dependencies[i-1] = i;
							didAny = true;
						}
						if (st.dependencies[i+1] < 0) {
							st.dependencies[i+1] = i;
							didAny = true;
						}						
					}
					if (didAny) return 6;
				}
			}
			
			// If the first word is of unknown type, but the second word is
			// either a determinant or an adverb particle, then the first word
			// must be a verb.
			if (maxi > 0 && st.partOfSpeech[0] == null && (
					st.partOfSpeech[1] == PartOfSpeech.DT ||
					st.partOfSpeech[1] == PartOfSpeech.RP ||
					st.partOfSpeech[1] == PartOfSpeech.WRB)) {
				st.partOfSpeech[0] = PartOfSpeech.VB;
				st.CalculateScore();
				return 7;
			}
			
			// If we have an adverb particle still unassigned, it probably modifies the closest verb.
			for (int i=maxi; i>0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if (st.partOfSpeech[i] != PartOfSpeech.RP || st.partOfSpeech[i] != PartOfSpeech.WRB) continue;
				for (int j=i-1; j>=0; j--) {
					if (st.partOfSpeech[j] == PartOfSpeech.VB) {
						st.dependencies[i] = j;
						st.CalculateScore();
						return 8;
					}
				}
			}
					
			// A noun right before a verb modifies that verb.
			for (int i=maxi-1; i>=0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if ((st.partOfSpeech[i+1] == PartOfSpeech.VB) && (
					st.partOfSpeech[i] == PartOfSpeech.NN ||
					st.partOfSpeech[i] == PartOfSpeech.PPS)) {
					st.dependencies[i] = i+1;
					st.CalculateScore();
					return 9;
				}
			}
			

			// If we have a noun or pronoun particle still unassigned, 
			// it probably modifies the closest verb or preposition.
			for (int i=maxi; i>0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if (st.partOfSpeech[i] != PartOfSpeech.NN && 
						st.partOfSpeech[i] != PartOfSpeech.PPS) continue;
				for (int j=i-1; j>=0; j--) {
					if (st.partOfSpeech[j] == PartOfSpeech.VB || st.partOfSpeech[j] == PartOfSpeech.IN) {
						st.dependencies[i] = j;
						st.CalculateScore();
						return 10;
					}
				}
			}
			
			// Getting a bit desperate, if we have unknown words after a determinant
			// or adjective, they are probably adjectives and nouns.
			for (int i=0; i<maxi; i++) {
				if (st.partOfSpeech[i] != PartOfSpeech.DT && st.partOfSpeech[i] != PartOfSpeech.JJ) continue;
				if (st.partOfSpeech[i+1] != null) continue;
				for (i++; i<=maxi && st.partOfSpeech[i] == null; i++) {
					st.partOfSpeech[i] = PartOfSpeech.NN;
					if (st.partOfSpeech[i-1] == PartOfSpeech.NN) st.partOfSpeech[i-1] = PartOfSpeech.JJ;
				}
				st.CalculateScore();
				return 11;
			}
			
			// An ordinal at the end of the sentence or phrase often acts as a noun
			// (unless its dependency has already been determined).
			for (int i=maxi; i>=0; i--) {
				if (st.partOfSpeech[i] != PartOfSpeech.CD || st.dependencies[i] >= 0) continue;
				if (i == maxi || st.partOfSpeech[i+1] == PartOfSpeech.CC
						|| st.partOfSpeech[i+1] == PartOfSpeech.IN
						|| st.partOfSpeech[i+1] == PartOfSpeech.DT) {
					st.partOfSpeech[i] = PartOfSpeech.NN;
					st.CalculateScore();
					return 12;
				}						
			}
			
			
			// Check for an ordinal followed by "of" (one of, three of, etc.).
			// The of in that case always modifies the ordinal.
			for (int i=maxi; i>0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if (st.partOfSpeech[i-1] == PartOfSpeech.CD && st.words[i] == "of") {
					st.dependencies[i] = i-1;
					st.CalculateScore();
					return 13;
				}						
			}
			
			
			// Any preposition hanging out at the end of the sentence is
			// actually just an adverb (e.g. "pick it up", "set it down", "clear it off")
			if (st.partOfSpeech[maxi] == PartOfSpeech.IN && st.dependencies[maxi] < 0) {
				for (int i=maxi-1; i>=0; i--) {
					if (st.partOfSpeech[i] == PartOfSpeech.VB) {
						st.dependencies[maxi] = i;
						st.partOfSpeech[maxi] = PartOfSpeech.RB;
						st.CalculateScore();
						return 14;
					}
				}
			}
			
			// Any adverb right before a verb, modifies that verb.
			// Or if it's right before an adverb, it modifies the same thing as that.
			for (int i=maxi-1; i>=0; i--) {
				if (st.partOfSpeech[i] == PartOfSpeech.RB && st.dependencies[i] < 0) {
					if (st.partOfSpeech[i+1] == PartOfSpeech.VB) {
						st.dependencies[i] = i+1;
						st.CalculateScore();
						return 15;
					}
					if (st.partOfSpeech[i+1] == PartOfSpeech.RB && st.dependencies[i+1] >= 0) {
						st.dependencies[i] = st.dependencies[i+1];
						st.CalculateScore();
						return 16;
					}
				}
			}
			
			// Any remaining adverbs modify the closest preceeding verb.
			for (int i=maxi; i>0; i--) {
				if (st.dependencies[i] >= 0) continue;
				if (st.partOfSpeech[i] != PartOfSpeech.RB && st.partOfSpeech[i] != PartOfSpeech.WRB) continue;
				for (int j=i-1; j>=0; j--) {
					if (st.partOfSpeech[j] == PartOfSpeech.VB) {
						st.dependencies[i] = j;
						st.CalculateScore();
						return 17;
					}
				}
			}
			
			
			return 0;
		}
	}
	
	public class ParserUnitTest : QA.UnitTest {
		void Test(string input, string expectedTree) {
			ParseState st = Parser.Parse(input);
			string s = st.TreeForm();
			if (s == expectedTree) return;
			Fail("Parse failed on: " + input);
			QA.UnitTest.AssertEqual(expectedTree, st.TreeForm());
		}
		
		protected override void Run() {
			Test("thank you", "[NN[thank_you]]");
			Test("no thank you", "[UH[no] NN[thank_you]]");
			Test("pick up the big red block", "[VB[pick_up NN[DT[the] JJ[big] JJ[red] block]]]");
			Test("pick the two big ones up", "[VB[pick NN[DT[the] CD[two] JJ[big] ones] RB[up]]]");
			Test("now put one of them down", "[VB[RB[now] put NN[one] RB[down]] IN[of NN[them]]]");
			Test("put a yellow block on the green one", "[VB[put NN[DT[a] JJ[yellow] block]] IN[on NN[DT[the] JJ[green] one]]]");
			Test("what is 2+3", "[WP[what] VB[is] CC[CD[2] plus CD[3]]]");
			Test("point where I point", "[VB[point WRB[where]] VB[NN[I] point]]");
			Test("point where I am pointing", "[VB[point WRB[where]] VB[NN[I] am] VB[pointing]]");
			Test("look where I point", "[VB[look WRB[where]] VB[NN[I] point]]");
			Test("look at the green block", "[VB[look_at NN[DT[the] JJ[green] block]]]");
			Test("point to the green block", "[VB[point] IN[to NN[DT[the] JJ[green] block]]]");
		}

	}
}
