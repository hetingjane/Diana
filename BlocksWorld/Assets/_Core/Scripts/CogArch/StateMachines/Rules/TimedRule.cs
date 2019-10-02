using System;
using UnityEngine;

public class TimedRule : Rule
{
	private readonly double time;
	private DateTime trueStartTime;

	public TimedRule(Func<bool> func, double time) : base(func)
	{
		if (time < 0.0)
			throw new ArgumentOutOfRangeException("'time' must be a non-negative number");

		this.time = time;
		trueStartTime = DateTime.MaxValue;
	}

	public override void Reset()
	{
		base.Reset();
		trueStartTime = DateTime.MaxValue;
	}

	public override bool Evaluate()
	{
		bool prevFuncEvaluation = lastFuncEvaluation;
		bool curFuncEvaluation = base.Evaluate();

		if (curFuncEvaluation)
		{
			if (!prevFuncEvaluation)
				trueStartTime = DateTime.Now;
		}
		else
		{
			if (prevFuncEvaluation)
				trueStartTime = DateTime.MaxValue;
		}

		return curFuncEvaluation && (DateTime.Now - trueStartTime).TotalMilliseconds > time;
	}
}
