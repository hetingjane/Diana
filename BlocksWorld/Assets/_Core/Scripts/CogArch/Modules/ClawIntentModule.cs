/// <summary>
/// This module identifies a claw intent from gestures and posts it to the blackboard.
/// <para>
/// Reads:		user:isEngaged			If the user is engaged or not
///				user:hands:left			The hand gesture from the left hand
///				user:hands:right		The hand gesture from the right hand
/// </para>
/// <para>
/// Writes:		user:intent:isClaw		<c>true</c> if the user's intent is claw, <c>false</c> otherwise
/// </para>
/// </summary>
public class ClawIntentModule : ModuleBase
{
	private ClawStateMachine clawStateMachine;

	protected override void Start()
    {
		base.Start();

		clawStateMachine = new ClawStateMachine();
	}

	private void Update()
	{
		if (clawStateMachine.Evaluate())
		{
			switch (clawStateMachine.CurrentState)
			{
				case ClawState.ClawStart:
					DataStore.SetValue("user:intent:isClaw", DataStore.BoolValue.True, this, "claw down with either hand");
					break;
				case ClawState.ClawStop:
					DataStore.SetValue("user:intent:isClaw", DataStore.BoolValue.False, this, "");
					break;
			}
		}
	}
}
