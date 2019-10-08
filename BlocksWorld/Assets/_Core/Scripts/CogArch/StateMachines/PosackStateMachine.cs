public enum PosackState
{
	PosackStop,
	PosackStart
}

public class PosackStateMachine : RuleStateMachine<PosackState>
{
	public PosackStateMachine()
	{
		SetTransitionRule(PosackState.PosackStop, PosackState.PosackStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture == "thumbs up" || rightHandGesture == "thumbs up";
			}

			return false;
		}, 100));

		SetTransitionRule(PosackState.PosackStart, PosackState.PosackStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture != "thumbs up" && rightHandGesture != "thumbs up";
			}
		}, 100));
	}
}
