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
