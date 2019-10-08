public enum WaitState
{
	WaitStop,
	WaitStart
}

public class WaitStateMachine : RuleStateMachine<WaitState>
{
	public WaitStateMachine()
	{
		SetTransitionRule(WaitState.WaitStop, WaitState.WaitStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture == "one front" || rightHandGesture == "one front";
			}

			return false;
		}, 300));

		SetTransitionRule(WaitState.WaitStart, WaitState.WaitStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture != "one front" && rightHandGesture != "one front";
			}
		}, 100));
	}
}
