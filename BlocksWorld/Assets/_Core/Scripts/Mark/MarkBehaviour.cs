using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MarkBehaviour : StateMachineBehaviour {
    public GameObject mark;
    public Vector2 Position;

    public void RotateMark(float angle)
    {
        if (mark != null)
            mark.transform.Rotate(0, 0, angle * Time.deltaTime);
    }

    public void TranslateMarkTo(Vector2 position)
    {
        if (mark != null)
        {
            // Do not move along the y axis
            mark.transform.position = new Vector3(position.x, mark.transform.position.y, position.y);
        }
    }
}
