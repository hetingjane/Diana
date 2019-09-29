
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
					DataStore.SetStringValue("user:intent:negack", new DataStore.StringValue("negack start"), this, "thumbs down with either hand");
					break;
				case NegackState.NegackStop:
					DataStore.SetStringValue("user:intent:negack", new DataStore.StringValue("negack stop"), this, "");
					break;
			}
		}
	}
}
