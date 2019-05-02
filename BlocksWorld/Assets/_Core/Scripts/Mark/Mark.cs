using System;
using UnityEngine;

public class Mark : MonoBehaviour {
    #region Configuration
    [Range(0.0f, 0.4f)]
    public float tableMargin = 0.05f; // edge margin for the table
    public GameObject table;
    #endregion

    private Animator animator;
    private Controller controller;

    #region MonoBehaviour
    // Use this for initialization
    void Start () {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Set the mark object reference in all the behaviours of the underlying state machine
            foreach (MarkBehaviour mb in animator.GetBehaviours<MarkBehaviour>())
            {
                mb.mark = this.gameObject;
            }
        }

        controller = ControllerLoader.LocateInScene();


        SubscribeToEvents();
	}

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnSubscribeToEvents();
    }
    #endregion

    #region Subscribe/UnSubscribe
    private void SubscribeToEvents()
    {
        if (controller != null)
        {
            controller.Pointing += OnPointing;
            controller.Pointed += OnPointed;
            controller.StoppedPointing += OnStoppedPointing;
        }
    }

    private void UnSubscribeToEvents()
    {
        if (controller != null)
        {
            controller.Pointing -= OnPointing;
            controller.Pointed -= OnPointed;
            controller.StoppedPointing -= OnStoppedPointing;
        }
    }
    #endregion

    #region Event Handlers
    private void OnStoppedPointing(object sender, EventArgs e)
    {
        SetAnimatorPointingState(0, Vector2.zero);
    }

    private void OnPointing(object sender, Controller.PointingEventArgs e)
    {
        SetAnimatorPointingState(1, e.Position);
    }

    private void OnPointed(object sender, Controller.PointedEventArgs e)
    {
        SetAnimatorPointingState(2, e.Position);
    }
    #endregion

    public Vector3 Position
    {
        get
        {
            if (animator != null)
            {
                Vector2 pos = animator.GetBehaviour<MarkBehaviour>().Position;
                return new Vector3(pos.x, 0, pos.y);
            }
            return Vector3.zero;
        }
    }

    private void SetAnimatorPointingState(int state, Vector2 position)
    {
        if (animator != null)
        {
            animator.SetInteger("pointingState", state);
            foreach (MarkBehaviour mb in animator.GetBehaviours<MarkBehaviour>())
                mb.Position = position;
        }
    }
}
