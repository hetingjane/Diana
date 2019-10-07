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
