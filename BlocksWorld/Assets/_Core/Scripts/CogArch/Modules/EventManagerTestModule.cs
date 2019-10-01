/*
This script interfaces with the VoxSim event manager

Reads:      ...
Writes:     ...

*/
using UnityEngine;

using VoxSimPlatform.Core;

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
}
