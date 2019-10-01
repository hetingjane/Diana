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
    }

    public void UNGRASP(object[] args)
    {
        Debug.Log("Diana's World: I'm ungrasping!");
    }
}
