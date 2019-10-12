﻿/*
This script parses natural language input into forms VoxSim can work with

Reads:      user:speech (StringValue, transcribed speech)
Writes:     user:intent:event (StringValue, predicate logic form used by VoxSim event manager)

*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

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

        string mapped = MapTerms(input);

        string parsed = communicationsBridge.NLParse(mapped);
        Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".",parsed));

        SetValue("user:intent:event", parsed, string.Empty);
    }

    string MapTerms(string input) {

        string mapped = input;

        // do verb mapping
        if (mapped.StartsWith("pick up")) {
            mapped = mapped.Replace("pick up", "lift");
        }
        else if (mapped.StartsWith("pick") && (mapped.EndsWith("up"))) {
            mapped = mapped.Replace("pick", "lift").Replace("up", "");
        }
        else if (mapped.StartsWith("grab")) {
            mapped = mapped.Replace("grab", "grasp");
        }
        else if (mapped.StartsWith("take")) {
            mapped = mapped.Replace("take", "grasp");
        }
        else if (mapped.StartsWith("move")) {
            mapped = mapped.Replace("move", "put");
        }
        else if (mapped.StartsWith("push")) {
            mapped = mapped.Replace("push", "slide");
        }
        else if (mapped.StartsWith("pull")) {
            mapped = mapped.Replace("pull", "slide");
        }

        // do noun mapping
        if (mapped.Split().Contains("box")) {
            mapped = mapped.Replace("box", "block");
        }
        else if (mapped.Split().Contains("boxes")) {
            mapped = mapped.Replace("boxes", "blocks");
        }
        else if (mapped.Split().Contains("mug")) {
            mapped = mapped.Replace("mug", "cup");
        }
        else if (mapped.Split().Contains("mugs")) {
            mapped = mapped.Replace("mugs", "cups");
        }

        if (mapped.Split().Contains("these")) {
            mapped = mapped.Replace("these", "this");
        }
        else if (mapped.Split().Contains("those")) {
            mapped = mapped.Replace("those", "that");
        }

        // get rid of "on/to the" before PP
        // on the top of, on top of -> on
        // to back of, to the back of -> behind
        if ((mapped.Contains("to the left")) || (mapped.Contains("to the right"))) {
            mapped = mapped.Replace("to the", "");
        }
        else if (mapped.Contains("to back of")) {
            mapped = mapped.Replace("to back of", "behind");
        }
        else if (mapped.Contains("to the back of")) {
            mapped = mapped.Replace("to the back of", "behind");
        }
        else if (mapped.Contains("on top of")) {
            mapped = mapped.Replace("on top of", "on");
        }
        else if (mapped.Contains("on the top of")) {
            mapped = mapped.Replace("on the top of", "on");
        }

        // add anaphor placeholder
        if (mapped.Split().Contains("one")) {
            mapped = mapped.Replace("one", "{2}");
        }
        else if (mapped.Split().Contains("ones")) {
            mapped = mapped.Replace("ones", "{2}");
        }

        if (mapped.Split().Contains("it")) {
            mapped = mapped.Replace("it", "{0}");
        }
        else if (mapped.Split().Contains("them")) {
            mapped = mapped.Replace("them", "{0}");
        }

        if (mapped.Contains("here")) {
            mapped = mapped.Replace("here", "{1}");
        }
        else if (mapped.Contains("there")) {
            mapped = mapped.Replace("there", "{1}");
        }

        return mapped;
    }
}
