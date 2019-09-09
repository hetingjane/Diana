using System;
namespace Semantics
{
	public class LocationSpec {
		public enum Relation {
			Unknown,
			OnTopOf,
			NextTo,
			Under,
			Inside,
			Towards,
			Indicated  // i.e. pointed at: "here", "there", etc.
		}
		
		public Relation relation;
		public ObjSpec obj;
		
		public override string ToString() {
			return string.Format("[{0} {1}]", relation, obj);
		}
	}
}
