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
					DataStore.SetStringValue("user:intent:posack", new DataStore.StringValue("posack start"), this, "thumbs up with either hand");
					break;
				case PosackState.PosackStop:
					DataStore.SetStringValue("user:intent:posack", new DataStore.StringValue("posack stop"), this, "");
					break;
			}
		}
	}
}
