using Affdex;
using System.Collections.Generic;
using UnityEngine;

public enum Emotion
{
    Neutral,
    Angry,
    Happy,
    //Confused
}
public class PlayerEmotions : ImageResultsListener
{
    float currentJoy = 0f;
    float currentAnger = 0f;
    //public FeaturePoint[] featurePointsList;

    public Emotion dominantEmotion = Emotion.Neutral;

    [Range(0, 100)]
    public float happyThreshold = 10f;

    [Range(0, 100)]
    public float angryThreshold = 20f;
    public override void onFaceFound(float timestamp, int faceId)
    {
        Debug.Log("Found the face");
    }

    public override void onFaceLost(float timestamp, int faceId)
    {
        Debug.Log("Lost the face");
    }

    public override void onImageResults(Dictionary<int, Face> faces)
    {

        foreach (KeyValuePair<int, Face> pair in faces)
        {
            int FaceId = pair.Key;  // The Face Unique Id.
            Face face = pair.Value;    // Instance of the face class containing emotions, and facial expression values.

            //Retrieve the Emotions Scores
            face.Emotions.TryGetValue(Emotions.Joy, out currentJoy);
            face.Emotions.TryGetValue(Emotions.Anger, out currentAnger);

            //Retrieve the Smile Score
            //face.Expressions.TryGetValue(Expressions.Smile, out currentSmile);



            if (currentJoy > happyThreshold)
            {
                dominantEmotion = Emotion.Happy;


                var EmotionValue = new DataStore.IntValue((int)currentJoy);
                DataStore.SetValue("user:dominant emotion:" + dominantEmotion.ToString(), EmotionValue, null, dominantEmotion.ToString());

            }
            else if (currentAnger > angryThreshold)
            {
                dominantEmotion = Emotion.Angry;
                var EmotionValue = new DataStore.IntValue((int)currentAnger);
                DataStore.SetValue("user:dominant emotion:" + dominantEmotion.ToString(), EmotionValue, null, dominantEmotion.ToString());

            }
            else
            {
                dominantEmotion = Emotion.Neutral;
                DataStore.SetValue("user:dominant emotion:Happy" , new DataStore.IntValue(0), null, dominantEmotion.ToString());
                DataStore.SetValue("user:dominant emotion:Angry", new DataStore.IntValue(0), null, dominantEmotion.ToString());

            }
            //Retrieve the coordinates of the facial landmarks (face feature points)
            //featurePointsList = face.FeaturePoints;
        }

    }
}