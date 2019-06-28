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
        DataStore.Subscribe("user:pointpos:right", NoteScreenOrDeskMode);
    }

    void NoteScreenOrDeskMode(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.
    }

    protected void Update()
    {
        if (DataStore.HasValue("user:pointpos:right")) pointing = true;
        {
            Vector3 screenPos = DataStore.GetVector3Value("user:pointpos:right");
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
            {
                var comment = "ray hit " + hit.collider.name;
                SetValue("user:isPointing", true, comment);
                SetValue("user:pointPos", hit.point, comment);
            }
            else
            {
                SetValue("user:isPointing", false, "no ray hit");
            }
        }

    }
}