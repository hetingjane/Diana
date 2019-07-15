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
    //float currentFear = 0f;
    //float currentContempt = 0f;
    //public FeaturePoint[] featurePointsList;

    public Emotion dominantEmotion = Emotion.Neutral;

    [Range(0, 100)]
    public float happyThreshold = 70f;

    [Range(0, 100)]
    public float angryThreshold = 10f;

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
        //Debug.Log("Got face results");

        foreach (KeyValuePair<int, Face> pair in faces)
        {
            int FaceId = pair.Key;  // The Face Unique Id.
            Face face = pair.Value;    // Instance of the face class containing emotions, and facial expression values.

            //Retrieve the Emotions Scores
            //face.Emotions.TryGetValue(Emotions.Contempt, out currentContempt);
            face.Emotions.TryGetValue(Emotions.Joy, out currentJoy);
            face.Emotions.TryGetValue(Emotions.Anger, out currentAnger);
            //face.Emotions.TryGetValue(Emotions.Fear, out currentFear);

            //Retrieve the Smile Score
            //face.Expressions.TryGetValue(Expressions.Smile, out currentSmile);

            if (currentJoy >= currentAnger)
            {
                if (currentJoy > happyThreshold)
                    dominantEmotion = Emotion.Happy;
            }
            else if (currentAnger > angryThreshold)
            {
                dominantEmotion = Emotion.Angry;
            }
            else
            {
                dominantEmotion = Emotion.Neutral;
            }

            //Retrieve the coordinates of the facial landmarks (face feature points)
            //featurePointsList = face.FeaturePoints;

        }
    }
}