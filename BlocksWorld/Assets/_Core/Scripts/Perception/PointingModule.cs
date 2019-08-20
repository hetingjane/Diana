﻿/*
This module computes pointing position based on joint locations converts to pixel space and writes location to black bloard.

Writes:		user:pointpos:right = Vector3
            user:pointpos:left = Vector3
            user:pointpos:right:valid = boolean
            user:pointpos:left:valid = boolean

Reads:      user:joint:HandTipLeft
            user:joint:ShoulderLeft
            user:joint:HandTipRight
            user:joint:ShoulderRight

We will further use this module when we can recognize gestures again.  
*/

using System.Collections;
using System.Collections.Generic;
using System;
using MathNet.Numerics;
using UnityEngine;

public class PointingModule : ModuleBase
{

    string handTipLeftKey = "user:joint:HandTipLeft";
    string shoulderLeftKey = "user:joint:ShoulderLeft";
    string handTipRightKey = "user:joint:HandTipRight";
    string shoulderRightKey = "user:joint:ShoulderRight";

    private bool leftPoint = false;
    private bool rightPoint = false;
    private bool filterInitialized = false;
    private bool leftValid = true;
    private bool rightValid = true;
    public bool calibrationMode = false;

    public int windowSize = 10;

    private float leftDx, leftDy, rightDx, rightDy, leftSx, leftSy, rightSx, rightSy;

    private Vector3 pixSpaceTopRight = new Vector3(Screen.width, Screen.height, 1),
                    pixSpaceBotLeft = new Vector3(0, 0, 1);

    public Vector3 rHKinSpaceTopRight = new Vector3(0.4f, -0.6f, 1),
                   rHKinSpaceBotLeft = new Vector3(-0.5f, -0.9f, 1),
                   lHKinSpaceTopRight = new Vector3(0.4f, -0.6f, 1),
                   lHKinSpaceBotLeft = new Vector3(-0.5f, -0.9f, 1);

    List<Vector3> handTipLeftList = new List<Vector3>();
    List<Vector3> shoulderLeftList = new List<Vector3>();
    List<Vector3> handTipRightList = new List<Vector3>();
    List<Vector3> shoulderRightList = new List<Vector3>();
    List<Vector3> rightPointList = new List<Vector3>();
    List<Vector3> leftPointList = new List<Vector3>();


    protected override void Start()
    {
        base.Start();

        SetUpTransformations();

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
            handTipLeftList.Insert(0, DataStore.GetVector3Value(handTipLeftKey));
            shoulderLeftList.Insert(0,DataStore.GetVector3Value(shoulderLeftKey));

            if (handTipLeftList.Count > windowSize) handTipLeftList.RemoveAt(windowSize);
            if (shoulderLeftList.Count > windowSize) shoulderLeftList.RemoveAt(windowSize);

            avgHandTipLeft = AverageJoint(handTipLeftList);
            avgShoulderLeft = AverageJoint(shoulderLeftList);

            leftPointPos = CalcCoordinates(avgHandTipLeft, avgShoulderLeft);

            if (leftPointPos.x == -float.MaxValue)
                leftValid = false;
            else
                leftValid = true;

            if (calibrationMode)
            {
                DataStore.SetValue("user:pointPos:left", new DataStore.Vector3Value(leftPointPos), this, leftPointPos.ToString());
            }
            else
            {
                leftPointPos = ConvertToPixelSpace(leftPointPos, leftDx, leftDy, leftSx, leftSy);

                if (leftValid) leftPointList.Insert(0, leftPointPos);

                if (leftPointList.Count > windowSize) leftPointList.RemoveAt(windowSize);

                SmoothJoint(leftPointList);

                if (CheckValidRange(leftPointList[0]))
                {
                    DataStore.SetValue("user:pointPos:left", new DataStore.Vector3Value(leftPointList[0]), this, leftPointList[0].ToString());
                    DataStore.SetValue("user:pointPos:left:valid", new DataStore.BoolValue(true), this, leftPointList[0].ToString() + " is valid");
                }
                else
                {
                    DataStore.SetValue("user:pointPos:left", new DataStore.Vector3Value(leftPointList[0]), this, leftPointList[0].ToString());
                    DataStore.SetValue("user:pointPos:left:valid", new DataStore.BoolValue(false), this, leftPointList[0].ToString() + " is valid");
                }
            }
        }

