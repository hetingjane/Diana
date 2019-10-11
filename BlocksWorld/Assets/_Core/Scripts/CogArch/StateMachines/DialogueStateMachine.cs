using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Object = System.Object;
using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Global;
using VoxSimPlatform.Interaction;

public class StackSymbolContent : IEquatable<Object> {
    public object BlackboardState { get; set; }

    public StackSymbolContent(object blackboardState) {
        this.BlackboardState = blackboardState;
    }

    public StackSymbolContent(StackSymbolContent clone) {
        this.BlackboardState = (DataStore) clone.BlackboardState;
    }

    public override bool Equals(object obj) {
        if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
            return false;
        else {
            StackSymbolContent tuple = (StackSymbolContent) obj;

            return (DataStore) BlackboardState == (DataStore) tuple.BlackboardState;
        }
    }

    public override int GetHashCode() {
        return BlackboardState.GetHashCode();
    }

    public static bool operator == (StackSymbolContent tuple1, StackSymbolContent tuple2) {
        return tuple1.Equals(tuple2);
    }

    public static bool operator != (StackSymbolContent tuple1, StackSymbolContent tuple2) {
        return !tuple1.Equals(tuple2);
    }
}

public class StackSymbolConditions : IEquatable<Object> {
    public Expression<Predicate<DataStore>> BlackboardStateCondition { get; set; }

    public StackSymbolConditions(Expression<Predicate<DataStore>> blackboardStateCondition) {
        this.BlackboardStateCondition = blackboardStateCondition;
    }

    public bool SatisfiedBy(object obj) {
        if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
            return false;
        else {
            StackSymbolContent tuple = (StackSymbolContent) obj;

            return ((BlackboardStateCondition == null) ||
                    (BlackboardStateCondition.Compile().Invoke((DataStore) tuple.BlackboardState)));
        }
    }

    public override bool Equals(object obj) {
        if (obj == null || (obj as StackSymbolConditions) == null) //if the object is null or the cast fails
            return false;
        else {
            StackSymbolConditions tuple = (StackSymbolConditions) obj;

            bool equal = true;

            if ((BlackboardStateCondition == null) && (tuple.BlackboardStateCondition == null)) {
                equal &= true;
            }
            else if ((BlackboardStateCondition == null) && (tuple.BlackboardStateCondition != null)) {
                equal &= false;
            }
            else if ((BlackboardStateCondition != null) && (tuple.BlackboardStateCondition == null)) {
                equal &= false;
            }
            else {
                // loath to do this but it should work for now
                equal &= Convert.ToString(BlackboardStateCondition) ==
                         Convert.ToString(tuple.BlackboardStateCondition);
                //equal &= Expression.Lambda<Func<bool>>(Expression.Equal(IndicatedObjCondition, tuple.IndicatedObjCondition)).Compile()();
            }

            Debug.Log(equal);
            return equal;
        }
    }

    public override int GetHashCode() {
        return BlackboardStateCondition.GetHashCode();
    }

    public static bool operator ==(StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
        return tuple1.Equals(tuple2);
    }

    public static bool operator !=(StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
        return !tuple1.Equals(tuple2);
    }
}

public class StateChangeEventArgs : EventArgs {
    public PDAState State { get; set; }

    public StateChangeEventArgs(PDAState state) {
        this.State = state;
    }
}

public class DialogueStateMachine : CharacterLogicAutomaton
{
    public DataStore BlackboardState {
        get {
            return GetCurrentStackSymbol() == null
                ? null
                : (DataStore) ((StackSymbolContent) GetCurrentStackSymbol().Content).BlackboardState;
        }
    }

    bool useOrderingHeuristics;
    bool humanRelativeDirections;
    bool waveToStart;
    bool useEpistemicModel;

    public SingleAgentInteraction scenarioController;

    public event EventHandler ChangeState;

    public void OnChangeState(object sender, EventArgs e) {
        if (ChangeState != null) {
            ChangeState(this, e);
        }
    }

    public object NullObject(object arg) {
        return null;
    }

