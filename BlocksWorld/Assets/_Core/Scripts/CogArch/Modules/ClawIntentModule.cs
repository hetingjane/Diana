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
