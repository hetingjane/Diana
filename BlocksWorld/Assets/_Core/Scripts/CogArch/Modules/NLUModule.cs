/*
This script parses natural language input into forms VoxSim can work with

Reads:      user:speech (StringValue, transcribed speech)
Writes:     user:intent:event (StringValue, predicate logic form used by VoxSim event manager)

*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using VoxSimPlatform.Global;
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

        if (input.StartsWith("yes"))
        {
            // do posack
            SetValue("user:intent:isPosack", true, string.Empty);
            input = input.Replace("yes", "").Trim();
        }
        else if (input.StartsWith("no"))
        {
            // do negack
            SetValue("user:intent:isNegack", true, string.Empty);
            input = input.Replace("no", "").Trim();
        }
        else if ((input.StartsWith("never mind")) || (input.StartsWith("wait")) ||
            (input.StartsWith("no wait")))
        {
            // do nevermind
            SetValue("user:intent:isNevermind", true, string.Empty);
            input = input.Replace("never mind", "").Replace("wait", "").Replace("no wait", "").Trim();
            SetValue("user:intent:isNevermind", false, string.Empty);
        }

        string mapped = MapTerms(input);
	    Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".",mapped));

        string parsed = communicationsBridge.NLParse(mapped);
        Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".",parsed));

        if (parsed.Length > 0) {
            SetValue("user:intent:partialEvent", parsed, string.Empty);
        }
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
        else if (mapped.Contains("next to")) {
            mapped = mapped.Replace("next to",
                new List<string>() { "left of", "right of" }[RandomHelper.RandomInt(0, 1, (int)RandomHelper.RangeFlags.MaxInclusive)]);
        }

        // insert "one" after "this"/"that" if not already followed by noun
        if (mapped.Split().Contains("this")) {
	        string nextWord = mapped.Split().ToList().IndexOf("this") == mapped.Split().Length - 1 ? 
		        string.Empty : 
		        mapped.Split().ToList()[mapped.Split().ToList().IndexOf("this") + 1];
            string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "one" };
            if (!knownNominals.Contains(nextWord)) {
                mapped = mapped.Replace("this", "this one");
            }
        }
        else if (mapped.Split().Contains("this")) {
	        string nextWord = mapped.Split().Length == 1 ? 
		        string.Empty : 
		        mapped.Split().ToList()[mapped.Split().ToList().IndexOf("this") + 1];
	        string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "one" };
            if (!knownNominals.Contains(nextWord)) {
                mapped = mapped.Replace("this", "this one");
            }
        }

        // add anaphor placeholder
        if (mapped.Split().Contains("one")) {
            mapped = Regex.Replace(mapped, @"\bone\b", "{2}");
        }
        else if (mapped.Split().Contains("ones")) {
            mapped = Regex.Replace(mapped, @"\bones\b", "{2}");
        }

        if (mapped.Split().Contains("it")) {
            mapped = Regex.Replace(mapped, @"\bit\b", "{0}");
        }
        else if (mapped.Split().Contains("them")) {
            mapped = Regex.Replace(mapped, @"\bthem\b", "{0}");
        }

        if (mapped.Contains("there")) {
            mapped = Regex.Replace(mapped, @"\bthere\b", "{1}");
        }
        else if (mapped.Contains("here")) {
            mapped = Regex.Replace(mapped, @"\bhere\b", "{1}");
        }

        return mapped;
    }
}
