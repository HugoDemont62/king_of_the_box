using Sandbox;

public sealed class KobWeaponPickup : Component
{
	private Collider _collider;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		_collider = Components.Get<SphereCollider>();
		if ( _collider is not null )
			_collider.OnTriggerEnter += OnTriggerEnter;
	}

	protected override void OnDestroy()
	{
		if ( _collider is not null )
			_collider.OnTriggerEnter -= OnTriggerEnter;
	}

	private void OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) return;

		// KobPlayer est sur le root du joueur; le collider qui touche est sur un GO enfant
		// → on remonte la hiérarchie jusqu'à trouver KobPlayer
		KobPlayer player = null;
		var go = other.GameObject;
		while ( go is not null && player is null )
		{
			player = go.Components.Get<KobPlayer>();
			go = go.Parent;
		}
		if ( player is null ) return;

		var weapon = Components.Get<KobWeapon>();
		if ( weapon is null ) return;

		player.PickupWeapon( weapon );
	}
}
