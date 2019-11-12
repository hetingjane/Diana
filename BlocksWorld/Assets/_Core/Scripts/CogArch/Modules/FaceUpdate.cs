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
    private List<SkinnedMeshRenderer> renderers;

    // Index of the left and right blend shapes to set for each renderer above.
    private List<int> smileLeftIndex;
    private List<int> smileRightIndex;

    private readonly float currSmileStrength;

    private BlendShapeMixer bsm;
    private IEnumerator coroutineChange;
    private IEnumerator coroutineFade;
    private IEnumerator coroutineConcentration;

    protected override void Start()
    {
        //Find the avatar to change blend shape on
	    //(this is now always the object this component is on)
	    var avatar = gameObject;
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
        //DataStore.Subscribe("me:emotion", NoteDianaEmotion);
        DataStore.Subscribe("user:isPointing", NoteUserIsPointing);
        DataStore.Subscribe("user:emotion", NoteUserEmotion);

        
    }
    private void NoteUserIsEngaged(string key, DataStore.IValue value)
    {
        if ((value as DataStore.BoolValue).val)
        {
            coroutineChange = WaitAndChange(0.5f);
            coroutineFade = WaitAndFade(2f);
            StartCoroutine(coroutineChange);
            StartCoroutine(coroutineFade);
        }
        else SetValue("me:emotion", "neutral", "Diana is neutral");


    }
    private IEnumerator WaitAndChange(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SetValue("me:emotion", "greet", "Diana is happy");


    }
    private IEnumerator WaitAndFade(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SetValue("me:emotion", "neutral", "Diana is neutral");

    }
    private IEnumerator WaitAndConcentrate(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SetValue("me:emotion", "concentration", "Diana is concentrated");



    }
    private void NoteDianaEmotion(string key, DataStore.IValue value)
    {
        bool userPointing = DataStore.GetBoolValue("user:isPointing");
        if ((value as DataStore.StringValue).val != "neutral" && (value as DataStore.StringValue).val != "concentration")
        {
            if (userPointing)
            {
                //lock (coroutineConcentration)
                { StartCoroutine(coroutineConcentration); }
            }
            else
            {
                //lock (coroutineFade)
                { StartCoroutine(coroutineFade); }
            }
        }


    }
    private void NoteUserEmotion(string key, DataStore.IValue value)
    {
        bool userPointing = DataStore.GetBoolValue("user:isPointing");
        string dianaEmotion = DataStore.GetStringValue("me:emotion");
        if ((value as DataStore.StringValue).val == "joy")
        {
            if (userPointing) SetValue("me:emotion", "joy+concentration", "Diana is happy and concentrated");
            else SetValue("me:emotion", "joy", "Diana is happy");
        }
        if ((value as DataStore.StringValue).val == "angry")
        {
            if (userPointing) SetValue("me:emotion", "frustration+concentration", "Diana is frustrated and concentrated");
            else SetValue("me:emotion", "frustration", "Diana is frustrated");
        }
        if ((value as DataStore.StringValue).val == "neutral" && dianaEmotion != "neutral" && dianaEmotion != "concentration")
        {
            coroutineFade = WaitAndFade(2f);
            coroutineConcentration = WaitAndConcentrate(2f);
            if (userPointing) StartCoroutine(coroutineConcentration);
            else StartCoroutine(coroutineFade);
        }
    }
    private void NoteUserIsPointing(string key, DataStore.IValue value)
    {
        string userEmotion = DataStore.GetStringValue("user:emotion");
        if ((value as DataStore.BoolValue).val)
        {
            if (userEmotion == "joy") SetValue("me:emotion", "joy+concentration", "Diana is happy and concentrated");
            if (userEmotion == "angry") SetValue("me:emotion", "frustration+concentration", "Diana is frustrated and concentrated");
            else SetValue("me:emotion", "concentration", "Diana is concentrated");
        }
        else
        {

            if (userEmotion == "joy") SetValue("me:emotion", "joy", "Diana is happy");
            if (userEmotion == "angry") SetValue("me:emotion", "frustration", "Diana is frustrated");
            else SetValue("me:emotion", "neutral", "Diana is neutral");
        }



    }
    private void Update()
    {


        string dianaEmotion = DataStore.GetStringValue("me:emotion");
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

        //string userEmotion = DataStore.GetStringValue("user:emotion");
        //bool userPointing = DataStore.GetBoolValue("user:isPointing");
        //if (userPointing)
        //{
        //    //SetValue("me:emotion", "concentration", "Diana is concentrated");
        //    if (userEmotion == "joy") SetValue("me:emotion", "joy+concentration", "Diana is happy and concentrated");
        //    else if (userEmotion == "angry") SetValue("me:emotion", "frustration+concentration", "Diana is frustrated and concentrated");
        //    //else if (dianaEmotion != "neutral") StartCoroutine(coroutineFade); //if (dianaEmotion != "greet" && dianaEmotion != "confusion") SetValue("me:emotion", "concentration", "Diana is concentrated");

        //}
        //if (!userPointing)
        //{
        //    //SetValue("me:emotion", "neutral", "Diana is neutral");

        //    if (userEmotion == "joy") SetValue("me:emotion", "joy", "Diana is happy");
        //    else if (userEmotion == "angry") SetValue("me:emotion", "frustration", "Diana is frustrated");
        //    //else if (dianaEmotion != "neutral") StartCoroutine(coroutineFade); //if (dianaEmotion != "greet" && dianaEmotion != "confusion") SetValue("me:emotion", "neutral", "Diana is neutral");
        //}

    }
}