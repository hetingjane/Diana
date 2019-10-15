public enum ServoLeftState
{
	ServoLeftStop,
	ServoLeftStart
}

public class ServoLeftStateMachine : RuleStateMachine<ServoLeftState>
{
	public ServoLeftStateMachine()
	{
		SetTransitionRule(ServoLeftState.ServoLeftStop, ServoLeftState.ServoLeftStart, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (userIsEngaged)
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmServo = DataStore.GetStringValue("user:armMotion:right");
				return (rightHandGesture == "open left" || rightHandGesture == "closed left") && rightArmServo == "servo";
			}
			return false;
		}, 300));

		SetTransitionRule(ServoLeftState.ServoLeftStart, ServoLeftState.ServoLeftStop, new TimedRule(() =>
		{
			bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");

			if (!userIsEngaged)
				return true;
			else
			{
				string rightHandGesture = DataStore.GetStringValue("user:hands:right");
				string rightArmServo = DataStore.GetStringValue("user:armMotion:right");
				return (rightHandGesture != "open left" && rightHandGesture != "closed left") || rightArmServo != "servo";
			}
		}, 100));
	}
}