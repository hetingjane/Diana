﻿using System;
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
		Close
	}
	
	
	public class ActionSpec {
		public ObjSpec subject;		// (may often be named)
		public Action action;
		public ObjSpec directObject;
		public LocationSpec location;
		public ObjSpec instrument;
		
		public override string ToString() {
			var sb = new System.Text.StringBuilder();
			sb.Append("[");
			if (subject != null) sb.Append("Subj:" + subject + " ");
			sb.Append("Act:" + action.ToString());
			if (directObject != null) sb.Append(" Obj:" + directObject);
			if (location != null) sb.Append(" Loc:" + location);
			if (instrument != null) sb.Append(" With:" + instrument);
			sb.Append("]");
			return sb.ToString();
		}
	}
}