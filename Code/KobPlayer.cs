using Sandbox;

public sealed class KobPlayer : Component
{
	[Property] public float WalkSpeed             { get; set; } = 300f;
	[Property] public float JumpForce             { get; set; } = 500f;
	[Property] public float SprintSpeedMultiplier { get; set; } = 1.5f;

	[Sync] public KobTeam Team       { get; set; } = KobTeam.None;
	[Sync] public int     Kills      { get; set; }
	[Sync] public int     Deaths     { get; set; }
	[Sync] public int     Assists    { get; set; }
	[Sync] public string  PlayerName { get; set; } = "";
	[Sync] public int     ZonePoints { get; set; }
	[Sync] public float   TimePlayed { get; set; }
	[Sync] public int     Ping       { get; set; }

	private PlayerController _playerController;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		PlayerName = Connection.Local?.DisplayName ?? GameObject.Name;
		_playerController = Components.Get<PlayerController>();
		if ( _playerController is null ) return;

		_playerController.WalkSpeed = WalkSpeed;
		_playerController.RunSpeed  = WalkSpeed * SprintSpeedMultiplier;
		_playerController.JumpSpeed = JumpForce;
		_playerController.UseInputControls = false;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		TimePlayed += Time.Delta;
		Ping        = (int)(Connection.Local?.Ping ?? 0f);

		if ( _playerController is null )
			_playerController = Components.Get<PlayerController>();

		if ( _playerController is not null )
			_playerController.UseInputControls = (Team != KobTeam.None);
	}
}
