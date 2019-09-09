using System;
using CWCNLP;

namespace Semantics
{
	// Base class for any communication.  It stores just the text and
	// parse.  Subclasses elaborate this with deeper meaning as needed.
	public class Communication {
		public string originalText;
		public ParseState parse;
		public bool directAddress;	// addressed by name, e.g. "Diana, say hello"
		
		public Communication(string originalText, ParseState parse) {
			this.originalText = originalText;
			this.parse = parse;
		}
		
		public override string ToString() {
			return string.Format("{0} : {1}", TypeStr(), parse == null ? originalText : parse.ToString());
		}
		
		protected virtual string TypeStr() { return "Communication"; }
	}
	
	// ComCommand: a command or request for the listener to do something.
	public class ComCommand : Communication {
		// action requested by the speaker:
		public ActionSpec action;
		
		public ComCommand(string originalText, ParseState parse, ActionSpec action) : base(originalText, parse) {
			this.action = action;
		}

		public override string ToString() {
			return string.Format("{0} : {1}", TypeStr(), action == null ? originalText : action.ToString());
		}

		protected override string TypeStr() { return "Command"; }
	}
	
	// ComQuery: a request for information from the listener
	public class ComQuery : Communication {
		public ComQuery(string originalText, ParseState parse) : base(originalText, parse) {
		}
	
		protected override string TypeStr() { return "Query"; }
	}
	
	// ComAssertion: imparting information to the listener
	public class ComAssertion : Communication {
		public ComAssertion(string originalText, ParseState parse) : base(originalText, parse) {
		}
	
		protected override string TypeStr() { return "Assertion"; }
	}
	
	// ComConfirmation: agreeing with or confirming something the listener said
	public class ComConfirmation : Communication {
		public ComConfirmation(string originalText, ParseState parse) : base(originalText, parse) {
		}
	
		protected override string TypeStr() { return "Confirmation"; }
	}
	
	// ComDenial: denial/disagreement with something the listener said
	public class ComDenial : Communication {
		public ComDenial(string originalText, ParseState parse) : base(originalText, parse) {
		}
	
		protected override string TypeStr() { return "Denial"; }
	}
	
	// ComEmote: an interjection or other expression of emotion or state,
	// e.g. wow, argh, etc.
	public class ComEmote : Communication {
		public ComEmote(string originalText, ParseState parse) : base(originalText, parse) {
		}
	
		protected override string TypeStr() { return "Emote"; }
	}
	
	// ComPhatic: a back-channel communication; conversational grease.
	// e.g. hello, please, thank you, I see, etc.
	public class ComPhatic : Communication {
		public ComPhatic(string originalText, ParseState parse, Type type) : base(originalText, parse) {
			this.type = type;
		}

		public enum Type {
			Greeting,			// hello, hi, etc.
			Goodbye,			// good-bye, etc.
			Please,				// stand-alone "please" referring to earlier request
			ThankYou,			// thank you, thanks, etc.
			ThanksReceived,		// you're welcome, etc.
			Understood			// I see, I understand, got it, etc.
		}
		public Type type;

		protected override string TypeStr() { return "Phatic"; }

		public override string ToString() {
			return string.Format("{0}({1}) : {2}", TypeStr(), type.ToString(), 
					parse == null ? originalText : parse.ToString());
		}
	}
	
}
