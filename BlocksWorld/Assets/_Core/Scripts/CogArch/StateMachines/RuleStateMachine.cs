using System.Collections.Generic;
using System;

/// <summary>
/// Base class for a state machine that has a fixed number of states such
/// that the transitions between states are decided by <see cref="Rule"/>s.
/// </summary>
/// <typeparam name="T">an enumeration of the states</typeparam>
/// <example>
/// <code>
/// public enum MyStates
/// {
///		Start,
///		Stop
/// }
/// 
/// public class MyOwnStateMachine: RuleStateMachine{MyStates}
/// {
///		public MyOwnStateMachine(): base()
///		{
///			SetTransitionRule(MyStates.Start, MyStates.Stop, Rule.True);
///			SetTransitionRule(MyStates.Stop, MyStates.Start, Rule.False);
///			StateChanged += StateChangedEventHandler;
///		}
///		
///		private void StateChangedEventHandler()
///		{
///			// Do something
///		}
/// }
/// </code>
/// </example>
public abstract class RuleStateMachine<T> where T: Enum
{
	/// <summary>
	/// A mapping from initial state to a nested mapping. The nested mapping maps
	/// a target state to the rule that guards the transition to that state from
	/// the initial state.
	/// </summary>
	private Dictionary<T, Dictionary<T, Rule>> transitions;

	/// <summary>
	/// The current state
	/// </summary>
	private T currentState;

	/// <summary>
	/// The current state.
	/// <para>
	/// Setting the property resets all the rules transitioning out from the new state.
	/// Setting also invokes <see cref="StateChanged"/> event.
	/// </para>
	/// </summary>
	public T CurrentState
	{
		get
		{
			return currentState;
		}

		private set
		{
			T prevState = currentState;

			currentState = value;

			if (transitions.TryGetValue(currentState, out var stateToRule))
			{
				foreach (var rule in stateToRule.Values)
					rule.Reset();
			}

			StateChanged?.Invoke(this, new StateChangedEventArgs<T>(prevState, currentState));
		}
	}

	/// <summary>
	/// Event arguments that accompany a <see cref="StateChanged"/> event
	/// </summary>
	/// <typeparam name="T1">an enumeration of the states</typeparam>
	public class StateChangedEventArgs<T1>: EventArgs
	{
		/// <summary>
		/// State from which the transition was made (previous state)
		/// </summary>
		public T1 FromState
		{
			get;
		}

		/// <summary>
		/// State to which the transition was made (current state)
		/// </summary>
		public T1 ToState
		{
			get;
		}

		/// <summary>
		/// Create an instance using from state and to state
		/// </summary>
		/// <param name="fromState">State from which the transition was made (previous state)</param>
		/// <param name="toState">State to which the transition was made (current state)</param>
		public StateChangedEventArgs(T1 fromState, T1 toState)
		{
			FromState = fromState;
			ToState = toState;
		}
	}

	/// <summary>
	/// Event that is triggered whenever the state is changed
	/// </summary>
	public event EventHandler<StateChangedEventArgs<T>> StateChanged;

	public event EventHandler Evaluated;
	
	/// <summary>
	/// Create a rule state machine with initial state set to the first value of the <see cref="T"/> enumeration
	/// </summary>
	protected RuleStateMachine(): this(default)
	{
	}

	/// <summary>
	/// Create a rule state machine with initial state set to the given value of the<see cref="T"/> enumeration 
	/// </summary>
	/// <param name="initialState">The state in which the state machine is initially in</param>
	protected RuleStateMachine(T initialState)
	{
		transitions = new Dictionary<T, Dictionary<T, Rule>>();
		// This won't invoke StateChanged event, since no event handlers are attached to the event
		CurrentState = initialState;
	}

	/// <summary>
	/// Check if two enumeration values are equal. This is safe to use with generics.
	/// </summary>
	/// <param name="first">first enumeration value</param>
	/// <param name="second">second enumeration value</param>
	/// <returns><c>true</c> if the two enumeration values are equal, <c>false</c> otherwise</returns>
	private bool IsSameState(T first, T second)
	{
		return EqualityComparer<T>.Default.Equals(first, second);
	}

	/// <summary>
	/// Sets a transition rule from one state to another using a <see cref="Rule"/>
	/// </summary>
	/// <param name="fromState">the state from which the transition will be made</param>
	/// <param name="toState">the state to which the transition will be made</param>
	/// <param name="rule">the rule that must be true for the transition to be made</param>
	public void SetTransitionRule(T fromState, T toState, Rule rule)
	{
		if (IsSameState(fromState, toState))
			throw new ArgumentException("'fromState' and 'toState' cannot be the same states");

		if (rule == null)
			throw new ArgumentNullException("'rule' argument cannot be null");

		if (transitions.TryGetValue(fromState, out var transitionToRule))
		{
			transitionToRule[toState] = rule;
		}
		else
		{
			transitions[fromState] = new Dictionary<T, Rule>() {
				{ toState, rule }
			};
		}
	}

	/// <summary>
	/// Evaluates all the rules transitioning out of the current state.
	/// If a rule evaluates to <c>true</c>, the transition is made to the new state.
	/// </summary>
	/// <returns><c>true</c> if the transition was made, <c>false</c> otherwise</returns>
	public bool Evaluate()
	{
		bool stateChanged = false;
		foreach(var pair in transitions[CurrentState])
		{
			var (toState, rule) = (pair.Key, pair.Value);
			if (rule.Evaluate())
			{
				CurrentState = toState;
				stateChanged = true;
				break;
			}
		}

		Evaluated?.Invoke(this, EventArgs.Empty);

		return stateChanged;
	}
}