using Sandbox;

public sealed class KobTeamSelector : Component
{
	[Sync] public bool TeamSelected { get; private set; }

	protected override void OnUpdate()
	{
		if ( IsProxy || TeamSelected ) return;

		if ( Input.Pressed( "slot1" ) ) ChooseTeam( KobTeam.Red );
		if ( Input.Pressed( "slot2" ) ) ChooseTeam( KobTeam.Blue );
		if ( Input.Pressed( "slot3" ) ) ChooseTeam( KobTeam.Green );
	}

	public void ChooseTeam( KobTeam team )
	{
		TeamSelected = true;

		var player = Components.Get<KobPlayer>();
		if ( player is not null )
			player.Team = team;

		var spawnPoints = Scene.GetAllComponents<KobSpawnPoint>()
			.Where( s => s.Team == team )
			.ToList();

		if ( spawnPoints.Count == 0 ) return;

		var spawn = Game.Random.FromList( spawnPoints );
		WorldPosition = spawn.WorldPosition;
	}
}
