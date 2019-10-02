
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