    public object GetBlackboardState(object arg) {
        return BlackboardState;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DialogueStateMachine))]
    public class DebugPreview : Editor {
        public override void OnInspectorGUI() {
            var bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Attention Status", bold, GUILayout.Width(150));
            //((DianaInteractionLogic) target).attentionStatus =
            //    (AttentionStatus) GUILayout.SelectionGrid((int) ((DianaInteractionLogic) target).attentionStatus,
            //        new string[] {"Inattentive", "Attentive"}, 1, GUILayout.ExpandWidth(true));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Use Ordering Heuristics", bold, GUILayout.Width(150));
            ((DialogueStateMachine) target).useOrderingHeuristics =
                GUILayout.Toggle(((DialogueStateMachine) target).useOrderingHeuristics, "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Human Relative Directions", bold, GUILayout.Width(150));
            ((DialogueStateMachine) target).humanRelativeDirections =
                GUILayout.Toggle(((DialogueStateMachine) target).humanRelativeDirections, "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Wave To Start", bold, GUILayout.Width(150));
            ((DialogueStateMachine) target).waveToStart =
                GUILayout.Toggle(((DialogueStateMachine) target).waveToStart, "");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Use Epistemic Model", bold, GUILayout.Width(150));
            ((DialogueStateMachine) target).useEpistemicModel =
                GUILayout.Toggle(((DialogueStateMachine) target).useEpistemicModel, "");
            GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Repeat After Wait", bold, GUILayout.Width(150));
            //((DianaInteractionLogic) target).repeatAfterWait =
            //    GUILayout.Toggle(((DianaInteractionLogic) target).repeatAfterWait, "");
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Repeat Wait Time", bold, GUILayout.Width(150));
            //((DianaInteractionLogic) target).repeatTimerTime = Convert.ToDouble(
            //    GUILayout.TextField(((DianaInteractionLogic) target).repeatTimerTime.ToString(),
            //        GUILayout.Width(50)));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Servo Wait Time", bold, GUILayout.Width(150));
            //((DianaInteractionLogic) target).servoWaitTimerTime = Convert.ToDouble(
            //    GUILayout.TextField(((DianaInteractionLogic) target).servoWaitTimerTime.ToString(),
            //        GUILayout.Width(50)));
            //GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Servo Loop Time", bold, GUILayout.Width(150));
            //((DianaInteractionLogic) target).servoLoopTimerTime = Convert.ToDouble(
            //    GUILayout.TextField(((DianaInteractionLogic) target).servoLoopTimerTime.ToString(),
            //        GUILayout.Width(50)));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current State", bold, GUILayout.Width(150));
            GUILayout.Label(((DialogueStateMachine) target).CurrentState == null
                ? "Null"
                : ((DialogueStateMachine) target).CurrentState.Name);
            GUILayout.EndHorizontal();

            // some styling for the header, this is optional
            GUILayout.Label("Stack", bold);

            // add a label for each item, you can add more properties
            // you can even access components inside each item and display them
            // for example if every item had a sprite we could easily show it 
            if (((DialogueStateMachine) target).Stack != null) {
                foreach (PDASymbol item in ((DialogueStateMachine) target).Stack) {
                    GUILayout.Label(((DialogueStateMachine) target).StackSymbolToString(item));
                }
            }

            GUILayout.Label("State History", bold);
            if (((DialogueStateMachine) target).StateTransitionHistory != null) {
                foreach (Triple<PDASymbol, PDAState, PDASymbol> item in ((DialogueStateMachine) target)
                    .StateTransitionHistory) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(item.Item1 == null ? "Null" : item.Item1.Name, GUILayout.Width(150));
                    GUILayout.Label(item.Item2 == null ? "Null" : item.Item2.Name, GUILayout.Width(150));
                    GUILayout.Label(item.Item3 == null
                        ? "Null"
                        : ((DialogueStateMachine) target).StackSymbolToString(item.Item3));
                    GUILayout.EndHorizontal();
                }
            }

            //GUILayout.Label("Context Memory", bold);
            //if (((DianaInteractionLogic) target).ContextualMemory != null) {
            //    foreach (Triple<PDASymbol, PDAState, PDASymbol> item in ((DianaInteractionLogic) target)
            //        .ContextualMemory) {
            //        GUILayout.BeginHorizontal();
            //        GUILayout.Label(item.Item1 == null ? "Null" : item.Item1.Name, GUILayout.Width(150));
            //        GUILayout.Label(item.Item2 == null ? "Null" : item.Item2.Name, GUILayout.Width(150));
            //        GUILayout.Label(item.Item3 == null
            //            ? "Null"
            //            : ((DianaInteractionLogic) target).StackSymbolToString(item.Item3));
            //        GUILayout.EndHorizontal();
            //    }
            //}
        }
    }
