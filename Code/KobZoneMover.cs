using Sandbox;
using System.Collections.Generic;

public sealed class KobZoneMover : Component
{
	[Property] public List<GameObject> Waypoints    { get; set; } = new();
	[Property] public float            MoveInterval { get; set; } = 60f;
	[Property] public float            MoveSpeed    { get; set; } = 50f;

	[Sync] public float TimeUntilMove { get; set; }

	private int     _waypointIndex;
	private bool    _moving;
	private Vector3 _target;

	protected override void OnStart()
	{
		if ( IsProxy || Waypoints.Count == 0 ) return;
		WorldPosition = Waypoints[0].WorldPosition;
		TimeUntilMove = MoveInterval;
	}

	public void ResetToFirstWaypoint()
	{
		if ( Waypoints.Count == 0 ) return;
		_waypointIndex = 0;
		_moving        = false;
		TimeUntilMove  = MoveInterval;
		WorldPosition  = Waypoints[0].WorldPosition;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || Waypoints.Count == 0 ) return;

		if ( _moving )
		{
			float remaining = Vector3.DistanceBetween( WorldPosition, _target );
			float step      = Time.Delta * MoveSpeed;

			if ( remaining <= step )
			{
				WorldPosition = _target;
				_moving       = false;
				TimeUntilMove = MoveInterval;
			}
			else
			{
				WorldPosition += ( _target - WorldPosition ).Normal * step;
			}
		}
		else
		{
			TimeUntilMove -= Time.Delta;
			if ( TimeUntilMove <= 0f )
			{
				_waypointIndex = ( _waypointIndex + 1 ) % Waypoints.Count;
				_target        = Waypoints[_waypointIndex].WorldPosition;
				_moving        = true;
			}
		}
	}
}
