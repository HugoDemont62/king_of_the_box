using Sandbox;
using System.Linq;

public sealed class KobProjectile : Component
{
	[Property] public float SplashRadius { get; set; } = 150f;
	[Property] public float LifeTime     { get; set; } = 5f;

	public float      Damage        { get; set; }
	public KobPlayer  Shooter       { get; set; }
	public GameObject ShooterObject { get; set; }

	private float    _spawnTime;
	private Collider _collider;

	protected override void OnStart()
	{
		_spawnTime = Time.Now;

		if ( IsProxy ) return;

		_collider = Components.Get<Collider>();
		if ( _collider is not null )
			_collider.OnTriggerEnter += HandleTrigger;
	}

	protected override void OnDestroy()
	{
		if ( _collider is not null )
			_collider.OnTriggerEnter -= HandleTrigger;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( Time.Now - _spawnTime > LifeTime )
			GameObject.Destroy();
	}

	private void HandleTrigger( Collider other )
	{
		if ( IsProxy ) return;

		// Vérifie que le collider touché n'appartient pas au tireur (root ou enfants)
		if ( ShooterObject is not null )
		{
			var go = other.GameObject;
			while ( go is not null )
			{
				if ( go == ShooterObject ) return;
				go = go.Parent;
			}
		}

		var targets = Scene.GetAllComponents<KobHealth>()
			.Where( h => Vector3.DistanceBetween( h.WorldPosition, WorldPosition ) <= SplashRadius )
			.ToList();

		foreach ( var health in targets )
		{
			float dist    = Vector3.DistanceBetween( health.WorldPosition, WorldPosition );
			float falloff = 1f - ( dist / SplashRadius );
			health.TakeDamage( Damage * falloff, Shooter );
		}

		GameObject.Destroy();
	}
}
