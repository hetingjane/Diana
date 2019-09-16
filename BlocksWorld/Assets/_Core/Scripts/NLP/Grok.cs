using Semantics;

namespace CWCNLP
{
	public static class Grok {
	
		public static bool EqualsAny(string s, params string[] options) {
			return System.Array.IndexOf(options, s) >= 0;
		}
	
		public static ObjSpec GrokObject(ParseState st, int root) {
			var obj = new ObjSpec();
			
			obj.referredToAs = st.words[root].ToLower();
			// ToDo: we could get better determination of singular/plural,
			// and get the lemma at the same time, from the SEMCOR corpus.
			// But for now we'll just hard-code a few.
			if (EqualsAny(obj.referredToAs, "i", "me", "you", "it", "they", "them")) {
				obj.specificity = Specificity.Named;
			}
			if (EqualsAny(obj.referredToAs, "block", "one", "cube", "thing", "i", "me", "it", "box", "book")) {
				obj.plurality = Plurality.Singular;
			} else if (EqualsAny(obj.referredToAs, "blocks", "ones", "cubes", "things", "boxes", "books")) {
				obj.plurality = Plurality.Plural;
				obj.referredToAs = obj.referredToAs.Substring(0, obj.referredToAs.Length-1);
			} else if (EqualsAny(obj.referredToAs, "they", "them")) {
				obj.plurality = Plurality.Plural;
			}
			
			var kids = st.ChildrenOf(root);
			if (kids != null) foreach (int i in kids) {
				string pos = st.partOfSpeech[i];
				string word = st.words[i].ToLower();
				if (pos == PartOfSpeech.DT) {
					if (EqualsAny(word, "the", "that", "this", "those")) {
						obj.specificity = Specificity.Specific;
					} else if (EqualsAny(word, "a", "some", "any")) {
						obj.specificity = Specificity.Nonspecific;
					}
				} else if (pos == PartOfSpeech.JJ) {
					switch (word) {
					case "big":
					case "large":
						obj.vagueSize = VagueSize.Large;
						break;
					case "medium":
						obj.vagueSize = VagueSize.Medium;
						break;
					case "small":
					case "little":
						obj.vagueSize = VagueSize.Small;
						break;
					case "left":
						obj.leftRight = LeftRightAxis.Left;
						break;
					case "right":
						obj.leftRight = LeftRightAxis.Right;
						break;
					case "your":
						obj.owner = Owner.You;
						break;
					case "my":
						obj.owner = Owner.Me;
						break;
					default:
						foreach (Color c in System.Enum.GetValues(typeof(Color))) {
							if (word == c.ToString().ToLower()) {
								obj.color = c;
								break;
							}
						}
						break;
					}
				}
			}
			return obj;
		}
	
		public static LocationSpec GrokLocation(ParseState st, int root) {
			var loc = new LocationSpec();
			string preposition = st.words[root].ToLower();
			switch (preposition) {
			case "on":
			case "on_top_of":
			case "atop":
				loc.relation = LocationSpec.Relation.OnTopOf;
				break;
			case "in":
			case "inside":
			case "within":
				loc.relation = LocationSpec.Relation.Inside;
				break;
			case "next_to":
			case "beside":
				loc.relation = LocationSpec.Relation.NextTo;
				break;
			case "under":
			case "underneath":
			case "beneath":
				loc.relation = LocationSpec.Relation.Under;						
				break;
			case "to":
			case "at":
			case "towards":
				loc.relation = LocationSpec.Relation.Towards;
				break;
			}

			var kids = st.ChildrenOf(root);
			if (kids != null) foreach (int i in kids) {
				if (st.partOfSpeech[i] == PartOfSpeech.NN) {
					loc.obj = GrokObject(st, i);
					break;
				}
			}
			return loc;
		}
		
		public static ActionSpec GrokQuery(ParseState st, int queryWordIdx) {
			// Although we're grokiing a query, we convert that into an action
			// that describes what it is the agent should actually do to answer.
			var act = new ActionSpec();

			// For now, we support only identification queries, so:
			act.action = Action.Identify;
			
			// Find the verb.
			int verbIdx = st.FindChildOfType(PartOfSpeech.VB);
			if (verbIdx >= 0) {
				// Now find the NN of that verb.  This will be the object of the query.
				int nounIdx = st.FindChildOfType(PartOfSpeech.NN, verbIdx);
				if (nounIdx >= 0) {
					act.directObject = GrokObject(st, nounIdx);
				}
			}
			
			return act;
		}
		
