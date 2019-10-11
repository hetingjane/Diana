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
    }

    public void BeginInteraction(object content) {
        SetValue("me:speech:intent", "Hello.", string.Empty);
    }

    public void Ready(object content) {
        SetValue("me:speech:intent", "I'm ready to go.", string.Empty);
    }
}
