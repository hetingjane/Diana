using System;

/// <summary>
/// A timed rule is <c>true</c> if the wrapped boolean function has been <c>true</c>
/// at least once, and on each call thereafter, of <see cref="Evaluate"/> 
/// for a given number of milliseconds
/// </summary>
public class TimedRule : Rule
{
	/// <summary>
	/// The time in milliseconds for which the wrapped boolean function must consistently
	/// be <c>true</c> in order for it to evaluate to be <c>true</c>
	/// </summary>
	private readonly double time;

	/// <summary>
	/// Time at which the wrapped function first became <c>true</c>.
	/// Any time the wrapped function transitions from becoming <c>true</c>
	/// to <c>false</c>, this becomes <see cref="DateTime.MaxValue"/> which
	/// is meant to be "Never"
	/// </summary>
	private DateTime trueStartTime;

	/// <summary>
	/// Creates a timed rule from a boolean function and a time duration
	/// </summary>
	/// <param name="func">Boolean function that is called on every call to <see cref="Evaluate"/></param>
	/// <param name="time">Time in milliseconds for which the boolean function must be <c>true</c></param>
	public TimedRule(Func<bool> func, double time) : base(func)
	{
		if (time < 0.0)
			throw new ArgumentOutOfRangeException("'time' must be a non-negative number");

		this.time = time;
		trueStartTime = DateTime.MaxValue;
	}

	/// <summary>
	/// Resets the rule
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		trueStartTime = DateTime.MaxValue;
	}

	/// <summary>
	/// Evaluates the rule. The rule is <c>true</c> if the wrapped boolean function has
	/// been <c>true</c> at least once, and on each call thereafter, of <see cref="Evaluate"/> 
	/// for a given number of milliseconds
	/// </summary>
	/// <returns>The current evaluation result</returns>
	public override bool Evaluate()
	{
		bool prevFuncEvaluation = lastFuncEvaluation;
		bool curFuncEvaluation = base.Evaluate();

		if (curFuncEvaluation)
		{
			// Transition from False to True
			if (!prevFuncEvaluation)
				trueStartTime = DateTime.Now;
		}
		else
		{
			// Transition from True to False
			if (prevFuncEvaluation)
				trueStartTime = DateTime.MaxValue;
		}

		return curFuncEvaluation && (DateTime.Now - trueStartTime).TotalMilliseconds > time;
	}
}
