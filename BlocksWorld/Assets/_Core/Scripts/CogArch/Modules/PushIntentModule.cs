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
					DataStore.SetStringValue("user:intent:pushRight", new DataStore.StringValue("push right start"), this, "pushing right with left hand");
					break;
				case PushRightState.PushRightStop:
					DataStore.SetStringValue("user:intent:pushRight", new DataStore.StringValue("push right stop"), this, "stopped pushing right with left hand");
					break;
			}
		}

		if (pushLeftStateMachine.Evaluate())
		{
			switch (pushLeftStateMachine.CurrentState)
			{
				case PushLeftState.PushLeftStart:
					DataStore.SetStringValue("user:intent:pushLeft", new DataStore.StringValue("push left start"), this, "pushing left with right hand");
					break;
				case PushLeftState.PushLeftStop:
					DataStore.SetStringValue("user:intent:pushLeft", new DataStore.StringValue("push left stop"), this, "stopped pushing left with right hand");
					break;
			}
		}
	}
}
