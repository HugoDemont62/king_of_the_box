using Sandbox;

public sealed class KobWeaponProjectile : KobWeapon
{
	[Property] public float ProjectileSpeed { get; set; } = 1500f;
	[Property] public float SplashRadius    { get; set; } = 150f;

	protected override void DoFire( Vector3 origin, Vector3 direction )
	{
		var go = new GameObject( true, "Projectile" );
		go.WorldPosition = origin + direction * 20f;
		go.WorldRotation = Rotation.LookAt( direction );

		var proj = go.Components.Create<KobProjectile>();
		proj.Damage        = Damage;
		proj.SplashRadius  = SplashRadius;
		proj.Shooter       = Components.Get<KobPlayer>();
		proj.ShooterObject = GameObject;

		var rb = go.Components.Create<Rigidbody>();
		rb.Velocity = direction * ProjectileSpeed;
		rb.Gravity  = true;

		var col = go.Components.Create<SphereCollider>();
		col.Radius    = 8f;
		col.IsTrigger = true;

		go.NetworkSpawn();
	}
}
