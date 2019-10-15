public class PointingIntentModule: ModuleBase
{
	private PointingStateMachine pointingStateMachine;

	protected override void Start()
	{
		base.Start();
		pointingStateMachine = new PointingStateMachine();
	}

	private void Update()
	{
		if (pointingStateMachine.Evaluate())
		{
			switch (pointingStateMachine.CurrentState)
			{
				case PointingState.Pointed:
				case PointingState.PointingStop:
					DataStore.SetStringValue("user:lastPointedAt:name", new DataStore.StringValue(pointingStateMachine.LastPointedAtObject?.name ?? string.Empty), this, string.Empty);
					DataStore.SetValue("user:lastPointedAt:position", new DataStore.Vector3Value(pointingStateMachine.LastPointedAtLocation), this, string.Empty);
					break;
				default:
					break;
			}
		}
	}

}
