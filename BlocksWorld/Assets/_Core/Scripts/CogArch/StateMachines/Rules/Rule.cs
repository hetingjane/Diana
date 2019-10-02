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

	public static readonly Rule True = new Rule(() => true);
	public static readonly Rule False = new Rule(() => false);
}