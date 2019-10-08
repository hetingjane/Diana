/// <summary>
/// This module identifies a directional push intent from gestures and posts it to the blackboard.
/// <para>
/// Reads:		user:isEngaged			If the user is engaged or not
///				user:hands:left			The hand gesture from the left hand
///				user:hands:right		The hand gesture from the right hand
///				user:arms:left			The motion of left arm
///				user:arms:right			The motion of right arm
/// </para>
/// <para>
/// Writes:		user:intent:isPushLeft	<c>true</c> if the user's intent is to push left with the right hand, <c>false</c> otherwise
///				user:intent:isPushRight	<c>true</c> if the user's intent is to push right with the left hand, <c>false</c> otherwise
/// </para>
/// </summary>
public class PushIntentModule : ModuleBase
{
	private PushLeftStateMachine pushLeftStateMachine;
	private PushRightStateMachine pushRightStateMachine;
	
	protected override void Start()
    {
		base.Start();

		pushLeftStateMachine = new PushLeftStateMachine();
		pushRightStateMachine = new PushRightStateMachine();
	}

	private void Update()
	{
		if (pushRightStateMachine.Evaluate())
		{
			switch (pushRightStateMachine.CurrentState)
			{
				case PushRightState.PushRightStart:
					DataStore.SetValue("user:intent:isPushRight", DataStore.BoolValue.True, this, "pushing right with left hand");
					break;
				case PushRightState.PushRightStop:
					DataStore.SetValue("user:intent:isPushRight", DataStore.BoolValue.False, this, "stopped pushing right with left hand");
					break;
			}
		}

		if (pushLeftStateMachine.Evaluate())
		{
			switch (pushLeftStateMachine.CurrentState)
			{
				case PushLeftState.PushLeftStart:
					DataStore.SetValue("user:intent:isPushLeft", DataStore.BoolValue.True, this, "pushing left with right hand");
					break;
				case PushLeftState.PushLeftStop:
					DataStore.SetValue("user:intent:isPushLeft", DataStore.BoolValue.False, this, "stopped pushing left with right hand");
					break;
			}
		}
	}
}
