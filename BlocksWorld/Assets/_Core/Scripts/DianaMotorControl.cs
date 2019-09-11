using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DianaMotorControl : MonoBehaviour
{
    private Animator animator;

    private GrabPlaceModule grabPlaceModule;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);
        grabPlaceModule = FindObjectOfType<GrabPlaceModule>();
        Debug.Assert(grabPlaceModule != null);
        if (grabPlaceModule != null)
        {
            grabPlaceModule.StateChanged += OnStateChanged;
        }
    }

    private void OnStateChanged(object sender, GrabPlaceModule.StateChangedEventArgs e)
    {
        var previousState = e.PreviousState;
        var currentState = e.CurrentState;

        if (previousState == GrabPlaceModule.State.Idle && currentState == GrabPlaceModule.State.Reaching)
        {
            animator.SetBool("grab", true);
        }
        else if (previousState == GrabPlaceModule.State.Releasing && currentState == GrabPlaceModule.State.Unreaching)
        {
            animator.SetBool("grab", false);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (animator.GetBool("grab"))
        {
            Vector3 target = DataStore.GetVector3Value("me:intent:handPosR");
            animator.SetFloat("x", target.x);
            animator.SetFloat("y", target.y);
            animator.SetFloat("z", target.z);
        }
    }

    private void LateUpdate()
    {
        Vector3 actualPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        DataStore.SetValue("me:actual:handPosR", new DataStore.Vector3Value(actualPos), null, "DianaMotorControl");

        Debug.Log($"{DataStore.GetVector3Value("me:intent:handPosR")}, {DataStore.GetVector3Value("me:actual:handPosR")}");
    }
}
