using Sandbox;

public class KobWeapon : Component
{
	[Property] public int   WeaponSlot  { get; set; } = 0;
	[Property] public float Damage      { get; set; } = 25f;
	[Property] public float FireRate    { get; set; } = 5f;
	[Property] public int   AmmoMax     { get; set; } = 30;
	[Property] public float ReloadTime  { get; set; } = 2f;

	[Sync] public int  AmmoCurrent { get; set; }
	[Sync] public bool IsReloading { get; set; }

	private float _nextFireTime;
	private float _reloadEndTime;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		AmmoCurrent = AmmoMax;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( IsReloading && Time.Now >= _reloadEndTime )
		{
			AmmoCurrent = AmmoMax;
			IsReloading = false;
		}
	}

	[Rpc.Host]
	public void FireRequest( Vector3 origin, Vector3 direction )
	{
		if ( IsProxy || IsReloading ) return;
		if ( AmmoCurrent <= 0 ) { StartReload(); return; }
		if ( Time.Now < _nextFireTime ) return;

		AmmoCurrent--;
		_nextFireTime = Time.Now + 1f / FireRate;
		DoFire( origin, direction );

		if ( AmmoCurrent == 0 )
			StartReload();
	}

	[Rpc.Host]
	public void ReloadRequest()
	{
		if ( IsProxy ) return;
		StartReload();
	}

	private void StartReload()
	{
		if ( IsProxy || IsReloading || AmmoCurrent == AmmoMax ) return;
		IsReloading    = true;
		_reloadEndTime = Time.Now + ReloadTime;
	}

	protected virtual void DoFire( Vector3 origin, Vector3 direction ) { }
}
