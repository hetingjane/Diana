/*
This script interfaces with the VoxSim event manager

Reads:      I don't know yet
Writes:     me:intent:action (StringValue)
            me:intent:targetName (StringValue, name of object that is theme of action)
*/
using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

public class EventManagerTestModule : ModuleBase
{
    public EventManager eventManager;

    // need to keep the same as GrabPlaceModule's value but don't want to couple them
    private readonly Vector3 holdOffset = new Vector3(0f, -.08f, .04f);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (DataStore.GetStringValue("me:holding") != string.Empty)
        {
            GameObject heldObj = GameObject.Find(DataStore.GetStringValue("me:holding"));
            if (heldObj != null)
            {
                Voxeme heldVoxeme = heldObj.GetComponent<Voxeme>();
                if (heldVoxeme != null)
                {
                    // keep target intents up to date with voxeme target position
                    Vector3 curHandPos = DataStore.GetVector3Value("me:actual:handPosR");
                    if (((curHandPos+holdOffset)-heldVoxeme.transform.position).sqrMagnitude > Constants.EPSILON)
                    {
                        Debug.Log("Update handPosR");
                        if (heldVoxeme.interTargetPositions.Count > 0) // a queued path
                        {
                            if (!GlobalHelper.VectorIsNaN(heldVoxeme.interTargetPositions.ElementAt(0)))   // has valid destination
                            {
                                Debug.Log(string.Format("Setting handPosR to {0}",
                                    GlobalHelper.VectorToParsable(heldVoxeme.interTargetPositions.ElementAt(0)+holdOffset)));
                                SetValue("me:intent:handPosR", heldVoxeme.interTargetPositions.ElementAt(0)+holdOffset, string.Empty);
                            }
                        }
                        else
                        {
                            if (!GlobalHelper.VectorIsNaN(heldVoxeme.targetPosition))   // has valid destination
                            {
                                SetValue("me:intent:handPosR", heldVoxeme.targetPosition+holdOffset, string.Empty);
                            }
                        }
                    }
                }
            }
        }
    }

    public void GRASP(object[] args)
    {
        if (args[args.Length - 1] is bool)
        {
            if ((bool) args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    GameObject obj = (args[0] as GameObject);
                    SetValue("me:intent:action", "pickUp", string.Empty);
                    SetValue("me:intent:targetName", obj.name, string.Format("Grasping {0}",obj.name));
                }
            }                    
        }
    }

    public void UNGRASP(object[] args)
    {
        if (args[args.Length - 1] is bool)
        {
            if ((bool) args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    GameObject obj = (args[0] as GameObject);
                    SetValue("me:intent:action", "setDown", string.Empty);
                    SetValue("me:intent:target", obj.transform.position, 
                        string.Format("Ungrasping {0} at {1}", obj.name, GlobalHelper.VectorToParsable(obj.transform.position)));
                }
            }                    
        }
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
            GameObject theme = GameObject.Find(argsStrings[0] as string);

            if (theme != null) {
                if (DataStore.GetStringValue("me:holding") != theme.name) {
                    satisfied = true;
                }
            }
        }

        return satisfied;
    }
}
