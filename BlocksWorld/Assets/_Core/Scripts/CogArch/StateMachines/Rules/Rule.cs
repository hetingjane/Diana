using System;

/// <summary>
/// A rule is <c>true</c> any time the wrapped boolean function is <c>true</c>
/// </summary>
public class Rule
{
	/// <summary>
	/// The function to call on each call to <see cref="Evaluate"/>
	/// </summary>
	protected readonly Func<bool> func;

	/// <summary>
	/// Result of last call to <see cref="func"/>
	/// </summary>
	protected bool lastFuncEvaluation;

	/// <summary>
	/// Creates a rule from a boolean function
	/// </summary>
	/// <param name="func">Boolean function that is called on every call to <see cref="Evaluate"/></param>
	public Rule(Func<bool> func)
	{
		this.func = func;
		lastFuncEvaluation = false;
	}

	/// <summary>
	/// Evaluates the rule. The rule is <c>true</c> if the wrapped boolean function is <c>true</c>
	/// </summary>
	/// <returns>The evaluation result</returns>
	public virtual bool Evaluate()
	{
		// Update last evaluation result with the current result
		lastFuncEvaluation = func();
		return lastFuncEvaluation;
	}

	/// <summary>
	/// Resets the rule
	/// </summary>
	public virtual void Reset()
	{
		lastFuncEvaluation = false;
	}

	/// <summary>
	/// A rule that is always <c>true</c> i.e. <see cref="Evaluate"/> will always return <c>true</c>
	/// </summary>
	public static readonly Rule True = new Rule(() => true);

	/// <summary>
	/// A rule that is always <c>false</c> i.e. <see cref="Evaluate"/> will always return <c>false</c>
	/// </summary>
	public static readonly Rule False = new Rule(() => false);
}