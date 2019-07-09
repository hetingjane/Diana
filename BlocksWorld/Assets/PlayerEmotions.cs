using Affdex;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEmotions : ImageResultsListener
{
    public float currentJoy;
    public float currentSadness;

    //public DataStore.IntValue joy;
    //public DataStore.IntValue sadness;
    public AffectModule affectModule;
    public override void onFaceFound(float timestamp, int faceId)
    {
        Debug.Log("Found the face");

    }

    public override void onFaceLost(float timestamp, int faceId)
    {
        currentSadness = 0;
        currentJoy = 0;
        Debug.Log("Lost the face");

    }

    public override void onImageResults(Dictionary<int, Face> faces)
    {
        if (faces.Count > 0)
        {

            faces[0].Emotions.TryGetValue(Emotions.Sadness, out currentSadness); //setting emotions...
            faces[0].Emotions.TryGetValue(Emotions.Joy, out currentJoy);
            Debug.LogError(currentJoy);
            var joy = new DataStore.IntValue((int)currentJoy);
            var sadness = new DataStore.IntValue((int)currentSadness);
            affectModule = GetComponent<AffectModule>();
            //DataStore.Subscribe("user:joy:", NoteJoy);
            DataStore.SetValue("user:joy:", joy, affectModule, currentJoy.ToString());

            DataStore.SetValue("user:sadness:", sadness, affectModule, currentSadness.ToString());
        }
    }
    //void NoteJoy(string key, DataStore.IValue value)
    //{
    //    if (value.ToString() == "userPoint" && key == "me:intent:lookAt") mode = Mode.Looking;
    //    else if (value.ToString() == "userPoint" && key == "me:intent:pointAt") mode = Mode.Pointing;
    //    else if (mode != Mode.Off)
    //    {
    //        mode = Mode.Off;
    //        SetValue("me:intent:target", "", "mode := Mode.Off");
    //        SetValue("me:intent:action", "", "mode := Mode.Off");
    //    }
    //}
}
