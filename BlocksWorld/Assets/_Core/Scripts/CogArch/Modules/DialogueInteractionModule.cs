using UnityEngine;
using System.Collections;

using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Logging;
using VoxSimPlatform.Interaction;

public class DialogueInteractionModule : ModuleBase
{
    public DialogueStateMachine stateMachine;

    // Use this for initialization
    void Start()
    {
        base.Start();

        for (int i = 0; i < GameObject.Find("Avatars (Pick One)").transform.childCount; i++)
        {
            if (GameObject.Find("Avatars (Pick One)").transform.transform.GetChild(i).gameObject.activeSelf)
            {
                stateMachine = GameObject.Find("Avatars (Pick One)").transform.GetChild(i).GetComponent<DialogueStateMachine>();
                stateMachine.scenarioController = gameObject.GetComponent<SingleAgentInteraction>();
            }
        }

        DataStore.instance.onValueChanged.AddListener(ValueChanged);
    }

    // Update is called once per frame
    void Update()
    {

    }

    protected override void ValueChanged(string key)
    {
        //Debug.Log(string.Format("Diana's World: {0} changed",key));
        stateMachine.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
            stateMachine.GenerateStackSymbol(DataStore.instance)));

        if (key == "user:intent:isPushLeft") {
            if (DataStore.GetBoolValue(key)) {
                SetValue("user:intent:action", "slide({0},{1}(left))", string.Empty);
            }
        }
        else if (key == "user:intent:isPushRight") {
            if (DataStore.GetBoolValue(key)) {
                SetValue("user:intent:action", "slide({0},{1}(right))", string.Empty);
            }
        }
        if (key == "user:intent:isServoLeft") {
            if (DataStore.GetBoolValue(key)) {
                SetValue("user:intent:action", "slidep({0},{1}(left))", string.Empty);
            }
        }
        else if (key == "user:intent:isServoRight") {
            if (DataStore.GetBoolValue(key)) {
                SetValue("user:intent:action", "slidep({0},{1}(right))", string.Empty);
            }
        }
        else if (key == "user:intent:isClaw") {
            if (DataStore.GetBoolValue(key)) {
                SetValue("user:intent:action", "grasp({0})", string.Empty);
            }
        }
        else if (key == "user:isPointing") {
            if (!DataStore.GetBoolValue(key)) {
                SetValue("user:lastPointedAt:name", DataStore.StringValue.Empty, string.Empty);
                SetValue("user:lastPointedAt:position", DataStore.Vector3Value.Zero, string.Empty);
            }
        }
        else if (key == "user:lastPointedAt:name") {
            if (DataStore.GetStringValue(key) != string.Empty) {
                if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object"))) {
                    SetValue("user:intent:object", DataStore.GetStringValue(key), string.Empty);
                }
                else if (DataStore.GetStringValue("user:intent:object") != DataStore.GetStringValue(key)) {
                    SetValue("user:intent:partialEvent",
                        string.Format("put({0},on({1}))",
                        DataStore.GetStringValue("user:intent:object"),
                        DataStore.GetStringValue(key)), string.Empty);
                }
            }
        }
        else if (key == "user:lastPointedAt:position") {
            if (string.IsNullOrEmpty(DataStore.GetStringValue("user:lastPointedAt:name"))) {
                if (DataStore.GetVector3Value(key) != default) {
                    Debug.Log(string.Format("Setting user:intent:location to {0} ({1})", DataStore.GetVector3Value(key), key));
                    SetValue("user:intent:location", DataStore.GetVector3Value(key), string.Empty);
                }
            }
        }
        else if (key == "user:communication") {
            CommunicationValue comm = DataStore.GetValue("user:communication") as CommunicationValue;
	        Debug.Log(string.Format("User communication: {0} {1} {2} {3}", comm.val, comm.val.directAddress, comm.val.parse, comm.val.originalText));
        }
    }

    public void BeginInteraction(object content) {
        SetValue("me:speech:intent", "Hello.", string.Empty);
    }

    public void Ready(object content) {
        SetValue("me:speech:intent", "I'm ready to go.", string.Empty);
        SetValue("user:isInteracting", true, string.Empty);
    }

    public void ModularInteractionLoop(object content) {
        // do anything that needs to happen when we first enter the main
        //  interaction loop here
    }

    public void CleanUp(object content) {
        SetValue("me:speech:intent", "Bye!", string.Empty);
        SetValue("user:isInteracting", false, string.Empty);
    }
}