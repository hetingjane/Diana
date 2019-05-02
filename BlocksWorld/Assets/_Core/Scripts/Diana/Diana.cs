using System;
using System.Collections;
using UnityEngine;

public class Diana : MonoBehaviour {

    private Animator animator;
    private Controller controller;
    private Table table;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
        controller = ControllerLoader.LocateInScene();
        table = FindObjectOfType<Table>();
        SubscribeEvents();

        //GrabBehaviour.leftHandJoint = transform.Find("M3DFemale/hip/abdomenLower/abdomenUpper/chestLower/chestUpper/lCollar/lShldrBend/lShldrTwist/lForearmBend/lForearmTwist/lHand");
        //GrabBehaviour.rightHandJoint = transform.Find("M3DFemale/hip/abdomenLower/abdomenUpper/chestLower/chestUpper/rCollar/rShldrBend/rShldrTwist/rForearmBend/rForearmTwist/rHand");
    }
	
	// Update is called once per frame
	void Update () {
        if (GrabBehaviour.target != null)
        {
            Vector2 pos = table.ToTableCoordinates(new Vector2(GrabBehaviour.target.transform.position.x, GrabBehaviour.target.transform.position.z));
            animator.SetFloat("x", pos.x);
            animator.SetFloat("y", pos.y);
        }
	}

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnSubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (controller != null)
        {
            controller.Grabbed += OnGrab;
            controller.Pointed += OnPointed;
            controller.AskedToIgnore += OnAskedToIgnore;
            controller.Waved += OnWaved;
        }
    }

    private void UnSubscribeEvents()
    {
        if (controller != null)
        {
            controller.Grabbed -= OnGrab;
            controller.Pointed -= OnPointed;
            controller.AskedToIgnore -= OnAskedToIgnore;
            controller.Waved -= OnWaved;
        }
    }

    private void OnPointed(object sender, Controller.PointedEventArgs e)
    {
        if (animator != null)
        {
            if (!animator.GetBool("hasReference"))
            {
                if (table != null)
                {
                    var blocks = table.BlocksNear(e.Position, true);
                    if (blocks.Count > 0)
                    {
                        GameObject handle = Table.GetBlockHandle(blocks[0]);
                        GrabBehaviour.target = handle;
                        Vector2 pos = table.ToTableCoordinates(new Vector2(handle.transform.position.x, handle.transform.position.z));
                        animator.SetFloat("x", pos.x);
                        animator.SetFloat("y", pos.y);
                        animator.SetBool("hasReference", true);
                    }
                }
            }
            else
            {
                var slideAnim = SlideBlock(e.Position);
                StartCoroutine(slideAnim);
            }
        }   
    }

    private IEnumerator SlideBlock(Vector2 destination)
    {
        Vector3 destination3d = new Vector3 { x = destination.x, y = GrabBehaviour.target.transform.position.y, z = destination.y };
        if (GrabBehaviour.target != null)
        {
            Vector3 direction = (destination3d - GrabBehaviour.target.transform.position).normalized;
            while ((GrabBehaviour.target.transform.position - destination3d).sqrMagnitude > .002f)
            {
                GrabBehaviour.target.transform.parent.gameObject.transform.Translate(direction * Time.deltaTime);
                yield return null;
            }
        }
    }

    private void OnGrab(object sender, EventArgs e)
    {
        if (animator != null)
        {
            animator.SetBool("grab", !animator.GetBool("grab"));
        }
    }
    

    private void OnAskedToIgnore(object sender, EventArgs e)
    {
        if (animator != null)
        {
            animator.SetTrigger("ignore");
        }
    }

    private void OnWaved(object sender, EventArgs e)
    {
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            animator.SetBool("wave", true);
        }
    }
}
