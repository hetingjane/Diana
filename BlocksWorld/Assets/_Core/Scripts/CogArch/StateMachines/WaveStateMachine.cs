using System;
using UnityEngine;

public enum WaveState
{
	WaveStart,
	WaveStop
}

public class WaveStateMachine : RuleStateMachine<WaveState>
{
    WaveHelper leftArmHelper = new WaveHelper(1.0, "user:joint:ElbowLeft", "user:joint:WristLeft");
    WaveHelper rightArmHelper = new WaveHelper(1.0, "user:joint:ElbowRight", "user:joint:WristRight");

    private bool isWaving()
    {
        bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");
        return userIsEngaged && (leftArmHelper.IsArmWaving() || rightArmHelper.IsArmWaving());
    }

    public WaveStateMachine()
    {
        SetTransitionRule(WaveState.WaveStop, WaveState.WaveStart, new Rule(() => isWaving()));
        SetTransitionRule(WaveState.WaveStart, WaveState.WaveStop, new Rule(() => !isWaving()));
    }
}

class WaveHelper
{
    private string elbowKey;
    private string wristKey;
    private bool wristWasLeftOfElbow = false;
    private bool wristWasRightOfElbow = false;
    private DateTime leftStart;
    private DateTime rightStart;
    private double secondsThreshold;

    public WaveHelper(double secondsThreshold, string elbowKey, string wristKey)
    {
        this.secondsThreshold = secondsThreshold;
        this.leftStart = DateTime.Now;
        this.rightStart = DateTime.Now;
        this.elbowKey = elbowKey;
        this.wristKey = wristKey;
    }

    public bool IsArmWaving()
    {
        Vector3 wristPos = DataStore.GetVector3Value(wristKey);
        Vector3 elbowPos = DataStore.GetVector3Value(elbowKey);

        bool wristAboveElbow = wristPos.y > elbowPos.y;
        bool wristLeftOfElbow = wristPos.x < elbowPos.x && wristAboveElbow;
        bool wristRightOfElbow = wristPos.x > elbowPos.x && wristAboveElbow;

        if (wristLeftOfElbow)
        {
            wristWasLeftOfElbow = true;
            leftStart = DateTime.Now;
        }
        else if ((DateTime.Now - leftStart).TotalSeconds > secondsThreshold)
        {
            wristWasLeftOfElbow = false;
        }

        if (wristRightOfElbow)
        {
            wristWasRightOfElbow = true;
            rightStart = DateTime.Now;
        }
        else if ((DateTime.Now - rightStart).TotalSeconds > secondsThreshold)
        {
            wristWasRightOfElbow = false;
        }

        return wristAboveElbow && wristWasRightOfElbow && wristWasLeftOfElbow;
    }
}
