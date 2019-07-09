using UnityEngine;
using UnityEngine.UI;
using Affdex;
using System.Collections.Generic;

public class DianaEmotion : ImageResultsListener
{
    //public float currentValence;
    //public float currentEngagement;
    //public float currentAttention;
    public float currentJoy;
    public float currentSadness;

    //public Slider joySlider;
    //public Slider sadnessSlider;
    //public Slider surpriseSlider;
    //public Slider valenceSlider;
    //public Slider engagementSlider;


    public override void onFaceFound(float timestamp, int faceId) //being called instantly when first found face
    {
       
        if (Debug.isDebugBuild) Debug.Log("Found the face");
    }

    public override void onFaceLost(float timestamp, int faceId)
    {

        currentSadness = 0;
        //currentEngagement = 0;

        currentJoy = 0;
        //currentSadness = 0;
        if (Debug.isDebugBuild) Debug.Log("Lost the face");
    }

    public override void onImageResults(Dictionary<int, Face> faces)
    {
        if (faces.Count > 0)
        {
            faces[0].Emotions.TryGetValue(Emotions.Sadness, out currentSadness); //setting emotions...
            //faces[0].Emotions.TryGetValue(Emotions.Engagement, out currentEngagement);
            //faces[0].Emotions.TryGetValue(Emotions., out currentSadness);
            faces[0].Emotions.TryGetValue(Emotions.Joy, out currentJoy);
            //faces[0].Expressions.TryGetValue(Expressions.Attention, out currentAttention);
            //joySlider.value = currentJoy;
            //sadnessSlider.value = currentSadness;
            //surpriseSlider.value = currentSurprise;
            //valenceSlider.value = currentValence;
            //engagementSlider.value = currentEngagement;

        }
    }
}