/*
This module uses the mouse pointer to let the user point at a location in the scene,
and then sets the blackboard values just as if the user were pointing with his hand.

Reads:		(nothing)
Writes:		user:isPointing (BoolValue)
			user:pointPos (Vector3Value)
			user:pointValid (BoolValue)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MousePointModule : ModuleBase
{
	public float maxDistance = 10;
	public LayerMask layerMask = -1;
	
	protected void Update() {
		Vector3 screenPos = Input.mousePosition;
		Ray ray = Camera.main.ScreenPointToRay(screenPos);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, maxDistance, layerMask)) {
			if (hit.collider.name.EndsWith("Backstop")) {
				// We have an invisible pointer backstop wall behind Diana.  This
				// is to give the user some feedback when they're pointing to high.
				// When the ray hits this, we want to show a "no bueno" indicator
				// and report the point as invalid.
				var comment = "hit pointer backstop";
				SetValue("user:isPointing", true, comment);
				SetValue("user:pointPos", hit.point, comment);
				SetValue("user:pointValid", false, comment);
			} else {
				var comment =  "ray hit " + hit.collider.name;
				SetValue("user:isPointing", true, comment);
				SetValue("user:pointPos", hit.point, comment);
				SetValue("user:pointValid", true, comment);
			}
		} else {
			SetValue("user:isPointing", false, "no ray hit");
		}
	}
}
