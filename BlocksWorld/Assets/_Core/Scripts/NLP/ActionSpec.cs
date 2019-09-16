using System;
namespace Semantics
{	
	public enum Action {
		Unknown,
		PickUp,
		Raise,
		SetDown,
		Lower,
		Put,
		Say,
		Open,
		Close,
		Point,
		Look,
		Stop,
		StandBy,
		Resume,
		Identify,
		Count
	}
	
	
	public class ActionSpec {
		public ObjSpec subject;		// (may often be named)
		public Action action;
		public ObjSpec directObject;
		public LocationSpec location;
		public DirectionSpec direction;
		public ObjSpec instrument;
		
		public override string ToString() {
			var sb = new System.Text.StringBuilder();
			sb.Append("[");
			if (subject != null) sb.Append("Subj:" + subject + " ");
			sb.Append("Act:" + action.ToString());
			if (directObject != null) sb.Append(" Obj:" + directObject);
			if (location != null) sb.Append(" Loc:" + location);
			if (direction != null) sb.Append(" Dir:" + direction);
			if (instrument != null) sb.Append(" With:" + instrument);
			sb.Append("]");
			return sb.ToString();
		}
	}
}
