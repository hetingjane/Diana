public enum WaveState
{
	WaveStart,
	WaveStop
}

public class WaveStateMachine : RuleStateMachine<WaveState>
{
	private bool IsUserWaving()
	{
		string rightArmMotion = DataStore.GetStringValue("user:arms:right");
		string leftArmMotion = DataStore.GetStringValue("user:arms:left");
		return rightArmMotion == "ra wave" || leftArmMotion == "la wave";
	}

	protected override void Initialize()
	{
		SetTransitionRule(WaveState.WaveStop, WaveState.WaveStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");
			return userIsEngaged && IsUserWaving();
		}, 300));

		SetTransitionRule(WaveState.WaveStart, WaveState.WaveStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");
			return !userIsEngaged || !IsUserWaving();
		}, 200));
	}
}