		public static ActionSpec GrokAction(ParseState st, int verbIdx) {
			//UnityEngine.Debug.Log("GrokAction: " + st.ToString() + " with verb at " + verbIdx);
			var act = new ActionSpec();
			string verb = st.words[verbIdx].ToLower();
			switch (verb) {
				case "grab":
				case "pick_up":
					act.action = Action.PickUp;
					break;
				case "drop":
				case "set_down":
				case "put_down":
					act.action = Action.SetDown;
					break;
				case "say":
					act.action = Action.Say;
					break;
				case "put":
				case "set":
				case "place":
					act.action = Action.Put;
					break;
				case "close":
					act.action = Action.Close;
					break;
				case "open":
					act.action = Action.Open;
					break;
				case "raise":
				case "lift":
					act.action = Action.Raise;
					break;
				case "lower":
					act.action = Action.Lower;
					break;
				case "look":
				case "look_at":
					act.action = Action.Look;
					break;
				case "point":
					act.action = Action.Point;
					break;
				case "stop":
					act.action = Action.Stop;
					break;
				case "thank":
				case "thank_you":
					// whoops, this isn't an action, it's a phatic comment.
					return null;
				case "stand_by":
					act.action = Action.StandBy;
					break;
			}

			// Check for specific action idioms.
			string verbTree = st.TreeForm(verbIdx);
			if (verbTree == "VB[is RB[enough]]" && verbIdx > 0 && st.words[verbIdx-1] == "that") {
				// "that's enough": idiom for "stop"
				act.action = Action.Stop;
			}

			var kids = st.ChildrenOf(verbIdx);
			if (kids != null) foreach (int i in kids) {
				if (st.partOfSpeech[i] == PartOfSpeech.NN) {
					ObjSpec obj = GrokObject(st, i);
					act.directObject = obj;
					if (obj.referredToAs == "hand" && verb == "follow") {
						// "Follow my hand" idiom
						act.action = Action.Point;
					}
				}
				if (st.partOfSpeech[i] == PartOfSpeech.WRB) {
					// e.g.: [VB[point WRB[where]] VB[NN[I] point]]
					// For now, we'll assume any "where" clause boils down to:
					act.direction = new DirectionSpec(DirectionSpec.Direction.WhereUserPoints);
				}
				if (st.partOfSpeech[i] == PartOfSpeech.RB) {
					if (st.words[i] == "up" && verb == "pick") {
						act.action = Action.PickUp;
					} else if (st.words[i] == "down" && (verb == "put" || verb == "set" || verb == "place")) {
						act.action = Action.SetDown;
					} else if (st.words[i] == "here" || st.words[i] == "there"
					|| st.words[i] == "over_here" || st.words[i] == "over_there") {
						act.location = new LocationSpec() { relation = LocationSpec.Relation.Indicated };
					}
				}
			}			
			
			return act;
		}

		public static Communication GrokInput(string text, ParseState st) {
			Communication comm = null;
			ActionSpec act = null;
			foreach (int child in st.ChildrenOf(-1)) {
				if (act == null) {
					switch (st.partOfSpeech[child]) {
					case PartOfSpeech.VB:
						act = Grok.GrokAction(st, child);
						break;
					case PartOfSpeech.WP:
					case PartOfSpeech.WDT:
					case PartOfSpeech.WRB:
						act = Grok.GrokQuery(st, child);
						break;
					}
				}
				if (st.partOfSpeech[child] == PartOfSpeech.IN) {
					// Here's where we get fancy: based on the verb, and
					// possibly the current state of the world, figure out
					// this prepositional phrase modifies the last object
					// already parsed, or instead modifies the verb.
					// For now, we'll just assume it modifies the verb.
					if (act != null) {
						act.location = GrokLocation(st, child);
					}
				}
				if (st.partOfSpeech[child] == PartOfSpeech.UH) {
					switch (st.words[child].ToLower()) {
					case "hello":
					case "hi":
						comm = new ComPhatic(text, st, ComPhatic.Type.Greeting);
						break;
					case "bye":
					case "goodbye":
					case "good-bye":
						comm = new ComPhatic(text, st, ComPhatic.Type.Goodbye);
						break;
					default:
						comm = new ComEmote(text, st);
						break;
					}
				} else if (st.partOfSpeech[child] == PartOfSpeech.VB) {
					switch (st.words[child].ToLower()) {
					case "thank":
					case "thank_you":
						comm = new ComPhatic(text, st, ComPhatic.Type.ThankYou);
						break;
					}
				}
			}
			
			if (act != null && act.action != null) {
				// For now, we'll assume commands...
				comm = new ComCommand(text, st, act);
			} else if (comm == null) {
				// Not sure what to do with this.  Wrap it in the base class.
				comm = new Communication(text, st);
			}
			
			// Check for a direct address.
			// ToDo: find and use user self-knowledge, rather than hard-coded names.
			var firstWord = st.words[0].ToLower();
			if (firstWord == "diana" || firstWord == "sam") {
				comm.directAddress = true;
			}
			
			return comm;
		}

	}
	
	public class GrokUnitTest : QA.UnitTest {
		void Test(string input, string expectedComm) {
			ParseState st = Parser.Parse(input);
			string s = st.TreeForm();
			Communication comm = Grok.GrokInput(input, st);
			if (comm.ToString() == expectedComm) return;
			Fail("Grok failed on: " + input);
			Fail("Parse: " + st.TreeForm());
			QA.UnitTest.AssertEqual(expectedComm, comm.ToString());
		}
		
		protected override void Run() {
			Test("stop", "Command : [Act:Stop]");
			Test("that's enough", "Command : [Act:Stop]");
			Test("pick up this block", "Command : [Act:PickUp Obj:[the single block]]");
			Test("pick up that one", "Command : [Act:PickUp Obj:[the single one]]");
			Test("put it down", "Command : [Act:SetDown Obj:[single it]]");
			Test("set it down", "Command : [Act:SetDown Obj:[single it]]");
			Test("pick it up", "Command : [Act:PickUp Obj:[single it]]");
			Test("place it on this one", "Command : [Act:Put Obj:[single it] Loc:[OnTopOf [the single one]]]");			
			Test("set it down over here", "Command : [Act:SetDown Obj:[single it] Loc:[Indicated ]]");
			Test("put it over there", "Command : [Act:Put Obj:[single it] Loc:[Indicated ]]");
			Test("stand by", "Command : [Act:StandBy]");
			Test("what is this block", "Command : [Act:Identify Obj:[the single block]]");
			Test("which block is this", "Command : [Act:Identify Obj:[single block]]");
		}
	}
}
