using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointingBehaviour : MarkBehaviour {
    public Color color = Color.gray;
    public float rotationSpeed = 90f;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	    if (mark != null)
        {
            Projector projector = mark.GetComponent<Projector>();
            if (projector != null)
                projector.material.color = color;
        }
	}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (mark != null)
        {
            RotateMark(rotationSpeed);
            TranslateMarkTo(Position);
        }
    }
}
