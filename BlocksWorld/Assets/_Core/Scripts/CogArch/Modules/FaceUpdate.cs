/*
This is the module to change Diana's facial expressions based on emotion scores we get from BlackBoard.
It sets different weights to blend shapes when the score of a certain emotion below/above a certain threshold.

Reads:  user:emotion (StringValue)

TODO: add more emotions and a dynamic mechanism to let Diana express supportive empathy in appropraite context.
*/
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class FaceUpdate : ModuleBase
{
    // List of SkinnedMeshRenderers to change blend shapes on
    List<SkinnedMeshRenderer> renderers;

    // Index of the left and right blend shapes to set for each renderer above.
    List<int> smileLeftIndex;
    List<int> smileRightIndex;

    private readonly float currSmileStrength;

    string dianaEmotion;
    BlendShapeMixer bsm;
    private IEnumerator coroutineChange;
    private IEnumerator coroutineDecade;

    protected override void Start()
    {
        //Find the avatar to change blend shape on

        var avatar = GameObject.Find("Diana2");
        if (avatar == null)
        {
            Debug.LogError("EmotionModule: Cannot find Diana2 object!");
            return;
        }
        bsm = avatar.GetComponentInChildren<BlendShapeMixer>();
        if (bsm == null)
        {
            Debug.LogError("EmotionModule: Cannot find BlendShapeMixer!");
        }
        renderers = new List<SkinnedMeshRenderer>();
        smileLeftIndex = new List<int>();
        smileRightIndex = new List<int>();
        foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh mesh = smr.sharedMesh;
            int leftIdx = mesh.GetBlendShapeIndex("Smile_Left");
            int rightIdx = mesh.GetBlendShapeIndex("Smile_Right");
            if (leftIdx >= 0 && rightIdx >= 0)
            {
                renderers.Add(smr);
                smileLeftIndex.Add(leftIdx);
                smileRightIndex.Add(rightIdx);
            }
        }
        //SetValue("me:emotion", "neutral", "Initialize");
        DataStore.Subscribe("user:isEngaged", NoteUserIsEngaged);
        coroutineChange = WaitAndChange(dianaEmotion, 0.5f);
        coroutineDecade = WaitAndDecade(4f);

    }
    protected void NoteUserIsEngaged(string key, DataStore.IValue value)
    {
        if ((value as DataStore.BoolValue).val)
        {
            StartCoroutine(coroutineChange);
            StartCoroutine(coroutineDecade);
        }
        else SetValue("me:emotion", "neutral", "Diana is neutral");


    }
    private IEnumerator WaitAndChange(string dianaEmotion, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SetValue("me:emotion", "greet", "Diana is happpy");


    }
    private IEnumerator WaitAndDecade(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SetValue("me:emotion", "neutral", "Diana is neutral");

    }
    protected void Update()
    {


        dianaEmotion = DataStore.GetStringValue("me:emotion");
        switch (dianaEmotion)
        {
            case "greet":
                bsm.Apply("joy");
                break;
            case "neutral":
                bsm.ResetToNeutral();
                break;
            case "joy":
                bsm.Apply(dianaEmotion);
                break;
            case "concentration":
                bsm.Apply(dianaEmotion);
                break;
            case "joy+concentration":
                bsm.Apply(dianaEmotion);
                break;
            case "frustration+concentration":
                bsm.Apply(dianaEmotion);
                break;
            case "confusion":
                bsm.Apply(dianaEmotion);
                break;
            case "frustration":
                bsm.Apply(dianaEmotion);
                break;
            default:
                break;
        }

        string userEmotion = DataStore.GetStringValue("user:emotion");
        bool userPointing = DataStore.GetBoolValue("user:isPointing");
        if (userPointing)
        {
            if (userEmotion == "joy") SetValue("me:emotion", "joy+concentration", "Diana is happy and concentrated");
            else if (userEmotion == "angry") SetValue("me:emotion", "frustration+concentration", "Diana is frustrated and concentrated");
            else if (dianaEmotion != "greet" && dianaEmotion != "confusion") SetValue("me:emotion", "concentration", "Diana is concentrated");

        }
        if (!userPointing)
        {
            if (userEmotion == "joy") SetValue("me:emotion", "joy", "Diana is happy");
            else if (userEmotion == "angry") SetValue("me:emotion", "frustration", "Diana is frustrated");
            else if (dianaEmotion != "greet" && dianaEmotion != "confusion") SetValue("me:emotion", "neutral", "Diana is neutral");
        }

    }
}