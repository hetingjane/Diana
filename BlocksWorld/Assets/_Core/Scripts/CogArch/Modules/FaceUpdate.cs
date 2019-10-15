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

public class FaceUpdate : MonoBehaviour
{
    // List of SkinnedMeshRenderers to change blend shapes on
    List<SkinnedMeshRenderer> renderers;

    // Index of the left and right blend shapes to set for each renderer above.
    List<int> smileLeftIndex;
    List<int> smileRightIndex;
    List<int> frownLeftIndex;
    List<int> frownRightIndex;

    float currSmileStrength;
    float currFrownStrength;

    float maxStrength;
    float recoveryRate;

    BlendShapeMixer bsm;
    void Start()
    {
        renderers = new List<SkinnedMeshRenderer>();
        smileLeftIndex = new List<int>();
        smileRightIndex = new List<int>();
        frownLeftIndex = new List<int>();
        frownRightIndex = new List<int>();

        //Find the avatar to change blend shape on

        var avatar = GameObject.Find("Diana2");
        if (avatar == null)
        {
            Debug.LogError("EmotionModule: Cannot find Diana object!");
            return;
        }

        foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Mesh mesh = smr.sharedMesh;
            int sleftIdx = mesh.GetBlendShapeIndex("Smile_Left");
            int srightIdx = mesh.GetBlendShapeIndex("Smile_Right");
            int fleftIdx = mesh.GetBlendShapeIndex("Frown_Left");
            int frightIdx = mesh.GetBlendShapeIndex("Frown_Right");

            if (sleftIdx >= 0 && srightIdx >= 0 && fleftIdx >= 0 && frightIdx >= 0)
            {
                renderers.Add(smr);
                smileLeftIndex.Add(sleftIdx);
                smileRightIndex.Add(srightIdx);
                frownLeftIndex.Add(fleftIdx);
                frownRightIndex.Add(frightIdx);

            }
        }

        bsm = avatar.GetComponentInChildren<BlendShapeMixer>();
        if (bsm == null)
        {
            Debug.LogError("bsm is null!");
        }
        DataStore.Subscribe("user:isEngaged", NoteUserIsEngaged);
        //DataStore.Subscribe("user:intent:isPosack", NoteUserIsGesturing);
        //DataStore.Subscribe("user:intent:isNegack", NoteUserIsGesturing);
        //DataStore.Subscribe("user:intent:isPushLeft", NoteUserIsGesturing);
        //DataStore.Subscribe("user:intent:isPushRight", NoteUserIsGesturing);
        //DataStore.Subscribe("user:intent:isNevermind", NoteUserIsGesturing);
        //DataStore.Subscribe("user:intent:isPosack", NoteUserIsGesturing);
        //DataStore.Subscribe("user:intent:isWave", NoteUserIsGesturing);


    }
    void NoteUserIsGesturing(string key, DataStore.IValue value)
    {
        if ((value as DataStore.BoolValue).val)
        {
            // User's gesturing, better pay attention.

            bsm.Apply();
        }
        else { bsm.ResetToNeutral(); }
    }
    void NoteUserIsEngaged(string key, DataStore.IValue value)
    {
        if ((value as DataStore.BoolValue).val)
        {
            StartCoroutine(WaitAndSmile(value, 0.55f));

        }
    }
    private IEnumerator WaitAndSmile(DataStore.IValue value, float delay)
    {
        yield return new WaitForSeconds(delay);
        for (int j = 0; j < 10; j++)
        {
            // User has just approached the table.  Smile to greet.
            recoveryRate = 30;
            maxStrength = 130;
            currSmileStrength = Mathf.MoveTowards(currSmileStrength, maxStrength, recoveryRate);
            for (int i = 0; i < renderers.Count; i++)
            {
                renderers[i].SetBlendShapeWeight(smileLeftIndex[i], currSmileStrength);
                renderers[i].SetBlendShapeWeight(smileRightIndex[i], currSmileStrength);

            }
        }

    }
    void Update()
    {
        DataStore.Subscribe("user:isPointing", NoteUserIsGesturing);

        //Get current dominantEmotion and its measurement score

        string dominantEmotion = DataStore.GetStringValue("user:dominantEmotion");
        int score = DataStore.GetIntValue("user:Emotion" + dominantEmotion);
        switch (dominantEmotion)
        {
            case "Neutral":
                recoveryRate = 30;
                maxStrength = 0;
                currSmileStrength = Mathf.MoveTowards(currSmileStrength, maxStrength + 30, recoveryRate * Time.deltaTime);
                currFrownStrength = Mathf.MoveTowards(currFrownStrength, maxStrength, recoveryRate * Time.deltaTime);
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].SetBlendShapeWeight(smileLeftIndex[i], currSmileStrength);
                    renderers[i].SetBlendShapeWeight(smileRightIndex[i], currSmileStrength);
                    renderers[i].SetBlendShapeWeight(frownLeftIndex[i], currFrownStrength);
                    renderers[i].SetBlendShapeWeight(frownRightIndex[i], currFrownStrength);

                }
                break;
            case "Happy":
                recoveryRate = 40;
                maxStrength = 100;
                currSmileStrength = Mathf.MoveTowards(currSmileStrength, maxStrength, recoveryRate * Time.deltaTime);
                currFrownStrength = Mathf.MoveTowards(currFrownStrength, 0, recoveryRate * Time.deltaTime);
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].SetBlendShapeWeight(smileLeftIndex[i], currSmileStrength);
                    renderers[i].SetBlendShapeWeight(smileRightIndex[i], currSmileStrength);
                    renderers[i].SetBlendShapeWeight(frownLeftIndex[i], currFrownStrength);
                    renderers[i].SetBlendShapeWeight(frownRightIndex[i], currFrownStrength);
                }
                break;
            case "Angry":
                recoveryRate = 40;
                maxStrength = 100;
                currFrownStrength = Mathf.MoveTowards(currFrownStrength, maxStrength, recoveryRate * Time.deltaTime);
                currSmileStrength = Mathf.MoveTowards(currSmileStrength, 0, recoveryRate * Time.deltaTime);
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].SetBlendShapeWeight(frownLeftIndex[i], currFrownStrength);
                    renderers[i].SetBlendShapeWeight(frownRightIndex[i], currFrownStrength);
                    renderers[i].SetBlendShapeWeight(smileLeftIndex[i], currSmileStrength);
                    renderers[i].SetBlendShapeWeight(smileRightIndex[i], currSmileStrength);

                }
                break;
            default:
                break;
        }
    }
}