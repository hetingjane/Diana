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
					DataStore.SetStringValue("user:intent:nevermind", new DataStore.StringValue("nevermind start"), this, "thumbs up with either hand");
					break;
				case NevermindState.NevermindStop:
					DataStore.SetStringValue("user:intent:nevermind", new DataStore.StringValue("nevermind stop"), this, "");
					break;
			}
		}
	}
}
