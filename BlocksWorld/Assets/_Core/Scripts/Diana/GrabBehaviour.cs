using UnityEngine;



public class GrabBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }

    // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
    override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateIK(animator, stateInfo, layerIndex);

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, animator.GetFloat("PositionWeight"));
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, animator.GetFloat("RotationWeight"));

        Vector3 target = new Vector3(animator.GetFloat("x"), animator.GetFloat("y"), animator.GetFloat("z"));
        animator.SetIKPosition(AvatarIKGoal.RightHand, target);
        animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.identity);
    }
}
