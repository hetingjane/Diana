/*
This is the module to change Diana's facial expressions based on emotion scores we get from BlackBoard.
It sets different weights to blend shapes when the score of a certain emotion below/above a certain threshold.

Reads:  user:dominantEmotion (StringValue)
        user:dominantEmotion:(enum)Emotion: (IntValue, ranges from 0 to 100)

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

    private float currSmileStrength;

    private float maxStrength = 100;
    private float recoveryRate = 10;
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
        DataStore.Subscribe("user:isEngaged", NoteUserIsEngaged);
        //SetValue("me:emotion", "neutral", "Initialize");
        coroutineChange = WaitAndChange(dianaEmotion,0.6f);
        coroutineDecade = WaitAndDecade(0.6f);

    }
    protected void NoteUserIsEngaged(string key, DataStore.IValue value)
    {
        if ((value as DataStore.BoolValue).val)
        {
            SetValue("me:emotion", "joy", "Diana is happpy");
            StartCoroutine(coroutineChange);
            //StartCoroutine(coroutineDecade);
        }
        else SetValue("me:emotion", "neutral", "Diana is neutral");


    }
    private IEnumerator WaitAndChange(string dianaEmotion,float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        bsm.Apply(dianaEmotion);

    }
    private IEnumerator WaitAndDecade(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        //SetValue("me:emotion", "neutral", "Diana is neutral");
        bsm.ResetToNeutral();

    }
    protected void Update()
    {
        dianaEmotion = DataStore.GetStringValue("me:emotion");
        switch (dianaEmotion)
        {
            case "neutral":
                StartCoroutine(coroutineDecade);
                
                break;
            case "joy":
                StartCoroutine(coroutineChange);

                break;
            case "concentration":
                StartCoroutine(coroutineChange);
                break;
            default:
                break;
        }

        string userEmotion = DataStore.GetStringValue("user:emotion");
        bool userPointing = DataStore.GetBoolValue("user:isPointing");
        if (userEmotion == "joy" && userPointing)
        {
            SetValue("me:emotion", "joy+concentration", "Diana is happy and concentrated");
        }
        if (userEmotion != "joy" && userPointing)
        {
            SetValue("me:emotion", "concentration", "Diana is concentrated");
        }
        if (userEmotion == "joy" && !userPointing)
        {
            SetValue("me:emotion", "joy", "Diana is happy");
        }
        if (userEmotion != "joy" && !userPointing)
        {
//            StartCoroutine(coroutineDecade);
            SetValue("me:emotion", "neutral", "Diana is neutral");
        }



    }
}