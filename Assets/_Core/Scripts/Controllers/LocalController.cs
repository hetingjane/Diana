using System;
using UnityEngine;

public class LocalController : Controller
{
    #region Configuration
    public KeyCode NV_POINT = KeyCode.Mouse0;
    public KeyCode NV_UNDO_POINT = KeyCode.Mouse1;
    public KeyCode NV_GRAB = KeyCode.G;
    public KeyCode NV_IGNORE = KeyCode.I;
    public KeyCode NV_WAVE = KeyCode.W;
    #endregion

    // To allow repeated press of NV_POINT to transition betwen the three states in forward direction
    private enum PointState
    {
        NOT_POINTING,
        POINTING,
        POINTED
    }

    private PointState pointState = PointState.NOT_POINTING;

    #region MonoBehaviour
    void Update () {
        HandlePointingInput();
        HandleGrabInput();
        HandleIgnoreInput();
        HandleWaveInput();
    }

    private void HandleWaveInput()
    {
        if (Input.GetKeyDown(NV_WAVE))
        {
            OnWaved(EventArgs.Empty);
        }
    }
    
    private void HandleGrabInput()
    {
        if (Input.GetKeyDown(NV_GRAB))
            OnGrabbed(EventArgs.Empty);
    }

    private void HandleIgnoreInput()
    {
        if (Input.GetKeyDown(NV_IGNORE))
            OnAskedToIgnore(EventArgs.Empty);
    }

    private void HandlePointingInput()
    {
        if (Input.GetKeyDown(NV_POINT))
        {
            if (pointState == PointState.NOT_POINTING)
            {
                pointState = PointState.POINTING;
            }
            else if (pointState == PointState.POINTING)
            {
                pointState = PointState.POINTED;
                Vector2 xz;
                if(GetMouseCoordinates(out xz))
                    OnPointed(new PointedEventArgs(xz));
            }
        }
        if (Input.GetKeyDown(NV_UNDO_POINT))
        {
            if (pointState != PointState.NOT_POINTING)
            {
                pointState = PointState.NOT_POINTING;
                OnStoppedPointing(EventArgs.Empty);
            }
        }

        if (pointState == PointState.POINTING)
        {
            Vector2 xz;
            if (GetMouseCoordinates(out xz))
                OnPointing(new PointingEventArgs(xz));
        }
    }
    #endregion

    #region Connect to base class events
    protected override void OnPointed(PointedEventArgs e)
    {
        base.OnPointed(e);
    }

    protected override void OnPointing(PointingEventArgs e)
    {
        base.OnPointing(e);
    }

    protected override void OnStoppedPointing(EventArgs e)
    {
        base.OnStoppedPointing(e);
    }

    protected override void OnGrabbed(EventArgs e)
    {
        base.OnGrabbed(e);
    }

    protected override void OnAskedToIgnore(EventArgs e)
    {
        base.OnAskedToIgnore(e);
    }
    #endregion

    private bool GetMouseCoordinates(out Vector2 xz)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool success = Physics.Raycast(ray, out hit, 100);
        xz = success ? new Vector2(hit.point.x, hit.point.z) : Vector2.positiveInfinity;
        return success;
    }
}
