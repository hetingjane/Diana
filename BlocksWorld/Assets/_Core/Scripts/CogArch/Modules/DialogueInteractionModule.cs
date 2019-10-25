using UnityEngine;
using System.Timers;

using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Global;
using VoxSimPlatform.Interaction;
using VoxSimPlatform.Vox;

public class DialogueInteractionModule : ModuleBase
{
    public DialogueStateMachine stateMachine;

    public double servoLoopTimerTime = 10;

    public Transform grabbableBlocks;
    public GameObject demoSurface;

    public VoxMLLibrary voxmlLibrary;

    Timer servoLoopTimer;

    bool checkServoStatus = false;

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

        if (servoLoopTimerTime > 0) {
            servoLoopTimer = new Timer(servoLoopTimerTime);
            servoLoopTimer.Enabled = false;
            servoLoopTimer.Elapsed += CheckServoStatus;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (checkServoStatus)
        {
            SetValue("me:isCheckingServo", true, string.Empty);
            SetValue("me:isInServoLoop", true, string.Empty);
            checkServoStatus = false;
        }
    }

    protected override void ValueChanged(string key)
    {
        //Debug.Log(string.Format("Diana's World: {0} changed",key));
        stateMachine.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
            stateMachine.GenerateStackSymbol(DataStore.instance)));

        if (DataStore.GetBoolValue("user:isInteracting")) {
            if (key == "user:intent:isNevermind") {
                if (DataStore.GetBoolValue(key)) {
                    if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:lastEvent"))) {
                        string lastEventStr = DataStore.GetStringValue("user:intent:lastEvent");
                    //if (GlobalHelper.GetTopPredicate(lastEventStr) 

                        UndoLastEvent();
                    }
                    else if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object"))) {
                        ForgetFocusObject();
                    }
                }
            } 
            else if (key == "user:intent:isPushLeft") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "slide({0},{1}(left))", string.Empty);
                }
            }
            else if (key == "user:intent:isPushRight") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "slide({0},{1}(right))", string.Empty);
                }
            }
            else if (key == "user:intent:isPushFront") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "slide({0},{1}(front))", string.Empty);
                }
            }
            else if (key == "user:intent:isPushBack") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "slide({0},{1}(back))", string.Empty);
                }
            }
            else if (key == "user:intent:isServoLeft") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "servo({0},{1}(left))", string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = true;
                }
                else {
                    SetValue("user:intent:append:action", "ungrasp({0})", string.Empty);
                    SetValue("user:intent:action", string.Empty, string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);
                    SetValue("me:isInServoLoop", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = false;
                }
            }
            else if (key == "user:intent:isServoRight") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "servo({0},{1}(right))", string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = true;
                }
                else {
                    SetValue("user:intent:append:action", "ungrasp({0})", string.Empty);
                    SetValue("user:intent:action", string.Empty, string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);
                    SetValue("me:isInServoLoop", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = false;
                }
            }
            else if (key == "user:intent:isServoFront") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "servo({0},{1}(front))", string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = true;
                }
                else {
                    SetValue("user:intent:append:action", "ungrasp({0})", string.Empty);
                    SetValue("user:intent:action", string.Empty, string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);
                    SetValue("me:isInServoLoop", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = false;
                }
            }
            else if (key == "user:intent:isServoBack") {
                if (DataStore.GetBoolValue(key)) {
                    SetValue("user:intent:action", "servo({0},{1}(back))", string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = true;
                }
                else {
                    SetValue("user:intent:append:action", "ungrasp({0})", string.Empty);
                    SetValue("user:intent:action", string.Empty, string.Empty);
                    SetValue("me:isCheckingServo", false, string.Empty);
                    SetValue("me:isInServoLoop", false, string.Empty);

                    servoLoopTimer.Interval = servoLoopTimerTime;
                    servoLoopTimer.Enabled = false;
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
                if (!string.IsNullOrEmpty(DataStore.GetStringValue(key))) {
                    GameObject target = GameObject.Find(DataStore.GetStringValue(key));
                    GameObject blocker = null;

                    if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")) &&
                        DialogueUtility.SurfaceClear(target, out blocker)) {
                        SetValue("user:intent:object", DataStore.GetStringValue(key), string.Empty);
                    }
                    else if (DataStore.GetStringValue("user:intent:object") != DataStore.GetStringValue(key)) {
                        do
                        {
                            DialogueUtility.SurfaceClear(target, out blocker);
                            if (blocker != null)
                            {
                                target = blocker;
                            }
                        } while (blocker != null);
                            SetValue("user:intent:partialEvent",
                                string.Format("put({0},on({1}))",
                                DataStore.GetStringValue("user:intent:object"),
                                target.name), string.Empty);
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
            else if (key == "user:intent:object") {
                if (!string.IsNullOrEmpty(DataStore.GetStringValue(key))) {
                    if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:action")) && 
                        DataStore.GetVector3Value("user:intent:location") == default) {
                        //SetValue("me:speech:intent", "OK.", string.Empty);
                        SetValue("me:intent:action", "reach", string.Empty);
                        SetValue("me:intent:target",
                           GlobalHelper.GetObjectWorldSize(GameObject.Find(DataStore.GetStringValue(key))).max, string.Empty);
                        SetValue("me:intent:targetName", DataStore.GetStringValue(key), string.Empty);
                    }
                }
            }
            else if (key == "user:communication") {
                CommunicationValue comm = DataStore.GetValue("user:communication") as CommunicationValue;
    	        Debug.Log(string.Format("User communication: {0} {1} {2} {3}", comm.val, comm.val.directAddress, comm.val.parse, comm.val.originalText));
            }
        }
    }

    void UndoLastEvent() {
        string lastEventStr = DataStore.GetStringValue("user:intent:lastEvent");
        string undoEventStr = string.Empty;

        Debug.Log(string.Format("Undo: Undoing last event {0}", lastEventStr));

        switch(GlobalHelper.GetTopPredicate(lastEventStr)) {
            case "grasp":
                undoEventStr = lastEventStr.Replace("grasp", "ungrasp");
                break;

            case "lift":
                undoEventStr = string.Format("put({0},{1})",
                    DataStore.GetStringValue("me:lastTheme"),
                    GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                break;

            case "put":
                if (DataStore.GetVector3Value("me:lastThemePos") != default) {
                    undoEventStr = string.Format("put({0},{1})",
                        DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                }
                else {
                    SetValue("me:speech:intent", "I already undid that.  What else would you like me to do?", string.Empty);
                }
                break;

            case "slide":
                if (DataStore.GetVector3Value("me:lastThemePos") != default) {
                    undoEventStr = string.Format("slide({0},{1})",
                        DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                }
                else {
                    SetValue("me:speech:intent", "I already undid that.  What else would you like me to do?", string.Empty);
                }
                break;

            case "servo":
                if (DataStore.GetVector3Value("me:lastThemePos") != default) {
                    undoEventStr = string.Format("slide({0},{1})",
                        DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                }
                else {
                    SetValue("me:speech:intent", "I already undid that.  What else would you like me to do?", string.Empty);
                }
                break;

            default:
                break;
        }

        if (!string.IsNullOrEmpty(undoEventStr)) {
            SetValue("me:isUndoing", true, string.Empty);
            SetValue("user:intent:event", undoEventStr, string.Empty);
        }
    }

    void ForgetFocusObject() {
        Debug.Log(string.Format("Undo: forgetting focus object {0}", DataStore.GetBoolValue("user:intent:object")));
        SetValue("user:intent:object", string.Empty, string.Empty);
        SetValue("me:intent:action", "unreach", string.Empty);
        SetValue("me:intent:targetName", "unreach", string.Empty);
    }

    void CheckServoStatus(object sender, ElapsedEventArgs e) {
        checkServoStatus = true;

        servoLoopTimer.Interval = servoLoopTimerTime;
        servoLoopTimer.Enabled = true;
    }

    public void BeginInteraction(object content) {
        SetValue("me:speech:intent", "Hello.", string.Empty);
        // TODO: this animation looks like a crazy alien wave
        // no really, it terrified me to the pit of my soul when I first saw it
        //SetValue("me:intent:motion:rightArm", "wave", string.Empty);
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