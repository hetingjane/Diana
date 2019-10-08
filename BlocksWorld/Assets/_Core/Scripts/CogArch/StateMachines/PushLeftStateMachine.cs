public enum PushLeftState
{
	PushLeftStop,
	PushLeftStart
}

public class PushLeftStateMachine : RuleStateMachine<PushLeftState>
{
	public PushLeftStateMachine()
	{
		SetTransitionRule(PushLeftState.PushLeftStop, PushLeftState.PushLeftStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmMotion = DataStore.GetStringValue("user:arms:right");
				return (rightHandGesture == "open left" || rightHandGesture == "closed left") && rightArmMotion == "move left";
			}
			return false;
		}, 300));

		SetTransitionRule(PushLeftState.PushLeftStart, PushLeftState.PushLeftStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmMotion = DataStore.GetStringValue("user:arms:right");
				return (rightHandGesture != "open left" && rightHandGesture != "closed left") || rightArmMotion != "move left";
			}
		}, 100));
	}
}