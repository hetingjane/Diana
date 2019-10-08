/// <summary>
/// This module identifies a nevermind intent from gestures and posts it to the blackboard.
/// <para>
/// Reads:		user:isEngaged			If the user is engaged or not
///				user:hands:left			The hand gesture from the left hand
///				user:hands:right		The hand gesture from the right hand
/// </para>
/// <para>
/// Writes:		user:intent:isNevermind	<c>true</c> if the user's intent is never mind, <c>false</c> otherwise
/// </para>
/// </summary>
public class NevermindIntentModule : ModuleBase
{
	private NevermindStateMachine nevermindStateMachine;

	protected override void Start()
    {
		base.Start();

		nevermindStateMachine = new NevermindStateMachine();
	}

	private void Update()
	{
		if (nevermindStateMachine.Evaluate())
		{
			switch (nevermindStateMachine.CurrentState)
			{
				case NevermindState.NevermindStart:
					DataStore.SetValue("user:intent:isNevermind", DataStore.BoolValue.True, this, "stop gesture with either hand");
					break;
				case NevermindState.NevermindStop:
					DataStore.SetValue("user:intent:isNevermind", DataStore.BoolValue.False, this, "");
					break;
			}
		}
	}
}
