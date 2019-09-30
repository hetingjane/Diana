/*
This is the module to change Diana's facial expressions based on emotion scores we get from BlackBoard.
It sets different weights to blend shapes when the score of a certain emotion below/above a certain threshold.

Reads:  user:dominantEmotion: (StringValue)
user:dominantEmotion:(enum)Emotion: (IntValue, ranges from 0 to 100)

TODO: add more emotions and a dynamic mechanism to let Diana express supportive empathy in appropraite context.
*/
using UnityEngine;
using System;
using System.Collections.Generic;

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


    }

    void Update()
    {
        //Get current dominantEmotion and its measurement score

        string dominantEmotion = DataStore.GetStringValue("user:dominantEmotion:");
        int score = DataStore.GetIntValue("user:dominantEmotion:" + dominantEmotion);
        switch (dominantEmotion)
        {
            case "Neutral":
                recoveryRate = 10;
                maxStrength = 0;
                currSmileStrength = Mathf.MoveTowards(currSmileStrength, maxStrength, recoveryRate * Time.deltaTime);
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
                recoveryRate = score / 5;
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
                recoveryRate = score / 5;
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