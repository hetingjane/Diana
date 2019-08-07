using UnityEngine;
using MORPH3D;
using System;

public class FaceUpdate : MonoBehaviour
{


    M3DCharacterManager charMgr;
    PlayerEmotions playerEmotions;

    void Start()
    {
        playerEmotions = GetComponent<PlayerEmotions>();
        charMgr = GetComponent<M3DCharacterManager>();
        Debug.Assert(playerEmotions != null);
        Debug.Assert(charMgr != null);

    }

    void Update()
    {
        int score = DataStore.GetIntValue("user:dominant emotion:" + playerEmotions.dominantEmotion.ToString());
        switch (playerEmotions.dominantEmotion)
        {
            case Emotion.Neutral:
                charMgr.SetBlendshapeValue("eCTRLHappy", 0);
                charMgr.SetBlendshapeValue("eCTRLSad", 0);
                break;
            case Emotion.Happy:
                if (score > 90)
                {
                    charMgr.SetBlendshapeValue("eCTRLHappy", 100);
                }
                else
                {
                    charMgr.SetBlendshapeValue("eCTRLHappy", 50);

                }
                break;
            case Emotion.Angry:
                if (score > 30)
                {
                    charMgr.SetBlendshapeValue("eCTRLSad", 100);
                }
                else
                {
                    charMgr.SetBlendshapeValue("eCTRLSad", 50);

                }
                break;
            default:
                break;

        }


    }
}
