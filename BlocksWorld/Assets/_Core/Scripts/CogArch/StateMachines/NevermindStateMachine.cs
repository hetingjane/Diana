public enum NevermindState
{
	NevermindStop,
	NevermindStart
}

public class NevermindStateMachine : RuleStateMachine<NevermindState>
{
	public NevermindStateMachine()
	{
		SetTransitionRule(NevermindState.NevermindStop, NevermindState.NevermindStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture == "stop" || rightHandGesture == "stop" || leftHandGesture == "five front" || rightHandGesture == "five front";
			}

			return false;
		}, 600));

		SetTransitionRule(NevermindState.NevermindStart, NevermindState.NevermindStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");

				return leftHandGesture != "stop" && rightHandGesture != "stop" && leftHandGesture != "five front" && rightHandGesture != "five front";
			}
		}, 600));
	}
}
