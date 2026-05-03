using Sandbox;

public sealed class KobPlayer : Component
{
	[Property] public float WalkSpeed { get; set; } = 300f;
	[Property] public float JumpForce { get; set; } = 500f;
	[Property] public float SprintSpeedMultiplier { get; set; } = 1.5f;

	[Sync] public KobTeam Team { get; set; } = KobTeam.None;

	private PlayerController _playerController;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
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

		if ( _playerController is null )
			_playerController = Components.Get<PlayerController>();

		if ( _playerController is not null )
		{
			// Active les contrôles une fois l'équipe sélectionnée
			_playerController.UseInputControls = (Team != KobTeam.None);
		}
	}
}
