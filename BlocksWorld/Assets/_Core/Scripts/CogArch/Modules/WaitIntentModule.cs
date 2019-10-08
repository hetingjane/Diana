/// <summary>
/// This module identifies a wait intent from gestures and posts it to the blackboard.
/// <para>
/// Reads:		user:isEngaged			If the user is engaged or not
///				user:hands:left			The hand gesture from the left hand
///				user:hands:right		The hand gesture from the right hand
/// </para>
/// <para>
/// Writes:		user:intent:isWait		<c>true</c> if the user's intent is wait, <c>false</c> otherwise
/// </para>
/// </summary>
public class WaitIntentModule : ModuleBase
{
	private WaitStateMachine waitStateMachine;

	protected override void Start()
    {
		base.Start();

		waitStateMachine = new WaitStateMachine();
	}

	private void Update()
	{
		if (waitStateMachine.Evaluate())
		{
			switch (waitStateMachine.CurrentState)
			{
				case WaitState.WaitStart:
					DataStore.SetValue("user:intent:isWait", DataStore.BoolValue.True, this, "count one with either hand");
					break;
				case WaitState.WaitStop:
					DataStore.SetValue("user:intent:isWait", DataStore.BoolValue.False, this, "");
					break;
			}
		}
	}
}
