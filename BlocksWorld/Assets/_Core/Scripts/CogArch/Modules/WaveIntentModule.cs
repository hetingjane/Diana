public class WaveIntentModule : ModuleBase
{
	private WaveStateMachine waveStateMachine;

	protected override void Start()
	{
		base.Start();

		waveStateMachine = new WaveStateMachine();
	}

	private void Update()
	{
		if (waveStateMachine.Evaluate())
		{
			switch (waveStateMachine.CurrentState)
			{
				case WaveState.WaveStart:
					DataStore.SetValue("user:intent:isWaving", DataStore.BoolValue.True, this, "waving with either hand");
					break;
				case WaveState.WaveStop:
					DataStore.SetValue("user:intent:isWaving", DataStore.BoolValue.False, this, "");
					break;
			}
		}
	}
}
