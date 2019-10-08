public enum NegackState
{
	NegackStop,
	NegackStart
}

public class NegackStateMachine : RuleStateMachine<NegackState>
{
	public NegackStateMachine()
	{
		SetTransitionRule(NegackState.NegackStop, NegackState.NegackStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture == "thumbs down" || rightHandGesture == "thumbs down";
			}

			return false;
		}, 100));

		SetTransitionRule(NegackState.NegackStart, NegackState.NegackStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture != "thumbs down" && rightHandGesture != "thumbs down";
			}
		}, 100));
	}
}
