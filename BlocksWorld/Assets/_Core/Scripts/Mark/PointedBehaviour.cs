using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointedBehaviour : MarkBehaviour {
    
    public Color color = Color.green;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (mark != null)
        {
            Projector projector = mark.GetComponent<Projector>();
            if (projector != null)
                projector.material.color = color;
            TranslateMarkTo(Position);
        }
    }
}
