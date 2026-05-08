using Sandbox;
using System;
using System.Linq;

public sealed class KobRespawnController : Component
{
	[Property] public float RespawnTime { get; set; } = 5f;

	[Sync] public bool   IsDead      { get; set; }
	[Sync] public float  RespawnTimer { get; set; }
	[Sync] public string KillerName   { get; set; } = "";

	private KobHealth        _health;
	private PlayerController _playerController;

	protected override void OnStart()
	{
		_health           = Components.Get<KobHealth>();
		_playerController = Components.Get<PlayerController>();

		if ( _health is not null )
		{
			_health.OnDeath += OnDeath;
		}
		else
		{
			Log.Warning( $"KobRespawnController on {GameObject.Name} has no KobHealth sibling." );
		}
	}

	protected override void OnDestroy()
	{
		if ( _health is not null )
			_health.OnDeath -= OnDeath;
	}

	private void OnDeath( KobPlayer killer )
	{
		if ( IsProxy ) return;

		IsDead       = true;
		RespawnTimer = RespawnTime;
		KillerName   = killer?.PlayerName ?? "Inconnu";

		if ( _playerController is not null )
			_playerController.UseInputControls = false;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || !IsDead ) return;

		RespawnTimer = MathF.Max( 0f, RespawnTimer - Time.Delta );
		if ( RespawnTimer <= 0f )
		{
			IsDead = false;
			Respawn();
		}
	}

	private void Respawn()
	{
		var player = Components.Get<KobPlayer>();
		KobManager.Instance?.RespawnPlayer( player );
	}

	public void OnRespawn()
	{
		if ( IsProxy ) return;

		IsDead       = false;
		RespawnTimer = 0f;
		KillerName   = "";

		if ( _playerController is not null )
			_playerController.UseInputControls = true;
	}
}
