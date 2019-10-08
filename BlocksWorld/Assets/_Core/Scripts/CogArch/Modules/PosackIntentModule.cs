/// <summary>
/// This module identifies a positive acknowledgement intent from gestures and posts it to the blackboard.
/// <para>
/// Reads:		user:isEngaged			If the user is engaged or not
///				user:hands:left			The hand gesture from the left hand
///				user:hands:right		The hand gesture from the right hand
/// </para>
/// <para>
/// Writes:		user:intent:isPosack	<c>true</c> if the user's intent is positive acknowledgement, <c>false</c> otherwise
/// </para>
/// </summary>
public class PosackIntentModule : ModuleBase
{
	private PosackStateMachine posackStateMachine;

	protected override void Start()
    {
		base.Start();

		posackStateMachine = new PosackStateMachine();
	}

	private void Update()
	{
		if (posackStateMachine.Evaluate())
		{
			switch (posackStateMachine.CurrentState)
			{
				case PosackState.PosackStart:
					DataStore.SetValue("user:intent:isPosack", DataStore.BoolValue.True, this, "thumbs up with either hand");
					break;
				case PosackState.PosackStop:
					DataStore.SetValue("user:intent:isPosack", DataStore.BoolValue.False, this, "");
					break;
			}
		}
	}
}
