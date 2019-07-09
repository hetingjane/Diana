using UnityEngine;
using MORPH3D;
using System;

public class FaceUpdate : MonoBehaviour
{

    DianaEmotion dianaEmotions;

    M3DCharacterManager charMgr;
    DataStore dataStore;
    void Start()
    {
        charMgr = GetComponent<M3DCharacterManager>();
        dataStore = GetComponent<DataStore>();
    }

    void Update()
    {

        

        int dominantEmotion = Mathf.Max(dataStore.IGetIntValue("user:joy:"), dataStore.IGetIntValue("user:sadness:")); //find the dominant emotion
        if (dominantEmotion <= 20)
        {
        }
        else if (dominantEmotion == dianaEmotions.currentJoy)
        {

            if (dominantEmotion > 60)
            {
                //charMgr.SetBlendshapeValue("eCTRLMouthOpen", 50);
                charMgr.SetBlendshapeValue("eCTRLHappy", 100);
                //charMgr.SetBlendshapeValue("eCTRLMouthSmile", 100);

            }
            else
            {

                charMgr.SetBlendshapeValue("eCTRLHappy", 50);
            }

        }
        else if (dominantEmotion == dianaEmotions.currentSadness)
        {
            //charMgr.SetBlendshapeValue("eCTRLSad", 100);

            if (dominantEmotion > 40)
            {
                charMgr.SetBlendshapeValue("eCTRLSad", 100);
            }
            else
            {
                charMgr.SetBlendshapeValue("eCTRLSad", 50);
            }

        }
    }
}
