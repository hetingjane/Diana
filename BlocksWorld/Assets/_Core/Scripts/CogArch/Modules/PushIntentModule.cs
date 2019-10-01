public class PushIntentModule : ModuleBase
{
	private PushLeftStateMachine pushLeftStateMachine;
	private PushRightStateMachine pushRightStateMachine;
	
	protected override void Start()
    {
		base.Start();

		pushLeftStateMachine = new PushLeftStateMachine();
		pushRightStateMachine = new PushRightStateMachine();
	}

	private void Update()
	{
		if (pushRightStateMachine.Evaluate())
		{
			switch (pushRightStateMachine.CurrentState)
			{
				case PushRightState.PushRightStart:
					DataStore.SetValue("user:intent:isPushRight", DataStore.BoolValue.True, this, "pushing right with left hand");
					break;
				case PushRightState.PushRightStop:
					DataStore.SetValue("user:intent:isPushRight", DataStore.BoolValue.False, this, "stopped pushing right with left hand");
					break;
			}
		}

		if (pushLeftStateMachine.Evaluate())
		{
			switch (pushLeftStateMachine.CurrentState)
			{
				case PushLeftState.PushLeftStart:
					DataStore.SetValue("user:intent:isPushLeft", DataStore.BoolValue.True, this, "pushing left with right hand");
					break;
				case PushLeftState.PushLeftStop:
					DataStore.SetValue("user:intent:isPushLeft", DataStore.BoolValue.False, this, "stopped pushing left with right hand");
					break;
			}
		}
	}
}
