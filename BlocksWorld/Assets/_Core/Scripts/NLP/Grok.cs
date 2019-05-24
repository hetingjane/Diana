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
			if (EqualsAny(obj.referredToAs, "block", "one", "cube", "thing", "i", "me", "it")) {
				obj.plurality = Plurality.Singular;
			} else if (EqualsAny(obj.referredToAs, "blocks", "ones", "cubes", "things")) {
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
					if (EqualsAny(word, "the", "that", "those")) {
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
		
		public static ActionSpec GrokAction(ParseState st, int verbIdx) {
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
					act.action = Action.Put;
					break;
				case "close":
					act.action = Action.Close;
					break;
				case "open":
					act.action = Action.Open;
					break;
				case "thank":
					// whoops, this isn't an action, it's a phatic comment.
					return null;
			}

			var kids = st.ChildrenOf(verbIdx);
			if (kids != null) foreach (int i in kids) {
				if (st.partOfSpeech[i] == PartOfSpeech.NN) {
					ObjSpec obj = GrokObject(st, i);
					act.directObject = obj;
				}
			}
			
			return act;
		}

		public static Communication GrokInput(string text, ParseState st) {
			Communication comm = null;
			ActionSpec act = null;
			foreach (int child in st.ChildrenOf(-1)) {
				if (st.partOfSpeech[child] == PartOfSpeech.VB) {
					act = Grok.GrokAction(st, child);
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
			return comm;
		}

	}
}
