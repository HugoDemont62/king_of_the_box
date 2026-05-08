using Sandbox;

public sealed class KobWeaponShotgun : KobWeaponHitscan
{
	[Property] public int PelletCount { get; set; } = 6;

	protected override void DoFire( Vector3 origin, Vector3 direction )
	{
		for ( int i = 0; i < PelletCount; i++ )
			base.DoFire( origin, direction );
	}
}
