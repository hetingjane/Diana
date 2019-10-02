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
