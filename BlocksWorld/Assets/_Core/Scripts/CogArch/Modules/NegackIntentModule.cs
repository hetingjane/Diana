/// <summary>
/// This module identifies a negative acknowledgement intent from gestures and posts it to the blackboard.
/// <para>
/// Reads:		user:isEngaged			If the user is engaged or not
///				user:hands:left			The hand gesture from the left hand
///				user:hands:right		The hand gesture from the right hand
/// </para>
/// <para>
/// Writes:		user:intent:isNegack	<c>true</c> if the user's intent is negative acknowledgement, <c>false</c> otherwise
/// </para>
/// </summary>
public class NegackIntentModule : ModuleBase
{
	private NegackStateMachine negackStateMachine;

	protected override void Start()
    {
		base.Start();

		negackStateMachine = new NegackStateMachine();
	}

	private void Update()
	{
		if (negackStateMachine.Evaluate())
		{
			switch (negackStateMachine.CurrentState)
			{
				case NegackState.NegackStart:
					DataStore.SetValue("user:intent:isNegack", DataStore.BoolValue.True, this, "thumbs down with either hand");
					break;
				case NegackState.NegackStop:
					DataStore.SetValue("user:intent:isNegack", DataStore.BoolValue.False, this, "");
					break;
			}
		}
	}
}
