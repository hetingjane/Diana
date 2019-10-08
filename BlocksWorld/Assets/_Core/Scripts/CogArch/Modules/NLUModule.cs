/*
This script parses natural language input into forms VoxSim can work with

Reads:      user:speech (StringValue, transcribed speech)
Writes:     user:intent:event (StringValue, predicate logic form used by VoxSim event manager)

*/

using UnityEngine;

using VoxSimPlatform.Network;

public class NLUModule : ModuleBase
{
    public CommunicationsBridge communicationsBridge;

    // Use this for initialization
    void Start()
    {
        base.Start();
        DataStore.Subscribe("user:speech", ParseLanguageInput);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ParseLanguageInput(string key, DataStore.IValue value)
    {
        string input = value.ToString().Trim();
        if (string.IsNullOrEmpty(input)) return;

        Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".",input));

        string parsed = communicationsBridge.NLParse(input);
        Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".",parsed));

        SetValue("user:intent:event", parsed, string.Empty);
    }
}
