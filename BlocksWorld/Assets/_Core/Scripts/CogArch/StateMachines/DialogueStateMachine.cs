using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Object = System.Object;
using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Global;
using VoxSimPlatform.Interaction;

public class StackSymbolContent : IEquatable<Object> {
    public object IndicatedObj { get; set; }

    public object GraspedObj { get; set; }

    public object IndicatedRegion { get; set; }

    public object ObjectOptions { get; set; }

    public object ActionOptions { get; set; }

    public object ActionSuggestions { get; set; }

    public StackSymbolContent(object indicatedObj, object graspedObj, object indicatedRegion,
        object objectOptions, object actionOptions, object actionSuggestions) {
        this.IndicatedObj = indicatedObj;
        this.GraspedObj = graspedObj;
        this.IndicatedRegion = indicatedRegion;
        this.ObjectOptions = objectOptions;
        this.ActionOptions = actionOptions;
        this.ActionSuggestions = actionSuggestions;
    }

    public StackSymbolContent(StackSymbolContent clone) {
        this.IndicatedObj = (GameObject) clone.IndicatedObj;
        this.GraspedObj = (GameObject) clone.GraspedObj;
        this.IndicatedRegion = (clone.IndicatedRegion != null) ? new Region((Region) clone.IndicatedRegion) : null;
        this.ObjectOptions = (clone.ObjectOptions != null)
            ? new List<GameObject>((List<GameObject>) clone.ObjectOptions)
            : null;
        this.ActionOptions = (clone.ActionOptions != null)
            ? new List<string>((List<string>) clone.ActionOptions)
            : null;
        this.ActionSuggestions = (clone.ActionSuggestions != null)
            ? new List<string>((List<string>) clone.ActionSuggestions)
            : null;
    }

    public override bool Equals(object obj) {
        if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
            return false;
        else {
            StackSymbolContent tuple = (StackSymbolContent) obj;

            return (GameObject) IndicatedObj == (GameObject) tuple.IndicatedObj &&
                   (GameObject) GraspedObj == (GameObject) tuple.GraspedObj &&
                   GlobalHelper.RegionsEqual((Region) IndicatedRegion, (Region) tuple.IndicatedRegion) &&
                   ((List<GameObject>) ObjectOptions).SequenceEqual((List<GameObject>) tuple.ObjectOptions) &&
                   ((List<string>) ActionOptions).SequenceEqual((List<string>) tuple.ActionOptions) &&
                   ((List<string>) ActionSuggestions).SequenceEqual((List<string>) tuple.ActionSuggestions);
        }
    }

    public override int GetHashCode() {
        return IndicatedObj.GetHashCode() ^ GraspedObj.GetHashCode() ^
               IndicatedRegion.GetHashCode() ^ ObjectOptions.GetHashCode() ^
               ActionOptions.GetHashCode() ^ ActionSuggestions.GetHashCode();
    }

    public static bool operator == (StackSymbolContent tuple1, StackSymbolContent tuple2) {
        return tuple1.Equals(tuple2);
    }

    public static bool operator != (StackSymbolContent tuple1, StackSymbolContent tuple2) {
        return !tuple1.Equals(tuple2);
    }
}

public class StackSymbolConditions : IEquatable<Object> {
    public Expression<Predicate<GameObject>> IndicatedObjCondition { get; set; }

    public Expression<Predicate<GameObject>> GraspedObjCondition { get; set; }

    public Expression<Predicate<Region>> IndicatedRegionCondition { get; set; }

    public Expression<Predicate<List<GameObject>>> ObjectOptionsCondition { get; set; }

    public Expression<Predicate<List<string>>> ActionOptionsCondition { get; set; }

    public Expression<Predicate<List<string>>> ActionSuggestionsCondition { get; set; }

