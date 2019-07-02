/*
This module computes pointing position based on joint locations converts to pixel space and writes location to black bloard.

Writes:		user:pointpos:right = Vector3
            user:pointpos:left = Vector3

We will further use this module when we can recognize gestures again.  
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointingModule : ModuleBase
{

    string handTipLeftKey = "user:joint:HandTipLeft";
    string shoulderLeftKey = "user:joint:ShoulderLeft";
    string handTipRightKey = "user:joint:HandTipRight";
    string shoulderRightKey = "user:joint:ShoulderRight";

    bool leftPoint = false;
    bool rightPoint = false;
    public bool calibrationMode = false;

    public int windowSize = 10;
    public Vector3 pixSpaceTopRight = new Vector3(1537, 696, 1),
                   pixSpaceBotLeft = new Vector3(0, 0, 1),
                   kinSpaceTopRight = new Vector3(0.4f, -0.6f, 1),
                   kinSpaceBotLeft = new Vector3(-0.5f, -0.9f, 1);

    List<Vector3> handTipLeftVal = new List<Vector3>();
    List<Vector3> shoulderLeftVal = new List<Vector3>();
    List<Vector3> handTipRightVal = new List<Vector3>();
    List<Vector3> shoulderRightVal = new List<Vector3>();

    protected override void Start()
    {
        base.Start();
        DataStore.Subscribe(handTipLeftKey, NoteScreenOrDeskMode);
        DataStore.Subscribe(shoulderLeftKey, NoteScreenOrDeskMode);

        DataStore.Subscribe(handTipRightKey, NoteScreenOrDeskMode);
        DataStore.Subscribe(shoulderRightKey, NoteScreenOrDeskMode);
    }

    void NoteScreenOrDeskMode(string key, DataStore.IValue value)
    {
        // May add some handler here for different settings.  Need to discuss. 
    }
    protected void Update()
    {
        Vector3 avgHandTipLeft, avgShoulderLeft, avgHandTipRight, avgShoulderRight, rightPointPos, leftPointPos;

        if (DataStore.HasValue(handTipLeftKey) && DataStore.HasValue(shoulderLeftKey)) leftPoint = true;
        if (DataStore.HasValue(handTipRightKey) && DataStore.HasValue(shoulderRightKey)) rightPoint = true;

        if (leftPoint)
        {
            handTipLeftVal.Insert(0, DataStore.GetVector3Value(handTipLeftKey));
            shoulderLeftVal.Insert(0,DataStore.GetVector3Value(shoulderLeftKey));

            if (handTipLeftVal.Count > windowSize) handTipLeftVal.RemoveAt(windowSize);
            if (shoulderLeftVal.Count > windowSize) shoulderLeftVal.RemoveAt(windowSize);

            avgHandTipLeft = AverageJoint(handTipLeftVal);
            avgShoulderLeft = AverageJoint(shoulderLeftVal);

            leftPointPos = CalcCoordinates(avgHandTipLeft, avgShoulderLeft);
            
            if (leftPointPos.x != -float.MaxValue)
                DataStore.SetValue("user:pointpos:left", new DataStore.Vector3Value(leftPointPos), this, leftPointPos.ToString());
        }

        if (rightPoint)
        {
            handTipRightVal.Insert(0, DataStore.GetVector3Value(handTipRightKey));
            shoulderRightVal.Insert(0, DataStore.GetVector3Value(shoulderRightKey));

            if (handTipRightVal.Count > windowSize) handTipRightVal.RemoveAt(windowSize);
            if (shoulderRightVal.Count > windowSize) shoulderRightVal.RemoveAt(windowSize);

            avgHandTipRight = AverageJoint(handTipRightVal);
            avgShoulderRight = AverageJoint(shoulderRightVal);

            rightPointPos = CalcCoordinates(avgHandTipRight, avgShoulderRight);

            if(!calibrationMode)
                rightPointPos = ConvertToPixelSpace(rightPointPos, kinSpaceBotLeft, kinSpaceTopRight, pixSpaceBotLeft, pixSpaceTopRight);

            if (rightPointPos.x != -float.MaxValue)
                DataStore.SetValue("user:pointpos:right", new DataStore.Vector3Value(rightPointPos), this, rightPointPos.ToString());
        }
    }

    // smoothing will likely help but will come later
    private void SmoothJoint(List<Vector3> joint)
    {
        if (joint.Count == windowSize)
        {

        }
    }

    private Vector3 AverageJoint(List<Vector3> jointList)
    {
        Vector3 averageJointPos = new Vector3(0.0f, 0.0f, 0.0f);

        foreach(Vector3 joint in jointList)
        {
            averageJointPos += joint;
        }
        return averageJointPos / windowSize;
    }

    /*
     * This method creates a ray with a start point at the shoulder joint in the direction of the handtip 
     * From there we can compute the value "t" which is how far along the ray that we travel when z = 0 
     * 0 = start.z + t*direction.z
     * 
     * Then we simply solve for x and y based on the t that we find
     */
    private Vector3 CalcCoordinates(Vector3 handTip, Vector3 shoulder)
    {
        Vector3 direction = new Vector3(handTip.x - shoulder.x, handTip.y - shoulder.y, handTip.z - shoulder.z);
        float t, x, y;

        t = (0 - shoulder.z) / direction.z;

        if (t < 0.0f) return new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        x = shoulder.x + t * direction.x;
        y = shoulder.y + t * direction.y;

        return new Vector3(x, y, 0);
    }

    /*
     * Here I solve for the scale and translation factors in both the x and y direction in order to perform the transformation
     * Using the two formulas:
     * 
     * [x1']   [sx 0 dx] [x1]
     * [y1'] = [0 sy dy] [y1]
     * [1  ]   [0  0  1] [1 ]
     * 
     * [x2']   [sx 0 dx] [x2]
     * [y2'] = [0 sy dy] [y2]
     * [1  ]   [0  0  1] [1 ]
     * 
     * The input points should be calibrated so that the bottom left and top right corners of the screen solve this transformation 
     * from kinect space to pixel space
     */
    private Vector3 ConvertToPixelSpace(Vector3 point, Vector3 kPoint1, Vector3 kPoint2, Vector3 pPoint1, Vector3 pPoint2)
    {
        float dx, dy, sx, sy, x, y;

        dx = (pPoint2.x * kPoint1.x - pPoint1.x * kPoint2.x) / (kPoint1.x - kPoint2.x);
        dy = (pPoint2.y * kPoint1.y - pPoint1.y * kPoint2.y) / (kPoint1.y - kPoint2.y);

        sx = (pPoint1.x - dx) / kPoint1.x;
        sy = (pPoint1.y - dy) / kPoint1.y;


        x = point.x * sx + dx;
        y = point.y * sy + dy;

        return new Vector3(x, y, 0.0f);
    }



}