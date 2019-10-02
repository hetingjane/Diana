public enum PushRightState
{
	PushRightStop,
	PushRightStart
}

public class PushRightStateMachine : RuleStateMachine<PushRightState>
{
	protected override void Initialize()
	{
		SetTransitionRule(PushRightState.PushRightStop, PushRightState.PushRightStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:engaged");

			if (userIsEngaged)
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string leftArmMotion = DataStore.GetStringValue("user:arms:left");

				return (leftHandGesture == "lh open right" || leftHandGesture == "lh closed right") && leftArmMotion == "la move right";
			}
			return false;
		}, 300));

		SetTransitionRule(PushRightState.PushRightStart, PushRightState.PushRightStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:engaged");

			if (!userIsEngaged)
			{
				return true;
			}
			else
			{
				string leftHandGesture = DataStore.GetStringValue("user:hands:left");
				string leftArmMotion = DataStore.GetStringValue("user:arms:left");

				return (leftHandGesture != "lh open right" && leftHandGesture != "lh closed right") || leftArmMotion != "la move right";
			}
		}, 100));
	}
}
