/*
This script gets all the emotions recognized by Affectiva and determines current dominantEmotion.
It compares emotion scores of all non-trivial emotions(above a low threshold) and picks the maximum then sets key-value pairs onto BlackBoard.

Writes:		user:dominantEmotion: (StringValue)
user:dominantEmotion:(enum)Emotion: (IntValue, ranges from 0 to 100)
TODO: add more emotions			
*/
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

public class PlayerEmotions : ModuleBase, ImageResultsListener
{
    float currentJoy = 0f;
    float currentAnger = 0f;

    public Emotion dominantEmotion = Emotion.Neutral;

    [Range(0, 100)]
    public float happyThreshold = 10f;

    [Range(0, 100)]
    public float angryThreshold = 1f;
    public void onFaceFound(float timestamp, int faceId)
    {
        Debug.Log("Found the face");
    }

    public void onFaceLost(float timestamp, int faceId)
    {
        Debug.Log("Lost the face");
    }

    public void onImageResults(Dictionary<int, Face> faces)
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


                var emotionValue = new DataStore.IntValue((int)currentJoy);
                DataStore.SetValue("user:emotion" + dominantEmotion.ToString(), emotionValue, null, emotionValue.ToString());
                DataStore.SetStringValue("user:dominantEmotion", new DataStore.StringValue(dominantEmotion.ToString()), null, dominantEmotion.ToString());
            }
            else if (currentAnger > angryThreshold)
            {
                dominantEmotion = Emotion.Angry;
                var emotionValue = new DataStore.IntValue((int)currentAnger);
                DataStore.SetValue("user:emotion" + dominantEmotion.ToString(), emotionValue, null, emotionValue.ToString());
                DataStore.SetStringValue("user:dominantEmotion", new DataStore.StringValue(dominantEmotion.ToString()), null, dominantEmotion.ToString());

            }
            else
            {
                dominantEmotion = Emotion.Neutral;
                DataStore.SetValue("user:emotionHappy" , new DataStore.IntValue(0), null, "0");
                DataStore.SetValue("user:emotionAngry", new DataStore.IntValue(0), null, "0");
                DataStore.SetStringValue("user:dominantEmotion", new DataStore.StringValue(dominantEmotion.ToString()), null, dominantEmotion.ToString());

            }

        }

    }
}