using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using VoxSimPlatform.Global;

public enum PointingState
{
	PointingStop,
	PointingStart,
	Pointed
}

public class PointingStateMachine : RuleStateMachine<PointingState>
{
	public GameObject LastPointedAtObject
	{
		get;
		private set;
	}

	public Vector3 LastPointedAtLocation
	{
		get;
		private set;
	}

	private bool IsUserEngaged()
	{
		bool userIsEngaged = DataStore.GetBoolValue("user:isEngaged");
		return userIsEngaged;
	}

	private bool IsUserPointing(bool checkValidity=true)
	{
		bool userIsPointing = DataStore.GetBoolValue("user:isPointing");
		if (checkValidity)
		{
			bool pointPositionIsValid = DataStore.GetBoolValue("user:pointValid");
			return pointPositionIsValid;
		}
		return userIsPointing;
	}

	private Queue<Vector3> lastNPositions;

	private const int N = 30;

	private bool IsPointedAtLocationStable()
	{
		if (lastNPositions.Count < N)
			return false;

		Vector3 minV = Vector3.one * float.MaxValue;
		Vector3 maxV = Vector3.one * float.MinValue;

		foreach (Vector3 v in lastNPositions)
		{
			if (v.x < minV.x)
				minV.x = v.x;
			if (v.y < minV.y)
				minV.y = v.y;
			if (v.z < minV.z)
				minV.z = v.z;

			if (v.x > maxV.x)
				maxV.x = v.x;
			if (v.y > maxV.y)
				maxV.y = v.y;
			if (v.z > maxV.z)
				maxV.z = v.z;
		}

		//Debug.LogWarning((maxV - minV).magnitude);
		return (maxV - minV).magnitude < .05f;
	}

	private Vector3 AveragePointedAtLocation
	{
		get
		{
			return new Vector3(
				x: lastNPositions.Average((v) => v.x),
				y: lastNPositions.Average((v) => v.y),
				z: lastNPositions.Average((v) => v.z)
				);
		}
	}

	public PointingStateMachine()
	{
		lastNPositions = new Queue<Vector3>();

		LastPointedAtObject = null;

		SetTransitionRule(PointingState.PointingStop, PointingState.PointingStart, new TimedRule(() => IsUserEngaged() && IsUserPointing(), 100));

		SetTransitionRule(PointingState.PointingStart, PointingState.Pointed, new TimedRule(() =>
		{
			if (IsUserEngaged() && IsUserPointing(checkValidity: true))
			{
				Vector3 pos = DataStore.GetVector3Value("user:pointPos");
				var currentPointedAtObject = GlobalHelper.FindTargetByLocation(pos, .1f, LayerMask.GetMask("Blocks"));

				bool makeTransition = false;

				if (currentPointedAtObject != null)
				{
					makeTransition = LastPointedAtObject == null || currentPointedAtObject == LastPointedAtObject;

					LastPointedAtObject = currentPointedAtObject;
					LastPointedAtLocation = currentPointedAtObject.transform.position;
				}
				else
				{
					bool isPointedAtLocationStable = IsPointedAtLocationStable();
					//Debug.LogWarning("Stable: " + isPointedAtLocationStable);
					makeTransition = LastPointedAtObject == null && isPointedAtLocationStable;

					LastPointedAtObject = null;
					if (isPointedAtLocationStable)
					{
						LastPointedAtLocation = AveragePointedAtLocation;
					}
					else
					{
						LastPointedAtLocation = default;
					}
				}

				return makeTransition;
			}
			return false;
		}, 700));

		SetTransitionRule(PointingState.Pointed, PointingState.PointingStart, Rule.True);

		SetTransitionRule(PointingState.PointingStart, PointingState.PointingStop, new TimedRule(() => !IsUserEngaged() || !IsUserPointing(checkValidity: true), 100));

		Evaluated += (object sender, EventArgs e) =>
		{
			Vector3 pos = DataStore.GetVector3Value("user:pointPos");
			lastNPositions.Enqueue(pos);
			if (lastNPositions.Count > N)
				lastNPositions.Dequeue();
		};
	}
}
