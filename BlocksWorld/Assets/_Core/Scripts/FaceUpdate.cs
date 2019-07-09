using UnityEngine;
using MORPH3D;
using System;

public class FaceUpdate : MonoBehaviour
{
    //public AnimationClip[] animations;

    //Animator anim;

    //Transform player;
    DianaEmotion dianaEmotions;

    //public float delayWeight;

    M3DCharacterManager charMgr;
    //void Awake()
    //{
    //    player = GameObject.FindGameObjectWithTag("Player").transform;
    //    dianaEmotions = player.GetComponent<DianaEmotion>();

    //}

    void Start()
    {
        //anim = GetComponent<Animator>();
        charMgr = GetComponent<M3DCharacterManager>();
        dianaEmotions = charMgr.GetComponent<DianaEmotion>();
        Debug.Assert(charMgr != null);
        
    }

    void OnGUI()
    {
    }

    //float current = 0;


    void Update()
    {
        //if (Input.GetMouseButton(0))
        //{
        //    current = 1;
        //}
        //else
        //{
        //    current = Mathf.Lerp(current, 0, delayWeight);
        //}

        //anim.SetLayerWeight(1, 1);
        float dominantEmotion = Mathf.Max(dianaEmotions.currentSadness, dianaEmotions.currentJoy); //find the dominant emotion
        if (dominantEmotion <= 20)
        {
            charMgr.RemoveAllMorphs();
            //anim.SetLayerWeight(1, current); //set to neutral emotion
        }
        //        else
        else if (dominantEmotion == dianaEmotions.currentJoy)
        {

            //charMgr.SetBlendshapeValue("eCTRLHappy", 100);

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
            Debug.LogWarning(dominantEmotion);
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
