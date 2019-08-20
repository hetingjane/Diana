/*
This module uses the body pointer to let the user point at a location in the scene,
and then sets the blackboard values just as if the user were pointing with his hand.

Reads:		(nothing)
Writes:		user:isPointing (BoolValue)
			user:pointPos (Vector3Value)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPointModule : ModuleBase
{
    bool pointing = false;
    public float maxDistance = 10;
    public LayerMask layerMask = -1;


    protected override void Start()
    {
        base.Start();
        DataStore.Subscribe("user:pointPos:right", NoteScreenOrDeskMode);
        DataStore.Subscribe("user:pointPos:left", NoteScreenOrDeskMode);

        DataStore.Subscribe("user:pointPos:right:valid", NoteScreenOrDeskMode);
        DataStore.Subscribe("user:pointPos:left:valid", NoteScreenOrDeskMode);
    }

    void NoteScreenOrDeskMode(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.
    }

    protected void Update()
    {
        if (DataStore.HasValue("user:pointPos:right") || DataStore.HasValue("user:pointPos:left")) pointing = true;
        {
            Vector3 screenPos;

            if (DataStore.GetBoolValue("user:pointPos:right:valid"))
                screenPos = DataStore.GetVector3Value("user:pointPos:right");
            else if (DataStore.GetBoolValue("user:pointPos:left:valid"))
                screenPos = DataStore.GetVector3Value("user:pointPos:left");
            else
                screenPos = new Vector3(0, 0, 0);

            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
            {
                if (hit.collider.name.EndsWith("Backstop"))
                {
                    // We have an invisible pointer backstop wall behind Diana.  This
                    // is to give the user some feedback when they're pointing to high.
                    // When the ray hits this, we want to show a "no bueno" indicator
                    // and report the point as invalid.
                    var comment = "hit pointer backstop";
                    SetValue("user:isPointing", true, comment);
                    SetValue("user:pointPos", hit.point, comment);
                    SetValue("user:pointValid", false, comment);
                }
                else
                {
                    var comment = "ray hit " + hit.collider.name;
                    SetValue("user:isPointing", true, comment);
                    SetValue("user:pointPos", hit.point, comment);
                    SetValue("user:pointValid", true, comment);
                }
            }
            else
            {
                SetValue("user:isPointing", false, "no ray hit");
            }
        }

    }
}