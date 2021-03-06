﻿using UnityEngine;
using MORPH3D;
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

    //PlayerEmotions playerEmotions;
    //GameObject player;

    void Start()
    {
        renderers = new List<SkinnedMeshRenderer>();
        smileLeftIndex = new List<int>();
        smileRightIndex = new List<int>();
        frownLeftIndex = new List<int>();
        frownRightIndex = new List<int>();

	    // Find the avatar.  Currently this works only with Diana2.
	    // If using some other avatar, log a warning, and disable this module.
	    var avatar = GameObject.Find("Diana2");
        if (avatar == null)
        {
	        Debug.LogWarning("EmotionModule: Cannot find Diana object!  (Disabling EmotionModule.)", this);
	        gameObject.SetActive(false);
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
        //player = GameObject.Find("AffectivaModule");
        //playerEmotions = player.GetComponent<PlayerEmotions>();
        //if (playerEmotions == null)
        //{
        //    Debug.LogError("No playerEmotions component found.");
        //}

    }

    void Update()
    {
        string dominantEmotion = DataStore.GetStringValue("user:dominantEmotion:");
        int score = DataStore.GetIntValue("user:dominantEmotion:" + dominantEmotion);
        switch (dominantEmotion)
        {
            case "Neutral":
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].SetBlendShapeWeight(smileLeftIndex[i], 0);
                    renderers[i].SetBlendShapeWeight(smileRightIndex[i], 0);
                    renderers[i].SetBlendShapeWeight(frownLeftIndex[i], 0);
                    renderers[i].SetBlendShapeWeight(frownRightIndex[i], 0);

                }
                break;
            case "Happy":
                if (score > 90)
                {
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        renderers[i].SetBlendShapeWeight(smileLeftIndex[i], 70);
                        renderers[i].SetBlendShapeWeight(smileRightIndex[i], 70);

                    }
                }
                else
                {

                    for (int i = 0; i < renderers.Count; i++)
                    {
                        renderers[i].SetBlendShapeWeight(smileLeftIndex[i], 30);
                        renderers[i].SetBlendShapeWeight(smileRightIndex[i], 30);

                    }

                }
                break;
            case "Angry":
                if (score > 20)
                {
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        renderers[i].SetBlendShapeWeight(frownLeftIndex[i], 100);
                        renderers[i].SetBlendShapeWeight(frownRightIndex[i], 100);

                    }
                }
                else
                {
                    for (int i = 0; i < renderers.Count; i++)
                    {
                        renderers[i].SetBlendShapeWeight(frownLeftIndex[i], 50);
                        renderers[i].SetBlendShapeWeight(frownRightIndex[i], 50);
                    }
                }
                break;
            default:
                break;

        }


    }
}
