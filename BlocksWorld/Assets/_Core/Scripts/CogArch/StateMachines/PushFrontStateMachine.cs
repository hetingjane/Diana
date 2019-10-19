public enum PushFrontState
{
	PushFrontStop,
	PushFrontStart
}

public class PushFrontStateMachine : RuleStateMachine<PushFrontState>
{
	public PushFrontStateMachine()
	{
		SetTransitionRule(PushFrontState.PushFrontStop, PushFrontState.PushFrontStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmMotion = DataStore.GetStringValue("user:arms:right");
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string leftArmMotion = DataStore.GetStringValue("user:arms:left");

				return (rightHandGesture == "closed front" && rightArmMotion == "move front") || (leftHandGesture == "closed front" && leftArmMotion == "move front");
			}
			return false;
		}, 300));

		SetTransitionRule(PushFrontState.PushFrontStart, PushFrontState.PushFrontStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmMotion = DataStore.GetStringValue("user:arms:right");
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string leftArmMotion = DataStore.GetStringValue("user:arms:left");

				return !(rightHandGesture == "closed front" && rightArmMotion == "move front") && !(leftHandGesture == "closed front" && leftArmMotion == "move front");
			}
		}, 100));
	}
}