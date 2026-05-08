using Sandbox;

public class KobWeaponHitscan : KobWeapon
{
	[Property] public float Range  { get; set; } = 5000f;
	[Property] public float Spread { get; set; } = 0.02f;

	protected override void DoFire( Vector3 origin, Vector3 direction )
	{
		var right    = direction.Cross( Vector3.Up ).Normal;
		var up       = direction.Cross( right ).Normal;
		var finalDir = ( direction
			+ right * Game.Random.Float( -Spread, Spread )
			+ up    * Game.Random.Float( -Spread, Spread ) ).Normal;

		var tr = Scene.Trace
			.Ray( new Ray( origin, finalDir ), Range )
			.IgnoreGameObjectHierarchy( OwnerPlayer?.GameObject ?? GameObject )
			.Run();

		if ( !tr.Hit ) return;

		var health = tr.GameObject?.Components.Get<KobHealth>();
		if ( health is null ) return;

		health.TakeDamage( Damage, OwnerPlayer );
	}
}
