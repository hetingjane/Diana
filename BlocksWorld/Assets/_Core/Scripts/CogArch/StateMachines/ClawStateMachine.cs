public enum ClawState
{
	ClawStop,
	ClawStart
}

public class ClawStateMachine : RuleStateMachine<ClawState>
{
	public ClawStateMachine()
	{
		SetTransitionRule(ClawState.ClawStop, ClawState.ClawStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture == "claw down" || rightHandGesture == "claw down";
			}

			return false;
		}, 300));

		SetTransitionRule(ClawState.ClawStart, ClawState.ClawStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture != "claw down" && rightHandGesture != "claw down";
			}
		}, 100));
	}
}