    public StackSymbolConditions(Expression<Predicate<GameObject>> indicatedObjCondition,
        Expression<Predicate<GameObject>> graspedObjCondition,
        Expression<Predicate<Region>> indicatedRegionCondition,
        Expression<Predicate<List<GameObject>>> objectOptionsCondition,
        Expression<Predicate<List<string>>> actionOptionsCondition,
        Expression<Predicate<List<string>>> actionSuggestionsCondition) {
        this.IndicatedObjCondition = indicatedObjCondition;
        this.GraspedObjCondition = graspedObjCondition;
        this.IndicatedRegionCondition = indicatedRegionCondition;
        this.ObjectOptionsCondition = objectOptionsCondition;
        this.ActionOptionsCondition = actionOptionsCondition;
        this.ActionSuggestionsCondition = actionSuggestionsCondition;
    }

    public bool SatisfiedBy(object obj) {
        if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
            return false;
        else {
            StackSymbolContent tuple = (StackSymbolContent) obj;

            return ((IndicatedObjCondition == null) ||
                    (IndicatedObjCondition.Compile().Invoke((GameObject) tuple.IndicatedObj))) &&
                   ((GraspedObjCondition == null) ||
                    (GraspedObjCondition.Compile().Invoke((GameObject) tuple.GraspedObj))) &&
                   ((IndicatedRegionCondition == null) ||
                    (IndicatedRegionCondition.Compile().Invoke((Region) tuple.IndicatedRegion))) &&
                   ((ObjectOptionsCondition == null) || (ObjectOptionsCondition.Compile()
                        .Invoke((List<GameObject>) tuple.ObjectOptions))) &&
                   ((ActionOptionsCondition == null) ||
                    (ActionOptionsCondition.Compile().Invoke((List<string>) tuple.ActionOptions))) &&
                   ((ActionSuggestionsCondition == null) || (ActionSuggestionsCondition.Compile()
                        .Invoke((List<string>) tuple.ActionSuggestions)));
        }
    }

    public override bool Equals(object obj) {
        if (obj == null || (obj as StackSymbolConditions) == null) //if the object is null or the cast fails
            return false;
        else {
            StackSymbolConditions tuple = (StackSymbolConditions) obj;

            bool equal = true;

            if ((IndicatedObjCondition == null) && (tuple.IndicatedObjCondition == null)) {
                equal &= true;
            }
            else if ((IndicatedObjCondition == null) && (tuple.IndicatedObjCondition != null)) {
                equal &= false;
            }
            else if ((IndicatedObjCondition != null) && (tuple.IndicatedObjCondition == null)) {
                equal &= false;
            }
            else {
                // loath to do this but it should work for now
                equal &= Convert.ToString(IndicatedObjCondition) ==
                         Convert.ToString(tuple.IndicatedObjCondition);
                //equal &= Expression.Lambda<Func<bool>>(Expression.Equal(IndicatedObjCondition, tuple.IndicatedObjCondition)).Compile()();
            }

            if ((GraspedObjCondition == null) && (tuple.GraspedObjCondition == null)) {
                equal &= true;
            }
            else if ((GraspedObjCondition == null) && (tuple.GraspedObjCondition != null)) {
                equal &= false;
            }
            else if ((GraspedObjCondition != null) && (tuple.GraspedObjCondition == null)) {
                equal &= false;
            }
            else {
                equal &= Convert.ToString(GraspedObjCondition) ==
                         Convert.ToString(tuple.GraspedObjCondition);
                //equal &= Expression.Lambda<Func<bool>>(Expression.Equal(GraspedObjCondition, tuple.GraspedObjCondition)).Compile()();
            }

            if ((IndicatedRegionCondition == null) && (tuple.IndicatedRegionCondition == null)) {
                equal &= true;
            }
            else if ((IndicatedRegionCondition == null) && (tuple.IndicatedRegionCondition != null)) {
                equal &= false;
            }
            else if ((IndicatedRegionCondition != null) && (tuple.IndicatedRegionCondition == null)) {
                equal &= false;
            }
            else {
                equal &= Convert.ToString(IndicatedRegionCondition) ==
                         Convert.ToString(tuple.IndicatedRegionCondition);
                //                  equal &= Expression.Lambda<Func<bool>>(Expression.Equal(IndicatedRegionCondition, tuple.IndicatedRegionCondition)).Compile()();
            }

            if ((ObjectOptionsCondition == null) && (tuple.ObjectOptionsCondition == null)) {
                equal &= true;
            }
            else if ((ObjectOptionsCondition == null) && (tuple.ObjectOptionsCondition != null)) {
                equal &= false;
            }
            else if ((ObjectOptionsCondition != null) && (tuple.ObjectOptionsCondition == null)) {
                equal &= false;
            }
            else {
                equal &= Convert.ToString(ObjectOptionsCondition) ==
                         Convert.ToString(tuple.ObjectOptionsCondition);
                //                  equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ObjectOptionsCondition, tuple.ObjectOptionsCondition)).Compile()();
            }

            if ((ActionOptionsCondition == null) && (tuple.ActionOptionsCondition == null)) {
                equal &= true;
            }
            else if ((ActionOptionsCondition == null) && (tuple.ActionOptionsCondition != null)) {
                equal &= false;
            }
            else if ((ActionOptionsCondition != null) && (tuple.ActionOptionsCondition == null)) {
                equal &= false;
            }
            else {
                equal &= Convert.ToString(ActionOptionsCondition) ==
                         Convert.ToString(tuple.ActionOptionsCondition);
                //                  equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ActionOptionsCondition, tuple.ActionOptionsCondition)).Compile()();
            }

            if ((ActionSuggestionsCondition == null) && (tuple.ActionSuggestionsCondition == null)) {
                equal &= true;
            }
            else if ((ActionSuggestionsCondition == null) && (tuple.ActionSuggestionsCondition != null)) {
                equal &= false;
            }
            else if ((ActionSuggestionsCondition != null) && (tuple.ActionSuggestionsCondition == null)) {
                equal &= false;
            }
            else {
                equal &= Convert.ToString(ActionSuggestionsCondition) ==
                         Convert.ToString(tuple.ActionSuggestionsCondition);
                //                  equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ActionSuggestionsCondition, tuple.ActionSuggestionsCondition)).Compile()();
            }

            Debug.Log(equal);
            return equal;
        }
    }

