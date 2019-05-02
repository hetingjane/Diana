using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PointState
{
    NO_POINTING = 0,
    POINTING = 1,
    POINTED = 2
}
[Obsolete("Don't use Highlighter. Use Mark instead")]
public class Highlighter : MonoBehaviour {

    // Should make these private in production
    public float x = 0f, z = 0f;

    // Configuration
    public GameObject table;

    private PointState pointState = PointState.NO_POINTING;
    
    [Range(0.0f, 0.4f)]
    public float edgeMargin = 0.1f; // edge margin for the table

    private readonly Vector2 farLeft = new Vector2(-.5f, -.25f);
    private readonly Vector2 nearLeft = new Vector2(-.75f, -.65f);
    private readonly Vector2 nearRight = new Vector2(.35f, -.45f);
    private readonly Vector2 farRight = new Vector2(.25f, -.35f);

    //private static Vector2 rhFarLeft = new Vector2(-.25f, -.3f);
    //private static Vector2 rhNearLeft = new Vector2(-.35f, -.4f);
    //private static Vector2 rhNearRight = new Vector2(.6f, -.5f);
    //private static Vector2 rhFarRight = new Vector2(.35f, -.25f);

    private PointingBehaviour pointingBehaviour = null;
    private Animator animator = null;
    private Vector4 tableBounds;
    private Mapper mapper = null;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start ()
    {
        // Get table bounds on runtime (accounting for margin as well)
        tableBounds = EstimateTableBounds(table, edgeMargin);
        // Setup Mapper
        Quadrilateral source = new Quadrilateral(farLeft, nearLeft, nearRight, farRight);
        Quadrilateral destination = new Quadrilateral(
            new Vector2(tableBounds.x, tableBounds.z),
            new Vector2(tableBounds.x, tableBounds.w),
            new Vector2(tableBounds.y, tableBounds.w),
            new Vector2(tableBounds.y, tableBounds.z)
        );

        mapper = new Mapper(source, destination);
        
        if (animator != null)
        {
            foreach (MarkBehaviour smb in animator.GetBehaviours<MarkBehaviour>())
            {
                smb.mark = this.gameObject;
                if (pointingBehaviour == null && smb.GetType() == typeof(PointingBehaviour))
                    pointingBehaviour = (PointingBehaviour)smb;
            }
        }
	}

    private static Vector4 EstimateTableBounds(GameObject table, float margin)
    {
        // Returns as xMin, xMax, zMin, zMax
        if (table != null)
        {
            Collider collider = table.GetComponent<Collider>();
            if (collider != null && collider.enabled)
            {
                Vector3 ep1 = collider.bounds.min;
                Vector3 ep2 = collider.bounds.max;
                Vector4 bounds = Vector4.zero;
                bounds.x = Mathf.Min(ep1.x, ep2.x);
                bounds.y = Mathf.Max(ep1.x, ep2.x);

                float xMargin = (bounds.y - bounds.x) * margin;
                bounds.x += xMargin;
                bounds.y -= xMargin;

                bounds.z = Mathf.Min(ep1.z, ep2.z);
                bounds.w = Mathf.Max(ep1.z, ep2.z);

                float zMargin = (bounds.y - bounds.x) * margin;
                bounds.z += zMargin;
                bounds.w -= zMargin;

                return bounds;
            }
        }
        return new Vector4(float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity);
    }

    private Vector2 ThresholdPosition(float x, float z)
    {
        Vector2 thresholded = new Vector2(x, z);
        if (x < tableBounds.x)
            thresholded.x = tableBounds.x;
        else if (x > tableBounds.y)
            thresholded.x = tableBounds.y;

        if (z < tableBounds.z)
            thresholded.y = tableBounds.z;
        else if (z > tableBounds.w)
            thresholded.y = tableBounds.w;

        return thresholded;

    }
    

    private void Update()
    {

        if (pointingBehaviour != null)
            pointingBehaviour.Position = ThresholdPosition(x, z);
    }
}