        if (rightPoint)
        {
            handTipRightList.Insert(0, DataStore.GetVector3Value(handTipRightKey));
            shoulderRightList.Insert(0, DataStore.GetVector3Value(shoulderRightKey));

            if (handTipRightList.Count > windowSize) handTipRightList.RemoveAt(windowSize);
            if (shoulderRightList.Count > windowSize) shoulderRightList.RemoveAt(windowSize);

            avgHandTipRight = AverageJoint(handTipRightList);
            avgShoulderRight = AverageJoint(shoulderRightList);

            rightPointPos = CalcCoordinates(avgHandTipRight, avgShoulderRight);

            if (rightPointPos.x == -float.MaxValue)
                rightValid = false;
            else
                rightValid = true;

            if (calibrationMode)
            {
                DataStore.SetValue("user:pointPos:right", new DataStore.Vector3Value(rightPointPos), this, rightPointPos[0].ToString());
            }
            else
            {
                rightPointPos = ConvertToPixelSpace(rightPointPos, rightDx, rightDy, rightSx, rightSy);

                if(rightValid) rightPointList.Insert(0, rightPointPos);

                if (rightPointList.Count > windowSize) rightPointList.RemoveAt(windowSize);

                SmoothJoint(rightPointList);

                if (CheckValidRange(rightPointList[0]) || calibrationMode)
                {
                    DataStore.SetValue("user:pointPos:right", new DataStore.Vector3Value(rightPointList[0]), this, rightPointList[0].ToString());
                    DataStore.SetValue("user:pointPos:right:valid", new DataStore.BoolValue(true), this, rightPointList[0].ToString() + " is valid");
                }
                else
                {
                    DataStore.SetValue("user:pointPos:right", new DataStore.Vector3Value(rightPointList[0]), this, rightPointList[0].ToString());
                    DataStore.SetValue("user:pointPos:right:valid", new DataStore.BoolValue(false), this, rightPointList[0].ToString() + " is not valid");
                }
            }
        }
    }

 
    /* 
     * Smooth the joint using an implementation of the Savitsky Golay filter.
     * I updated this implementation a bit so that the smoothed point is in the center
     * x = [joint[len -1].x, joint[len -2].x, ..., joint[0].x, joint[1].x, ..., joint[len-1].x]
     * The format is similar for y and z
     * count = [-(len-1), -(len-2), ..., 0, ..., len-2, len-1]
     * The count variable is the independent variable in our case
     */
    private void SmoothJoint(List<Vector3> joint)
    {
        if (joint.Count == windowSize)
        {
            double[] x = new double[joint.Count*2 -1], y = new double[joint.Count *2 - 1], z = new double[joint.Count*2 -1], count = new double[joint.Count*2-1];

            for (int i = 0; i < joint.Count; i++)
            {
                x[i] = joint[joint.Count - i - 1].x;
                y[i] = joint[joint.Count - i - 1].y;
                //z[i] = joint[joint.Count - i - 1].z;
                count[i] = i - (joint.Count - 1);
            }
            for (int i = 1; i < joint.Count; i++)
            {
                x[i + joint.Count - 1] = joint[i].x;
                y[i + joint.Count - 1] = joint[i].y;
                //z[i + joint.Count - 1] = joint[i].z;
                count[i + joint.Count - 1] = i;
            }

            joint[0] = new Vector3((float)SavGolay(count, x), (float)SavGolay(count, y), 0.0f);// (float)SavGolay(count, z));
        }
    }


    // Implmenentation of the Savitsky Golay Filter.  I am not sure that this implementation follows the exact defitinition. 
    // Basically we fit a line to the previous points (I found that a line worked the best) and adjust the point 
    // based on the point where it existed on the line.
    private double SavGolay(double[] x, double[] y, int polyOrder = 1)
    {
        double[] fit = Fit.Polynomial(x, y, polyOrder);
        double retVal = 0.0;


        for (int i = 0; i <= polyOrder; i++)
        {
            retVal += fit[i] * Math.Pow(x[x.Length/2], i); 
        }

        return retVal;
    }

    // average the joint x, y, and z position
    // provides a little smoothing over a window
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
    private Vector3 ConvertToPixelSpace(Vector3 point, float dx, float dy, float sx, float sy)
    {
        float x, y;

        x = point.x * sx + dx;
        y = point.y * sy + dy;

        return new Vector3(x, y, 0.0f);
    }

    /*
     * This function sets up translation and scaling paramters for both the left and the right hand transformations
     * I was originally recalculating this information on every frame, but the repeated computation was not necessary.
     */
    private void SetUpTransformations()
    {
        leftDx = (pixSpaceTopRight.x * lHKinSpaceBotLeft.x - pixSpaceBotLeft.x * lHKinSpaceTopRight.x) / (lHKinSpaceBotLeft.x - lHKinSpaceTopRight.x);
        leftDy = (pixSpaceTopRight.y * lHKinSpaceBotLeft.y - pixSpaceBotLeft.y * lHKinSpaceTopRight.y) / (lHKinSpaceBotLeft.y - lHKinSpaceTopRight.y);
        rightDx = (pixSpaceTopRight.x * rHKinSpaceBotLeft.x - pixSpaceBotLeft.x * rHKinSpaceTopRight.x) / (rHKinSpaceBotLeft.x - rHKinSpaceTopRight.x);
        rightDy = (pixSpaceTopRight.y * rHKinSpaceBotLeft.y - pixSpaceBotLeft.y * rHKinSpaceTopRight.y) / (rHKinSpaceBotLeft.y - rHKinSpaceTopRight.y);

        leftSx = (pixSpaceBotLeft.x - leftDx) / lHKinSpaceBotLeft.x;
        leftSy = (pixSpaceBotLeft.y - leftDy) / lHKinSpaceBotLeft.y;
        rightSx = (pixSpaceBotLeft.x - rightDx) / rHKinSpaceBotLeft.x;
        rightSy = (pixSpaceBotLeft.y - rightDy) / rHKinSpaceBotLeft.y;
    }
    

    // this function checks if the point position is in valid pixel space
    private bool CheckValidRange(Vector3 pointPos)
    {
        if (pointPos.x >= pixSpaceBotLeft.x && pointPos.x <= pixSpaceTopRight.x)
            if (pointPos.y >= pixSpaceBotLeft.y && pointPos.y <= pixSpaceTopRight.y)
                return true;
        return false;
    }


}
