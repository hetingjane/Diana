public enum PushBackState
{
	PushBackStop,
	PushBackStart
}

public class PushBackStateMachine : RuleStateMachine<PushBackState>
{
	public PushBackStateMachine()
	{
		SetTransitionRule(PushBackState.PushBackStop, PushBackState.PushBackStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmMotion = DataStore.GetStringValue("user:arms:right");
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string leftArmMotion = DataStore.GetStringValue("user:arms:left");

				return (rightHandGesture == "closed back" && rightArmMotion == "move back") || (leftHandGesture == "closed back" && leftArmMotion == "move back");
			}
			return false;
		}, 300));

		SetTransitionRule(PushBackState.PushBackStart, PushBackState.PushBackStop, new TimedRule(() =>
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

				return !(rightHandGesture == "closed back" && rightArmMotion == "move back") && !(leftHandGesture == "closed back" && leftArmMotion == "move back");
			}
		}, 100));
	}
}