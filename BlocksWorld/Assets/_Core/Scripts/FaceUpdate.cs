using UnityEngine;
using MORPH3D;
using System;

public class FaceUpdate : MonoBehaviour
{


    M3DCharacterManager charMgr;
    PlayerEmotions playerEmotions;

    //DataStore dataStore;
    void Start()
    {
        playerEmotions = GetComponent<PlayerEmotions>();
        charMgr = GetComponent<M3DCharacterManager>();
        //dataStore = GetComponent<DataStore>();
        Debug.Assert(playerEmotions != null);
        Debug.Assert(charMgr != null);

    }

    void Update()
    {

        switch (playerEmotions.dominantEmotion)
        {
            case Emotion.Neutral:
                charMgr.SetBlendshapeValue("eCTRLHappy", 0);
                charMgr.SetBlendshapeValue("eCTRLSad", 0);
                break;
            case Emotion.Happy:
                charMgr.SetBlendshapeValue("eCTRLHappy", 100);
                charMgr.SetBlendshapeValue("eCTRLSad", 0);

                break;
            case Emotion.Angry:
                charMgr.SetBlendshapeValue("eCTRLSad", 100);
                charMgr.SetBlendshapeValue("eCTRLHappy", 0);

                break;
            default:
                break;

        }


    }
}
