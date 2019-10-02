/*
This script interfaces with the VoxSim event manager

Reads:      I don't know yet
Writes:     me:intent:action (StringValue)
            me:intent:targetName (StringValue, name of object that is theme of action)
*/
using UnityEngine;
using System.Collections;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;

public class EventManagerTestModule : ModuleBase
{
    public EventManager eventManager;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GRASP(object[] args)
    {
        Debug.Log("Diana's World: I'm grasping!");

        if (args[args.Length - 1] is bool)
        {
            if ((bool) args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    GameObject obj = (args[0] as GameObject);
                    SetValue("me:intent:action", "pickUp", string.Empty);
                    SetValue("me:intent:targetName", obj.name, comment);
                }
            }                    
        }
    }

    public void UNGRASP(object[] args)
    {
        Debug.Log("Diana's World: I'm ungrasping!");
    }

    public bool IsSatisfied(string test) {
        bool satisfied = false;

        Hashtable predArgs = GlobalHelper.ParsePredicate(test);
        string predString = "";
        string[] argsStrings = null;

        foreach (DictionaryEntry entry in predArgs) {
            predString = (string) entry.Key;
            argsStrings = ((string) entry.Value).Split(',');
        }

        if (predString == "grasp") {
            GameObject theme = GameObject.Find(argsStrings[0] as string);

            if (theme != null) {
                if (DataStore.GetStringValue("me:holding") == theme.name) {
                    satisfied = true;
                }
            }
        }
        else if (predString == "ungrasp") {
        }

        return satisfied;
    }
}
