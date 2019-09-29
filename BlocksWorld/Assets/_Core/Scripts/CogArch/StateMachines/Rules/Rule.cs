using System;

public class Rule
{
	protected readonly Func<bool> func;

	protected bool lastFuncEvaluation;

	public Rule(Func<bool> func)
	{
		this.func = func;
		lastFuncEvaluation = false;
	}

	public virtual bool Evaluate()
	{
		lastFuncEvaluation = func();
		return lastFuncEvaluation;
	}

	public virtual void Reset()
	{
		lastFuncEvaluation = false;
	}
}