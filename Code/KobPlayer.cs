using Sandbox;
using System.Linq;

public sealed class KobPlayer : Component
{
	[Property] public float WalkSpeed             { get; set; } = 300f;
	[Property] public float JumpForce             { get; set; } = 500f;
	[Property] public float SprintSpeedMultiplier { get; set; } = 1.5f;

	[Sync] public KobTeam Team             { get; set; } = KobTeam.None;
	[Sync] public int     Kills            { get; set; }
	[Sync] public int     Deaths           { get; set; }
	[Sync] public int     Assists          { get; set; }
	[Sync] public string  PlayerName       { get; set; } = "";
	[Sync] public int     ZonePoints       { get; set; }
	[Sync] public float   TimePlayed       { get; set; }
	[Sync] public int     Ping             { get; set; }
	[Sync] public int     ActiveWeaponSlot { get; set; }

	private PlayerController _playerController;
	private KobWeapon[]      _weapons = System.Array.Empty<KobWeapon>();
	private KobHealth        _health;
	private CameraComponent  _camera;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		PlayerName        = Connection.Local?.DisplayName ?? GameObject.Name;
		_playerController = Components.Get<PlayerController>();
		_health           = Components.Get<KobHealth>();

		_weapons = Components.GetAll<KobWeapon>()
			.OrderBy( w => w.WeaponSlot )
			.ToArray();

		if ( _weapons.Length > 0 )
			SetActiveSlot( 0 );

		if ( _playerController is not null )
		{
			_playerController.WalkSpeed        = WalkSpeed;
			_playerController.RunSpeed         = WalkSpeed * SprintSpeedMultiplier;
			_playerController.JumpSpeed        = JumpForce;
			_playerController.UseInputControls = false;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( _playerController is null )
			_playerController = Components.Get<PlayerController>();
		if ( _health is null )
			_health = Components.Get<KobHealth>();
		if ( _camera is null )
			_camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( c => c.Enabled );

		TimePlayed += Time.Delta;
		Ping        = (int)( Connection.Local?.Ping ?? 0f );

		bool isDead = _health?.IsDead ?? false;

		if ( _playerController is not null )
			_playerController.UseInputControls = Team != KobTeam.None && !isDead;

		if ( isDead || Team == KobTeam.None ) return;

		HandleWeaponSwitch();
		HandleFire();
		HandleReload();
	}

	private void HandleWeaponSwitch()
	{
		if ( _weapons.Length < 2 ) return;
		if ( Input.Pressed( "slot1" ) ) SetActiveSlot( 0 );
		if ( Input.Pressed( "slot2" ) ) SetActiveSlot( 1 );
	}

	private void SetActiveSlot( int slot )
	{
		ActiveWeaponSlot = slot;
		for ( int i = 0; i < _weapons.Length; i++ )
			_weapons[i].Enabled = i == slot;
	}

	public void ResetWeaponState()
	{
		if ( _weapons.Length > 0 )
			SetActiveSlot( 0 );
	}

	private void HandleFire()
	{
		if ( _weapons.Length == 0 ) return;
		int slot   = System.Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		var weapon = _weapons[slot];
		if ( weapon is null || !weapon.Enabled ) return;

		if ( Input.Down( "attack1" ) )
		{
			Vector3 origin    = _camera?.WorldPosition    ?? WorldPosition + Vector3.Up * 64f;
			Vector3 direction = _camera?.WorldRotation.Forward ?? WorldRotation.Forward;

			weapon.FireRequest( origin, direction );
		}
	}

	private void HandleReload()
	{
		if ( _weapons.Length == 0 ) return;
		if ( !Input.Pressed( "reload" ) ) return;
		int slot = System.Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		_weapons[slot]?.ReloadRequest();
	}
}
