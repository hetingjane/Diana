using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Hand
{
    LEFT,
    RIGHT
}


public class GrabBehaviour : StateMachineBehaviour {
    public Hand hand;
   

    [HideInInspector]
    public static GameObject target;

    // Cache parameter ids for later use
    private readonly int PositionWeightId = Animator.StringToHash("PositionWeight");
    private readonly int RotationWeightId = Animator.StringToHash("RotationWeight");
    private readonly int LookAtWeightId = Animator.StringToHash("LookAtWeight");
    private bool grabbed = false;
    
    // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
    override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateIK(animator, stateInfo, layerIndex);

        animator.SetLookAtWeight(animator.GetFloat(LookAtWeightId));
        animator.SetIKPositionWeight(hand == Hand.LEFT ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand, animator.GetFloat(PositionWeightId));
        animator.SetIKRotationWeight(hand == Hand.LEFT ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand, animator.GetFloat(RotationWeightId));

        if (target != null)
        {
            animator.SetLookAtPosition(target.transform.position);
            animator.SetIKPosition(hand == Hand.LEFT ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand, target.transform.position);
            animator.SetIKRotation(hand == Hand.LEFT ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand, target.transform.rotation);
        }
    }

    //public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    base.OnStateUpdate(animator, stateInfo, layerIndex);
    //    if (target != null)
    //    {
    //        animator.SetFloat("x", target.transform.position.x);
    //        animator.SetFloat("y", target.transform.position.z);
    //    }
    //}

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        grabbed = false;
    }
}
