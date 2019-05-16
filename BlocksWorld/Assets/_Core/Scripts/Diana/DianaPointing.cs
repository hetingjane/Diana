using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DianaPointing : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        Debug.Assert(animator != null);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            animator.SetBool("grab", !animator.GetBool("grab"));
        }

        if (DataStore.GetStringValue("me:intent:action", "") == "point")
        {
            Vector3 target = DataStore.GetVector3Value("me:intent:target");
            target += Vector3.up * 0.10f;

            animator.SetFloat("x", target.x);
            animator.SetFloat("z", target.z);
        }
    }
}