#endif

    // Use this for initialization
    void Start()
    {
        base.Start();
        
        States.Add(new PDAState("StartState", null));
        States.Add(new PDAState("BeginInteraction", null));
        States.Add(new PDAState("Ready", null));
        States.Add(new PDAState("ModularInteractionLoop", null));
        States.Add(new PDAState("CleanUp", null));
        States.Add(new PDAState("EndState", null));

        TransitionRelation.Add(new PDAInstruction(
            GetStates("StartState"),
            null,
            GenerateStackSymbolFromConditions(
                (b) => b.IGetBoolValue("user:isEngaged", false) == true
            ),
            GetState("BeginInteraction"),
            new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

        TransitionRelation.Add(new PDAInstruction(
            GetStates("BeginInteraction"),
            null,
            GenerateStackSymbolFromConditions(
                (b) => b.IGetBoolValue("user:intent:isWave", false) == true
            ),
            GetState("Ready"),
            new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

        TransitionRelation.Add(new PDAInstruction(
            GetStates("Ready"),
            null,
            GenerateStackSymbolFromConditions(null),
            GetState("ModularInteractionLoop"),
            new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

        TransitionRelation.Add(new PDAInstruction(
            States.Except(new List<PDAState>(new PDAState[] { 
                GetState("StartState"), GetState("CleanUp"), GetState("EndState") })).ToList(),
            null,
            GenerateStackSymbolFromConditions(
                (b) => b.IGetBoolValue("user:isEngaged", false) == false
            ),
            GetState("CleanUp"),
            new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

        TransitionRelation.Add(new PDAInstruction(
            GetStates("CleanUp"),
            null,
            GenerateStackSymbolFromConditions(null),
            GetState("EndState"),
            new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, GetState("EndState"))));

        TransitionRelation.Add(new PDAInstruction(
            GetStates("EndState"),
            null,
            GenerateStackSymbolFromConditions(
                (b) => b.IGetBoolValue("user:isEngaged", true) == true
            ),
            GetState("StartState"),
            new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

        PerformStackOperation(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
            GenerateStackSymbol(DataStore.instance)));
        MoveToState(GetState("StartState"));
    }

    // Update is called once per frame
    void Update()
    {

    }

    void MoveToState(PDAState state) {
        Triple<PDASymbol, PDAState, PDASymbol> symbolStateTriple =
            new Triple<PDASymbol, PDAState, PDASymbol>(GetLastInputSymbol(), state, GetCurrentStackSymbol());

        if (CurrentState != null) {
            if (TransitionRelation.Where(i => (i.FromStates.Contains(CurrentState)) && (i.ToState == state))
                    .ToList().Count == 0) {
                Debug.Log(string.Format("No transition arc between state {0} and state {1}.  Aborting.",
                    CurrentState.Name, state.Name));
                return;
            }

            if (state.Name == "BeginInteraction") {
                //epistemicModel.state.InitiateEpisim();
                StateTransitionHistory.Push(symbolStateTriple);
                ContextualMemory.Push(symbolStateTriple);
            }
            else {
                StateTransitionHistory.Push(symbolStateTriple);
                ContextualMemory.Push(symbolStateTriple);
            }
        }
        else {
            StateTransitionHistory.Push(symbolStateTriple);
            ContextualMemory.Push(symbolStateTriple);
        }

	    Debug.Log(string.Format("Entering state {0} from state {1}.  Stack symbol: {2}",
		    state == null ? "Null" : state.Name,
		    CurrentState == null ? "Null" : CurrentState.Name,
		    StackSymbolToString(GetCurrentStackSymbol())));
		    
        CurrentState = state;
        OnChangeState(this, new StateChangeEventArgs(CurrentState));

        //if ((repeatAfterWait) && (repeatTimerTime > 0)) {
        //    repeatTimer.Interval = repeatTimerTime;
        //    repeatTimer.Enabled = true;
        //}
    }

    public PDASymbol GenerateStackSymbol(object blackboardState,
        bool overwriteCurrentSymbol = false, string name = "New Stack Symbol") {

        StackSymbolContent symbolContent =
            new StackSymbolContent(
                blackboardState == null
                    ? (DataStore) GetBlackboardState(null)
                    : blackboardState.GetType() == typeof(DelegateFactory)
                        ? ((DelegateFactory) blackboardState).Function
                        : blackboardState.GetType() == typeof(FunctionDelegate)
                            ? (DataStore) ((FunctionDelegate) blackboardState).Invoke(null)
                            : (DataStore) blackboardState);

        PDASymbol symbol = new PDASymbol(symbolContent);

        return symbol;
    }

    public PDASymbol GenerateStackSymbol(StackSymbolContent content, string name = "New Stack Symbol") {
        StackSymbolContent symbolContent =
            new StackSymbolContent(
                content.BlackboardState == null
                    ? (DataStore) GetBlackboardState(null)
                    : content.BlackboardState.GetType() == typeof(DelegateFactory)
                        ? ((DelegateFactory) content.BlackboardState).Function
                        : content.BlackboardState.GetType() == typeof(FunctionDelegate)
                            ? (DataStore) ((FunctionDelegate) content.BlackboardState).Invoke(null)
                            : (DataStore) content.BlackboardState);

        PDASymbol symbol = new PDASymbol(symbolContent);

        return symbol;
    }
        
    public PDASymbol GenerateStackSymbolFromConditions(
        Expression<Predicate<DataStore>> blackboardStateCondition,
        string name = "New Stack Symbol") {
        StackSymbolConditions symbolConditions =
            new StackSymbolConditions(null);

        symbolConditions.BlackboardStateCondition = blackboardStateCondition;

        PDASymbol symbol = new PDASymbol(symbolConditions);

        return symbol;
    }

    public string StackSymbolToString(object stackSymbol) {
        //Debug.Log (stackSymbol.GetType ());
        if (stackSymbol == null) {
            return "[]";
        }
        else if (stackSymbol.GetType() == typeof(PDASymbol)) {
            PDASymbol symbol = (PDASymbol) stackSymbol;
            if (symbol.Content == null) {
                return "[]";
            }
            else if (symbol.Content.GetType() == typeof(StackSymbolContent)) {
                StackSymbolContent content = (StackSymbolContent) symbol.Content;

                return string.Format("[{0}]",
                    content.BlackboardState == null
                        ? "Null"
                        : content.BlackboardState.ToString()
                    );
            }
            else if (symbol.Content.GetType() == typeof(StackSymbolConditions)) {
                StackSymbolConditions content = (StackSymbolConditions) symbol.Content;

                return string.Format("[{0}]",
                    content.BlackboardStateCondition == null
                        ? "Null"
                        : content.BlackboardStateCondition.ToString()
                    );
            }
            else if (symbol.Content.GetType() == typeof(PDAState)) {
                return ((PDAState) symbol.Content).Name;
            }
        }
        else if (stackSymbol.GetType() == typeof(StackSymbolContent)) {
            StackSymbolContent content = (StackSymbolContent) stackSymbol;

            return string.Format("[{0}]",
                    content.BlackboardState == null
                        ? "Null"
                        : content.BlackboardState.ToString()
                    );
        }
        else if (stackSymbol.GetType() == typeof(FunctionDelegate)) {
            return string.Format(":{0}", ((FunctionDelegate) stackSymbol).Method.Name);
        }

        return string.Empty;
    }

    void PerformStackOperation(PDAStackOperation operation) {
        switch (operation.Type) {
            case PDAStackOperation.PDAStackOperationType.None:
                break;

            case PDAStackOperation.PDAStackOperationType.Pop:
                if ((operation.Content == null) || (operation.Content.GetType() != typeof(PDAState))) {
                    Stack.Pop();
                    ContextualMemory.Pop();
                }
                else {
                    PDASymbol popUntilSymbol = ContextualMemory.First().Item3;
                    foreach (Triple<PDASymbol, PDAState, PDASymbol> symbolStateTriple in ContextualMemory.ToList()
                        .GetRange(1, ContextualMemory.Count - 2)) {
                        Debug.Log(string.Format("{0} {1}", symbolStateTriple.Item3.Name,
                            StackSymbolToString(symbolStateTriple.Item3)));
                        if ((symbolStateTriple.Item2 == (PDAState) operation.Content) &&
                            (symbolStateTriple.Item3 != GetCurrentStackSymbol())) {
                            // if state == operation content && stack symbol != current stack symbol
                            popUntilSymbol = symbolStateTriple.Item3;
                            break;
                        }
                    }

                    Debug.Log(string.Format(StackSymbolToString(popUntilSymbol)));
                    while (Stack.Count > 1 &&
                           !((StackSymbolContent) GetCurrentStackSymbol().Content).Equals(
                               (StackSymbolContent) popUntilSymbol.Content)) {
                        Debug.Log(string.Format("Popping {0} until {1}",
                            StackSymbolToString(GetCurrentStackSymbol()), StackSymbolToString(popUntilSymbol)));
                        Stack.Pop();
                        ContextualMemory.Pop();
                    }
                }

                break;

            case PDAStackOperation.PDAStackOperationType.Push:
                if (operation.Content.GetType() == typeof(FunctionDelegate)) {
                    object content = ((FunctionDelegate) operation.Content).Invoke(null);
                    Debug.Log(content.GetType());
                    foreach (PDASymbol symbol in (List<PDASymbol>) content) {
                        Debug.Log(StackSymbolToString((PDASymbol) symbol));
                    }

                    if (content.GetType() == typeof(PDASymbol)) {
                        // When we push a new Stack symbol we should clone the CurrentStackSymbol and check the conditions below to adjust the values
                        //PDASymbol pushSymbol = (PDASymbol)content;

                        Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) content).Content));
                    }
                    else if ((content is IList) && (content.GetType().IsGenericType) &&
                             (content.GetType().IsAssignableFrom(typeof(List<PDASymbol>)))) {
                        foreach (PDASymbol symbol in (List<PDASymbol>) content) {
                            //Debug.Log (((StackSymbolContent)symbol.Content).IndicatedObj);
                            Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) symbol).Content));
                        }
                    }
                }
                else if (operation.Content.GetType() == typeof(PDASymbol)) {
                    Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) operation.Content).Content));
                }
                else if ((operation.Content is IList) && (operation.Content.GetType().IsGenericType) &&
                         (operation.Content.GetType().IsAssignableFrom(typeof(List<PDASymbol>)))) {
                    foreach (PDASymbol symbol in (List<PDASymbol>) operation.Content) {
                        Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) symbol).Content));
                    }
                }
                else if (operation.Content.GetType() == typeof(StackSymbolContent)) {
                    Stack.Push(GenerateStackSymbol((StackSymbolContent) operation.Content));
                }
                else if ((operation.Content is IList) && (operation.Content.GetType().IsGenericType) &&
                         (operation.Content.GetType().IsAssignableFrom(typeof(List<StackSymbolContent>)))) {
                    foreach (StackSymbolContent symbol in (List<StackSymbolContent>) operation.Content) {
                        Stack.Push(GenerateStackSymbol((StackSymbolContent) symbol));
                    }
                }

                break;

            case PDAStackOperation.PDAStackOperationType.Rewrite:
                RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
                break;

            case PDAStackOperation.PDAStackOperationType.Flush:
                if (operation.Content != null) {
                    if (operation.Content.GetType() == typeof(StackSymbolContent)) {
                        Stack.Clear();
                        Stack.Push(GenerateStackSymbol((StackSymbolContent) operation.Content));
                        //ContextualMemory.Clear ();
                        //ContextualMemory.Push(
                        //new Triple<PDASymbol,PDAState,PDASymbol>(GetLastInputSymbol(),CurrentState,
                        //GenerateStackSymbol((StackSymbolContent)operation.Content)));
                    }
                    else if (operation.Content.GetType() == typeof(PDAState)) {
                        if (((PDAState) operation.Content) == GetState("EndState")) {
                            Stack.Clear();
	                        Stack.Push(GenerateStackSymbol(new StackSymbolContent(null as object)));
                            ContextualMemory.Clear();
                            ContextualMemory.Push(new Triple<PDASymbol, PDAState, PDASymbol>(null,
                                GetState("StartState"),
	                            GenerateStackSymbol(new StackSymbolContent(null as object))));
                            //GenerateStackSymbol((StackSymbolContent)operation.Content)));
                        }
                    }
                }

                break;

            default:
                break;
        }

	    //Debug.Log(string.Format("PerformStackOperation: {0} result {1}", operation.Type,
	    //    StackSymbolToString(GetCurrentStackSymbol())));
    }

    List<PDAInstruction> GetApplicableInstructions(PDAState fromState, PDASymbol inputSymbol, object stackSymbol) {
	    //Debug.Log(fromState.Name);
	    //Debug.Log(inputSymbol == null ? "Null" : inputSymbol.Name);
	    //Debug.Log(string.Format("Stack symbol: {0}", StackSymbolToString(stackSymbol)));
        //foreach (PDASymbol element in Stack) {
        //    Debug.Log(StackSymbolToString(element));
        //}

        List<PDAInstruction> instructions = TransitionRelation.Where(i =>
            (i.FromStates == null && fromState == null) ||
            (i.FromStates != null && i.FromStates.Contains(fromState))).ToList();
        instructions = instructions.Where(i =>
            (i.InputSymbols == null && inputSymbol == null) ||
            (i.InputSymbols != null && i.InputSymbols.Contains(inputSymbol))).ToList();

	    //Debug.Log(string.Format("{0} instructions from {1} with {2}", instructions.Count, fromState.Name,
	    //    inputSymbol == null ? "Null" : inputSymbol.Name));

        //foreach (PDAInstruction inst in instructions) {
        //    Debug.Log(string.Format("{0},{1},{2},{3},{4}",
        //        (inst.FromStates == null)
        //            ? "Null"
        //            : string.Format("[{0}]",
        //                String.Join(", ", ((List<PDAState>) inst.FromStates).Select(s => s.Name).ToArray())),
        //        (inst.InputSymbols == null)
        //            ? "Null"
        //            : string.Format("[{0}]",
        //                String.Join(", ",
        //                    ((List<PDASymbol>) inst.InputSymbols).Select(s => s.Content.ToString()).ToArray())),
        //        StackSymbolToString(inst.StackSymbol),
        //        inst.ToState.Name,
        //        string.Format("[{0},{1}]",
        //            inst.StackOperation.Type.ToString(),
        //            (inst.StackOperation.Content == null)
        //                ? "Null"
        //                : (inst.StackOperation.Content.GetType() == typeof(StackSymbolContent))
        //                    ? StackSymbolToString(inst.StackOperation.Content)
        //                    : (inst.StackOperation.Content.GetType() == typeof(PDAState))
        //                        ? ((PDAState) inst.StackOperation.Content).Name
        //                        : string.Empty)));
        //}

        //Debug.Log(string.Format("{0} instructions before symbol + gate filtering", instructions.Count));
        //Debug.Log(stackSymbol.GetType());

        if (stackSymbol.GetType() == typeof(StackSymbolContent)) {
            //instructions = instructions.Where (i => (i.StackSymbol.Content.GetType() == typeof(StackSymbolContent))).ToList();
            //Debug.Log (instructions.Count);
            instructions = instructions.Where(i =>
                    ((i.StackSymbol.Content.GetType() == typeof(StackSymbolContent)) &&
                     (i.StackSymbol.Content as StackSymbolContent) == (stackSymbol as StackSymbolContent)) ||
                    ((i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions)) &&
                     (i.StackSymbol.Content as StackSymbolConditions).SatisfiedBy(stackSymbol as StackSymbolContent)
                    ))
                .ToList();
        }

        instructions = instructions.Where(i => !(instructions.Where(j => ((j.ToState.Content != null) &&
                                                                          (j.ToState.Content.GetType() ==
                                                                           typeof(TransitionGate)))).Select(j =>
            ((TransitionGate) j.ToState.Content).RejectState).ToList()).Contains(i.ToState)).ToList();
        //          else if (stackSymbol.GetType () == typeof(StackSymbolConditions)) {
        //              instructions = instructions.Where (i => (i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions))).ToList();
        //              instructions = instructions.Where (i => ((i.StackSymbol.Content as StackSymbolConditions) == (stackSymbol as StackSymbolConditions))).ToList();
        //          }

        //          Debug.Log (instructions.Count);

        //Debug.Log(string.Format("{0} instructions after symbol + gate filtering", instructions.Count));

        return instructions;
    }

    public void RewriteStack(PDAStackOperation operation) {
        if (operation.Type != PDAStackOperation.PDAStackOperationType.Rewrite) {
            return;
        }
        else {
            if (operation.Content != null) {
	            PDASymbol symbol = Stack.Pop();
	            //Debug.Log(string.Format("Popped symbol {0}", StackSymbolToString(symbol)));
                //Stack.Push ((PDASymbol)operation.Content);

                // if the symbol you just popped has a null parameter
                //  and the operation content has the same null parameter
                //  but the new stack symbol (before the rewrite -> push) does not have a null parameter there
                if (GetCurrentStackSymbol() != null) {
                    if (operation.Content.GetType() == typeof(PDASymbol)) {
                        if ((((StackSymbolContent) symbol.Content).BlackboardState == null) &&
                            (((StackSymbolContent) ((PDASymbol) operation.Content).Content).BlackboardState == null) &&
                            (((StackSymbolContent) GetCurrentStackSymbol().Content).BlackboardState != null)) {
                            ((StackSymbolContent) ((PDASymbol) operation.Content).Content).BlackboardState =
                                new FunctionDelegate(NullObject);
                        }
                    }
                    else if ((operation.Content is IList) && (operation.Content.GetType().IsGenericType) &&
                             (operation.Content.GetType().IsAssignableFrom(typeof(List<StackSymbolContent>)))) {
                        if ((((StackSymbolContent) symbol.Content).BlackboardState == null) &&
                            (((List<StackSymbolContent>) operation.Content)[0].BlackboardState == null) &&
                            (((StackSymbolContent) GetCurrentStackSymbol().Content).BlackboardState != null)) {
                            ((List<StackSymbolContent>) operation.Content)[0].BlackboardState =
                                new FunctionDelegate(NullObject);
                        }
                    }
                }

                PerformStackOperation(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
                    operation.Content));
            }

	        //Debug.Log(string.Format("RewriteStack: {0} result {1}", operation.Type,
	        //    StackSymbolToString(GetCurrentStackSymbol())));

            // handle state transitions on stack rewrite

            List<PDAInstruction> instructions = GetApplicableInstructions(CurrentState, null,
                GetCurrentStackSymbol().Content);

            PDAInstruction instruction = null;

            if (instructions.Count > 1) {
                Debug.Log(string.Format("Multiple instruction condition ({0}).  Aborting.", instructions.Count));
                foreach (PDAInstruction inst in instructions) {
                    Debug.Log(string.Format("{0},{1},{2},{3},{4}",
                        (inst.FromStates == null)
                            ? "Null"
                            : string.Format("[{0}]",
                                String.Join(", ",
                                    ((List<PDAState>) inst.FromStates).Select(s => s.Name).ToArray())),
                        (inst.InputSymbols == null)
                            ? "Null"
                            : string.Format("[{0}]",
                                String.Join(", ",
                                    ((List<PDASymbol>) inst.InputSymbols).Select(s => s.Content.ToString())
                                    .ToArray())),
                        StackSymbolToString(inst.StackSymbol),
                        inst.ToState.Name,
                        string.Format("[{0},{1}]",
                            inst.StackOperation.Type.ToString(),
                            (inst.StackOperation.Content == null)
                                ? "Null"
                                : (inst.StackOperation.Content.GetType() == typeof(StackSymbolContent))
                                    ? StackSymbolToString(inst.StackOperation.Content)
                                    : (inst.StackOperation.Content.GetType() == typeof(PDAState))
                                        ? ((PDAState) inst.StackOperation.Content).Name
                                        : (inst.StackOperation.Content.GetType() == typeof(FunctionDelegate))
                                            ? ((FunctionDelegate) inst.StackOperation.Content).Method.Name
                                            : string.Empty)));
                }

                return;
            }
            else if (instructions.Count == 1) {
                instruction = instructions[0];
                Debug.Log(string.Format("{0},{1},{2},{3},{4}",
                    (instruction.FromStates == null)
                        ? "Null"
                        : string.Format("[{0}]",
                            String.Join(", ",
                                ((List<PDAState>) instruction.FromStates).Select(s => s.Name).ToArray())),
                    (instruction.InputSymbols == null)
                        ? "Null"
                        : string.Format("[{0}]",
                            String.Join(", ",
                                ((List<PDASymbol>) instruction.InputSymbols).Select(s => s.Content.ToString())
                                .ToArray())),
                    StackSymbolToString(instruction.StackSymbol),
                    instruction.ToState.Name,
                    string.Format("[{0},{1}]",
                        instruction.StackOperation.Type.ToString(),
                        (instruction.StackOperation.Content == null)
                            ? "Null"
                            : (instruction.StackOperation.Content.GetType() == typeof(StackSymbolContent))
                                ? StackSymbolToString(instruction.StackOperation.Content)
                                : (instruction.StackOperation.Content.GetType() == typeof(PDAState))
                                    ? ((PDAState) instruction.StackOperation.Content).Name
                                    : (instruction.StackOperation.Content.GetType() == typeof(FunctionDelegate))
                                        ? ((FunctionDelegate) instruction.StackOperation.Content).Method.Name
                                        : string.Empty)));
            }
            //else if (instructions.Count < 1) {
            //    Debug.Log("Zero instruction condition.  Aborting.");
            //    return;
            //}

            if (instruction != null) {
                MoveToState(instruction.ToState);
                PerformStackOperation(instruction.StackOperation);
                ExecuteStateContent();
            }
        }
    }

    void ExecuteStateContent(object tempMessage = null) {
	    //Debug.Log(scenarioController);
	    //Debug.Log(scenarioController.GetType());
	    //Debug.Log(CurrentState.Name);
	    //Debug.Log(scenarioController.GetType().GetMethod(CurrentState.Name));
	    DialogueInteractionModule dialogueModule = scenarioController.gameObject.GetComponent<DialogueInteractionModule>();
	    MethodInfo methodToCall = dialogueModule.GetType().GetMethod(CurrentState.Name);
        List<object> contentMessages = new List<object>();

        contentMessages.Add(tempMessage);

        if (methodToCall != null) {
            Debug.Log("MoveToState: invoke " + methodToCall.Name);
	        object obj = methodToCall.Invoke(dialogueModule, new object[] {contentMessages.ToArray()});
        }
        else {
            Debug.Log(string.Format("No method of name {0} on object {1}", CurrentState.Name, scenarioController));
        }
    }
}