    public override int GetHashCode() {
        return IndicatedObjCondition.GetHashCode() ^ GraspedObjCondition.GetHashCode() ^
               IndicatedRegionCondition.GetHashCode() ^ ObjectOptionsCondition.GetHashCode() ^
               ActionOptionsCondition.GetHashCode() ^ ActionSuggestionsCondition.GetHashCode();
    }

    public static bool operator ==(StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
        return tuple1.Equals(tuple2);
    }

    public static bool operator !=(StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
        return !tuple1.Equals(tuple2);
    }
}

public class DialogueStateMachine : CharacterLogicAutomaton
{
    bool useOrderingHeuristics;
    bool humanRelativeDirections;
    bool waveToStart;
    bool useEpistemicModel;

    public SingleAgentInteraction scenario;

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
        scenario = new SingleAgentInteraction();
    }

    // Update is called once per frame
    void Update()
    {

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

                return string.Format("[{0},{1},{2},{3},{4},{5}]",
                    content.IndicatedObj == null
                        ? "Null"
                        : content.IndicatedObj.GetType() == typeof(FunctionDelegate)
                            ? ((FunctionDelegate) content.IndicatedObj).ToString()
                            : Convert.ToString(((GameObject) content.IndicatedObj).name),
                    content.GraspedObj == null
                        ? "Null"
                        : content.GraspedObj.GetType() == typeof(FunctionDelegate)
                            ? ((FunctionDelegate) content.GraspedObj).ToString()
                            : Convert.ToString(((GameObject) content.GraspedObj).name),
                    content.IndicatedRegion == null
                        ? "Null"
                        : content.IndicatedRegion.GetType() == typeof(FunctionDelegate)
                            ? ((FunctionDelegate) content.IndicatedRegion).ToString()
                            : GlobalHelper.RegionToString((Region) content.IndicatedRegion),
                    content.ObjectOptions == null
                        ? "Null"
                        : content.ObjectOptions.GetType() == typeof(FunctionDelegate)
                            ? ((FunctionDelegate) content.ObjectOptions).ToString()
                            : string.Format("[{0}]",
                                String.Join(", ",
                                    ((List<GameObject>) content.ObjectOptions).Select(o => o.name).ToArray())),
                    content.ActionOptions == null
                        ? "Null"
                        : content.ActionOptions.GetType() == typeof(FunctionDelegate)
                            ? ((FunctionDelegate) content.ActionOptions).ToString()
                            : string.Format("[{0}]",
                                String.Join(", ", ((List<string>) content.ActionOptions).ToArray())),
                    content.ActionSuggestions == null
                        ? "Null"
                        : content.ActionSuggestions.GetType() == typeof(FunctionDelegate)
                            ? ((FunctionDelegate) content.ActionSuggestions).ToString()
                            : string.Format("[{0}]",
                                String.Join(", ", ((List<string>) content.ActionSuggestions).ToArray())));
            }
            else if (symbol.Content.GetType() == typeof(StackSymbolConditions)) {
                StackSymbolConditions content = (StackSymbolConditions) symbol.Content;

                return string.Format("[{0},{1},{2},{3},{4},{5}]",
                    content.IndicatedObjCondition == null
                        ? "Null"
                        : Convert.ToString(content.IndicatedObjCondition),
                    content.GraspedObjCondition == null
                        ? "Null"
                        : Convert.ToString(content.GraspedObjCondition),
                    content.IndicatedRegionCondition == null
                        ? "Null"
                        : Convert.ToString(content.IndicatedRegionCondition),
                    content.ObjectOptionsCondition == null
                        ? "Null"
                        : Convert.ToString(content.ObjectOptionsCondition),
                    content.ActionOptionsCondition == null
                        ? "Null"
                        : Convert.ToString(content.ActionOptionsCondition),
                    content.ActionSuggestionsCondition == null
                        ? "Null"
                        : Convert.ToString(content.ActionSuggestionsCondition));
            }
            else if (symbol.Content.GetType() == typeof(PDAState)) {
                return ((PDAState) symbol.Content).Name;
            }
        }
        else if (stackSymbol.GetType() == typeof(StackSymbolContent)) {
            StackSymbolContent content = (StackSymbolContent) stackSymbol;

            return string.Format("[{0},{1},{2},{3},{4},{5}]",
                content.IndicatedObj == null
                    ? "Null"
                    : content.IndicatedObj.GetType() == typeof(FunctionDelegate)
                        ? ((FunctionDelegate) content.IndicatedObj).ToString()
                        : Convert.ToString(((GameObject) content.IndicatedObj).name),
                content.GraspedObj == null
                    ? "Null"
                    : content.GraspedObj.GetType() == typeof(FunctionDelegate)
                        ? ((FunctionDelegate) content.GraspedObj).ToString()
                        : Convert.ToString(((GameObject) content.GraspedObj).name),
                content.IndicatedRegion == null
                    ? "Null"
                    : content.IndicatedRegion.GetType() == typeof(FunctionDelegate)
                        ? ((FunctionDelegate) content.IndicatedRegion).ToString()
                        : GlobalHelper.RegionToString((Region) content.IndicatedRegion),
                content.ObjectOptions == null
                    ? "Null"
                    : content.ObjectOptions.GetType() == typeof(FunctionDelegate)
                        ? ((FunctionDelegate) content.ObjectOptions).ToString()
                        : string.Format("[{0}]",
                            String.Join(", ",
                                ((List<GameObject>) content.ObjectOptions).Select(o => o.name).ToArray())),
                content.ActionOptions == null
                    ? "Null"
                    : content.ActionOptions.GetType() == typeof(FunctionDelegate)
                        ? ((FunctionDelegate) content.ActionOptions).ToString()
                        : string.Format("[{0}]", String.Join(", ", ((List<string>) content.ActionOptions).ToArray())),
                content.ActionSuggestions == null
                    ? "Null"
                    : content.ActionSuggestions.GetType() == typeof(FunctionDelegate)
                        ? ((FunctionDelegate) content.ActionSuggestions).ToString()
                        : string.Format("[{0}]",
                            String.Join(", ", ((List<string>) content.ActionSuggestions).ToArray())));
        }
        else if (stackSymbol.GetType() == typeof(FunctionDelegate)) {
            return string.Format(":{0}", ((FunctionDelegate) stackSymbol).Method.Name);
        }

        return string.Empty;
    }
}
