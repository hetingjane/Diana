using System;

namespace Semantics
{
	public enum Color {
		Unspecified,
		Red,
		Green,
		Blue,
		Yellow
	}
	
	public enum VagueSize {
		Unspecified,
		Small,
		Medium,
		Large
	}
	
	public enum Specificity {
		Named,			// referred to by name or pronoun without a determinant
		Specific,		// the, that, those
		Nonspecific,	// a, an, any, some
	}
	
	public enum Plurality {
		Unspecified,
		Singular,
		Plural,
	}
	
	public class ObjSpec {
		public string referredToAs;		// main noun or pronoun used
		public Color color;
		public VagueSize vagueSize;
		public Specificity specificity;
		public Plurality plurality;
		
		public override string ToString() {
			var sb = new System.Text.StringBuilder();
			sb.Append("[");
			if (specificity == Specificity.Specific) {
				sb.Append("the ");
			} else if (specificity == Specificity.Nonspecific) {
				sb.Append("any ");
			}
			if (plurality == Plurality.Singular) sb.Append("single ");
			if (plurality == Plurality.Plural) sb.Append("multiple ");
			if (vagueSize != VagueSize.Unspecified) {
				sb.Append(vagueSize.ToString());
				sb.Append(" ");
			}
			if (color != Color.Unspecified) {
				sb.Append(color.ToString());
				sb.Append(" ");
			}
			sb.Append(referredToAs);
			sb.Append("]");
			return sb.ToString();
		}
	}
	
	public class ObjSpecUnitTest : QA.UnitTest {
		protected override void Run() {
			var obj = new ObjSpec();
			obj.referredToAs = "block";
			obj.specificity = Specificity.Specific;
			AssertEqual("[the block]", obj.ToString());
			obj.specificity = Specificity.Nonspecific;
			obj.plurality = Plurality.Plural;
			obj.vagueSize = VagueSize.Large;
			obj.color = Color.Red;
			AssertEqual("[any multiple Large Red block]", obj.ToString());
			
		}
	}
}
