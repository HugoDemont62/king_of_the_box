using Sandbox;

public sealed class KobTeamSelector : Component
{
	[Sync] public bool TeamSelected { get; private set; }
	private float _selectionTimeout = 0f;
	private const float SELECTION_TIME = 30f; // 30 secondes avant auto-sélection

	protected override void OnUpdate()
	{
		if ( IsProxy || TeamSelected ) return;

		// Gestion du timeout
		_selectionTimeout += Time.Delta;
		if ( _selectionTimeout > SELECTION_TIME )
		{
			// Auto-sélection aléatoire si pas de choix
			var teams = new[] { KobTeam.Red, KobTeam.Blue, KobTeam.Green };
			ChooseTeam( teams[Game.Random.Next( teams.Length )] );
			return;
		}

		// Input - Touches rapides
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

		if ( spawnPoints.Count == 0 )
		{
			Log.Warning( $"No spawn points found for team {team}!" );
			return;
		}

		var spawn = Game.Random.FromList( spawnPoints );
		WorldPosition = spawn.WorldPosition;

		Log.Info( $"Player spawned in {team} team at {WorldPosition}" );
	}
}
