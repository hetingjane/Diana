/*
This script gets all the emotions recognized by Affectiva and determines current dominantEmotion.
It compares emotion scores of all non-trivial emotions(above a low threshold) and picks the maximum then sets key-value pairs onto BlackBoard.

Writes:		user:emotion: (StringValue)
TODO: add more emotions			
*/
using Affdex;
using System.Collections.Generic;
using UnityEngine;

public enum Emotion
{
    neutral,
    angry,
    joy,

}

public class PlayerEmotions : ModuleBase, ImageResultsListener
{
    float currentJoy = 0f;
    float currentAnger = 0f;

    public Emotion dominantEmotion = Emotion.neutral;

    [Range(0, 100)]
    public float happyThreshold = 50f;

    //[Range(0, 100)]
    public float angryThreshold = 10f;
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

            if (currentJoy > happyThreshold)
            {
                dominantEmotion = Emotion.joy;
                SetValue("user:emotion", dominantEmotion.ToString(), "User is happy");
            }
            else if (currentAnger > angryThreshold)
            {
                dominantEmotion = Emotion.angry;
                SetValue("user:emotion", dominantEmotion.ToString(), "User is angry");
            }
            else
            {
                dominantEmotion = Emotion.neutral;
                SetValue("user:emotion", dominantEmotion.ToString(), "User is neutral");
            }
        }

    }
}