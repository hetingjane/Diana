using UnityEngine;
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

    PlayerEmotions playerEmotions;
    GameObject player;

    void Start()
    {
        renderers = new List<SkinnedMeshRenderer>();
        smileLeftIndex = new List<int>();
        smileRightIndex = new List<int>();
        frownLeftIndex = new List<int>();
        frownRightIndex = new List<int>();
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
        player = GameObject.Find("AffectivaModule");
        playerEmotions = player.GetComponent<PlayerEmotions>();
        if (playerEmotions == null)
        {
            Debug.LogError("No playerEmotions component found.");
        }

    }

    void Update()
    {
        int score = DataStore.GetIntValue("user:dominantEmotion:" + playerEmotions.dominantEmotion.ToString());
        switch (playerEmotions.dominantEmotion)
        {
            case Emotion.Neutral:
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].SetBlendShapeWeight(smileLeftIndex[i], 0);
                    renderers[i].SetBlendShapeWeight(smileRightIndex[i], 0);
                    renderers[i].SetBlendShapeWeight(frownLeftIndex[i], 0);
                    renderers[i].SetBlendShapeWeight(frownRightIndex[i], 0);

                }
                break;
            case Emotion.Happy:
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
            case Emotion.Angry:
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
