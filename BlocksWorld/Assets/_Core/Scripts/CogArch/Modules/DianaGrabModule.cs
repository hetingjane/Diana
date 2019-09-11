using UnityEngine;

/// <summary>
/// 
/// </summary>
[RequireComponent(typeof(Animator))]
public class DianaGrabModule : ModuleBase
{
    public enum GrabState
    {
        Relaxed,
        Reaching,
        Raising,
        Moving,
        Lowering,
        Unreaching
    }

    private Animator animator;

    public float radius = 0.1f;

    private GameObject block;
    private Vector3 finalPosition;
    private GrabState state;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);

        block = null;
        finalPosition = default;
        state = GrabState.Relaxed;

        DataStore.Subscribe("me:intent:action", NoteUserGesture);
    }

    private void NoteUserGesture(string key, DataStore.IValue value)
    {
        bool doGrab = DataStore.GetStringValue("me:intent:action", "") == "grab";

        if (doGrab && state == GrabState.Relaxed)
        {
            if (block == null)
            {
                Vector3 userPointPos = DataStore.GetVector3Value("user:pointPos");
                // Find a block that we can grab
                Collider[] colliders = Physics.OverlapSphere(userPointPos, radius, LayerMask.GetMask("Blocks"));

                if (colliders != null && colliders.Length > 0)
                {
                    float minDistance = 1000000f;
                    foreach(Collider c in colliders)
                    {
                        float distance = Vector3.Distance(c.transform.position, userPointPos);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            block = c.gameObject;
                        }
                    }
                }
            }
            else if (finalPosition == default)
            {
                Vector3 userPointPos = DataStore.GetVector3Value("user:pointPos");
                finalPosition = userPointPos;
                state = GrabState.Reaching;
            }
        }
    }

    private Vector3 velocity = Vector3.zero;
    public float offset = 0.05f;
    public float raiseLowerTime = .4f;
    private float moveTime = -1f;
    [Range(0.1f, 1f)]
    public float moveTimeMultiplier = 0.5f; 

    private void Update()
    {
        Debug.Log($"Time: {animator.GetCurrentAnimatorStateInfo(0).normalizedTime}, State={state}");
        switch (state)
        {
            case GrabState.Relaxed:
                break;
            case GrabState.Reaching:
                if (!animator.GetBool("grab"))
                {
                    Vector3 targetPos = block.transform.position;
                    animator.SetFloat("x", targetPos.x);
                    animator.SetFloat("y", targetPos.y + offset);
                    animator.SetFloat("z", targetPos.z);

                    animator.SetBool("grab", true);
                    block.GetComponent<Rigidbody>().isKinematic = true;
                }
                else
                {
                    var curStateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (curStateInfo.IsName("Grab") && curStateInfo.normalizedTime > 1f)
                    {
                        state = GrabState.Raising;
                    }
                }
                break;
            case GrabState.Raising:
                Vector3 liftedPos = block.transform.position;
                liftedPos.y = 1.4f;
                block.transform.position = Vector3.SmoothDamp(block.transform.position, liftedPos, ref velocity, raiseLowerTime);
                if (Vector3.Distance(block.transform.position, liftedPos) < 0.05f)
                {
                    state = GrabState.Moving;
                }
                else
                {
                    animator.SetFloat("y", block.transform.position.y + offset);
                }
                break;
            case GrabState.Moving:
                Vector3 finalLiftedPos = finalPosition;
                finalLiftedPos.y = 1.4f;
                if (moveTime <= 0f)
                {
                    moveTime = Vector3.Distance(block.transform.position, finalLiftedPos) * moveTimeMultiplier;
                }
                block.transform.position = Vector3.SmoothDamp(block.transform.position, finalLiftedPos, ref velocity, 1f);
                if (Vector3.Distance(block.transform.position, finalLiftedPos) < 0.05f)
                {
                    moveTime = -1f;
                    state = GrabState.Lowering;
                }
                else
                {
                    animator.SetFloat("x", block.transform.position.x);
                    animator.SetFloat("y", block.transform.position.y + offset);
                    animator.SetFloat("z", block.transform.position.z);
                }
                break;
            case GrabState.Lowering:
                block.transform.position = Vector3.SmoothDamp(block.transform.position, finalPosition, ref velocity, raiseLowerTime);
                if (Vector3.Distance(block.transform.position, finalPosition) < 0.05f)
                {
                    state = GrabState.Unreaching;
                }
                else
                {
                    animator.SetFloat("y", block.transform.position.y + offset);
                }
                break;
            case GrabState.Unreaching:
                if (animator.GetBool("grab"))
                {
                    animator.SetBool("grab", false);
                }
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Ungrab") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > .8f)
                {
                    block.GetComponent<Rigidbody>().isKinematic = false;
                    block = null;
                    finalPosition = default;
                    velocity = Vector3.zero;
                    state = GrabState.Relaxed;
                }
                break;
        }
    }
}
