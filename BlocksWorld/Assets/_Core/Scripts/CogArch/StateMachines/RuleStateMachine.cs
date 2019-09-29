using System.Collections.Generic;
using System;

public abstract class RuleStateMachine<T> where T: Enum
{
	private Dictionary<T, Dictionary<T, Rule>> transitions;


	private T currentState;

	public T CurrentState
	{
		get
		{
			return currentState;
		}

		private set
		{
			currentState = value;

			if (transitions.TryGetValue(currentState, out var stateToRule))
			{
				foreach (var rule in stateToRule.Values)
					rule.Reset();
			}
		}
	}
	

	public RuleStateMachine()
	{
		transitions = new Dictionary<T, Dictionary<T, Rule>>();
		Initialize();
	}

	public RuleStateMachine(T initialState): this()
	{
		CurrentState = initialState;
	}

	private bool IsSameState(T first, T second)
	{
		return EqualityComparer<T>.Default.Equals(first, second);
	}

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

	public bool Evaluate()
	{
		foreach(var pair in transitions[CurrentState])
		{
			var (toState, rule) = (pair.Key, pair.Value);
			if (rule.Evaluate())
			{
				CurrentState = toState;
				return true;
			}
		}
		return false;
	}

	protected abstract void Initialize();
}