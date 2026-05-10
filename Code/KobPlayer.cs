using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class KobPlayer : Component
{
	[Property] public float WalkSpeed             { get; set; } = 300f;
	[Property] public float JumpForce             { get; set; } = 500f;
	[Property] public float SprintSpeedMultiplier { get; set; } = 1.5f;

	[Property] public List<GameObject> StartingWeaponPrefabs { get; set; } = new();

	[Property] public float WeaponForwardBias { get; set; } = 0f;

	[Sync] public KobTeam Team             { get; set; } = KobTeam.None;
	[Sync] public int     Kills            { get; set; }
	[Sync] public int     Deaths           { get; set; }
	[Sync] public int     Assists          { get; set; }
	[Sync] public string  PlayerName       { get; set; } = "";
	[Sync] public int     ZonePoints       { get; set; }
	[Sync] public float   TimePlayed       { get; set; }
	[Sync] public int     Ping             { get; set; }
	[Sync] public int     ActiveWeaponSlot { get; set; }
	[Sync] public int     HoldType         { get; set; } = 0;

	private PlayerController     _playerController;
	private KobWeapon[]          _weapons = Array.Empty<KobWeapon>();
	private KobHealth            _health;
	private CameraComponent      _camera;
	private SkinnedModelRenderer _bodyRenderer;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		PlayerName        = Connection.Local?.DisplayName ?? GameObject.Name;
		_playerController = Components.Get<PlayerController>();
		_health           = Components.Get<KobHealth>();

		if ( _health is not null )
			_health.OnDeath += HandleDeath;

		if ( _playerController is not null )
		{
			_playerController.WalkSpeed        = WalkSpeed;
			_playerController.RunSpeed         = WalkSpeed * SprintSpeedMultiplier;
			_playerController.JumpSpeed        = JumpForce;
			_playerController.UseInputControls = false;
		}

		GiveStartingWeapons();
	}

	protected override void OnDestroy()
	{
		if ( _health is not null )
			_health.OnDeath -= HandleDeath;
	}

	private void HandleDeath( KobPlayer killer )
	{
		DropActiveWeapon();
	}

	protected override void OnUpdate()
	{
		// Body renderer — résolu et animé pour tous les clients (local + proxy)
		if ( _bodyRenderer is not null && !_bodyRenderer.IsValid() ) _bodyRenderer = null;
		if ( _bodyRenderer is null )
		{
			var body = GameObject.Children.FirstOrDefault( c => c.Name == "Body" );
			_bodyRenderer = body?.Components.Get<SkinnedModelRenderer>();
		}
		_bodyRenderer?.Set( "holdtype", HoldType );

		if ( IsProxy ) return;

		if ( _playerController is not null && !_playerController.IsValid() ) _playerController = null;
		if ( _health          is not null && !_health.IsValid()           ) { _health = null; }

		if ( _playerController is null )
			_playerController = Components.Get<PlayerController>();
		if ( _health is null )
		{
			_health = Components.Get<KobHealth>();
			if ( _health is not null )
				_health.OnDeath += HandleDeath;
		}
		if ( _camera is null )
			_camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( c => c.Enabled );

		TimePlayed += Time.Delta;
		Ping        = (int)( Connection.Local?.Ping ?? 0f );

		bool isDead = _health?.IsDead ?? false;

		if ( _playerController is not null )
		{
			_playerController.UseInputControls = Team != KobTeam.None && !isDead;
			_playerController.ThirdPerson      = true; // toujours en vue 3e personne
		}

		if ( isDead || Team == KobTeam.None )
		{
			HoldType = 0;
			return;
		}

		UpdateHoldType();
		HandleWeaponSwitch();
		HandleFire();
		HandleReload();
		UpdateWeaponTransform();
	}

	private void UpdateHoldType()
	{
		if ( _weapons.Length == 0 ) { HoldType = 0; return; }
		int slot   = Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		var weapon = _weapons[slot];
		if ( weapon is null || !weapon.IsValid() || !weapon.GameObject.Enabled ) { HoldType = 0; return; }

		HoldType = weapon is KobWeaponShotgun ? 3 : 2; // 3 = shotgun, 2 = rifle/pistolet
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
			_weapons[i].GameObject.Enabled = ( i == slot );
	}

	private void HandleFire()
	{
		if ( _weapons.Length == 0 ) return;
		int slot   = Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		var weapon = _weapons[slot];
		if ( weapon is null || !weapon.IsValid() || !weapon.GameObject.Enabled ) return;

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
		int slot = Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		_weapons[slot]?.ReloadRequest();
	}

	// ── Weapon management ──────────────────────────────────────────────────

	private GameObject GetOrCreateWeaponsParent()
	{
		var existing = GameObject.Children.FirstOrDefault( c => c.Name == "Weapons" );
		if ( existing is not null ) return existing;
		var go = new GameObject( true, "Weapons" );
		go.SetParent( GameObject );
		return go;
	}

	public void GiveStartingWeapons()
	{
		if ( IsProxy ) return;

		var parent = GetOrCreateWeaponsParent();
		foreach ( var child in parent.Children.ToList() )
			child.Destroy();

		foreach ( var prefab in StartingWeaponPrefabs )
		{
			if ( prefab is null ) continue;
			var weaponGO = prefab.Clone();
			weaponGO.SetParent( parent );
			weaponGO.LocalPosition = Vector3.Zero;
			weaponGO.LocalRotation = Rotation.Identity;
			weaponGO.Enabled       = false;
		}

		RefreshWeapons();
		if ( _weapons.Length > 0 )
			SetActiveSlot( 0 );
	}

	private void UpdateWeaponTransform()
	{
		if ( _weapons.Length == 0 || _bodyRenderer is null ) return;
		int slot   = Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		var weapon = _weapons[slot];
		if ( weapon is null || !weapon.IsValid() || !weapon.GameObject.Enabled ) return;

		var attach = _bodyRenderer.GetAttachment( "hold_r" );
		if ( attach is null ) return;

		var weaponRot = attach.Value.Rotation * weapon.HoldAngles.ToRotation();
		weapon.GameObject.WorldPosition = attach.Value.Position
		                                + attach.Value.Rotation * weapon.HoldOffset
		                                + weaponRot.Forward * WeaponForwardBias;
		weapon.GameObject.WorldRotation = weaponRot;
	}

	public void DropActiveWeapon()
	{
		if ( IsProxy || _weapons.Length == 0 ) return;

		int slot   = Math.Clamp( ActiveWeaponSlot, 0, _weapons.Length - 1 );
		var weapon = _weapons[slot];
		if ( weapon is null ) return;

		var dropPos = WorldPosition + Vector3.Up * 50f;

		var pickupGO = new GameObject( true, $"Pickup_{weapon.WeaponName}" );
		pickupGO.WorldPosition = dropPos;

		CopyWeaponToGO( weapon, pickupGO );

		var srcRenderer = weapon.Components.Get<ModelRenderer>();
		if ( srcRenderer is not null )
		{
			var dstRenderer = pickupGO.Components.Create<ModelRenderer>();
			dstRenderer.Model = srcRenderer.Model;
		}

		pickupGO.Components.Create<KobWeaponPickup>();

		var rb = pickupGO.Components.Create<Rigidbody>();
		rb.Velocity = WorldRotation.Forward * 80f + Vector3.Up * 60f;
		rb.Gravity  = true;

		var col = pickupGO.Components.Create<SphereCollider>();
		col.Radius    = 40f;
		col.IsTrigger = true;

		pickupGO.NetworkSpawn();

		weapon.GameObject.Destroy();
		RefreshWeapons();

		if ( _weapons.Length > 0 )
			SetActiveSlot( 0 );
		else
			ActiveWeaponSlot = 0;
	}

	public void PickupWeapon( KobWeapon weapon )
	{
		if ( IsProxy ) return;

		bool slotTaken = Array.Exists( _weapons, w => w is not null && w.WeaponSlot == weapon.WeaponSlot );
		if ( slotTaken ) return;

		var parent = GetOrCreateWeaponsParent();
		var newGO  = new GameObject( true, weapon.WeaponName );
		newGO.SetParent( parent );
		newGO.LocalPosition = Vector3.Zero;
		newGO.LocalRotation = Rotation.Identity;
		newGO.Enabled       = false;

		CopyWeaponToGO( weapon, newGO );

		var srcRenderer = weapon.Components.Get<ModelRenderer>();
		if ( srcRenderer is not null )
		{
			var dstRenderer = newGO.Components.Create<ModelRenderer>();
			dstRenderer.Model = srcRenderer.Model;
		}

		weapon.GameObject.Destroy();
		RefreshWeapons();
	}

	private void RefreshWeapons()
	{
		_weapons = Components.GetAll<KobWeapon>( FindMode.EverythingInSelfAndDescendants )
			.Where( w => w.IsValid() )
			.OrderBy( w => w.WeaponSlot )
			.ToArray();
	}

	private static void CopyWeaponToGO( KobWeapon src, GameObject dst )
	{
		KobWeapon dstWeapon;

		if ( src is KobWeaponShotgun sg )
		{
			var c = dst.Components.Create<KobWeaponShotgun>();
			c.PelletCount = sg.PelletCount;
			c.Range       = sg.Range;
			c.Spread      = sg.Spread;
			dstWeapon = c;
		}
		else if ( src is KobWeaponHitscan hs )
		{
			var c = dst.Components.Create<KobWeaponHitscan>();
			c.Range  = hs.Range;
			c.Spread = hs.Spread;
			dstWeapon = c;
		}
		else if ( src is KobWeaponProjectile proj )
		{
			var c = dst.Components.Create<KobWeaponProjectile>();
			c.ProjectileSpeed = proj.ProjectileSpeed;
			c.SplashRadius    = proj.SplashRadius;
			dstWeapon = c;
		}
		else
		{
			dstWeapon = dst.Components.Create<KobWeapon>();
		}

		dstWeapon.WeaponName   = src.WeaponName;
		dstWeapon.WeaponSlot   = src.WeaponSlot;
		dstWeapon.Damage       = src.Damage;
		dstWeapon.FireRate     = src.FireRate;
		dstWeapon.AmmoMax      = src.AmmoMax;
		dstWeapon.AmmoCurrent  = src.AmmoCurrent;
		dstWeapon.ReloadTime   = src.ReloadTime;
		dstWeapon.FireSound    = src.FireSound;
		dstWeapon.ReloadSound  = src.ReloadSound;
		dstWeapon.HoldOffset   = src.HoldOffset;
		dstWeapon.HoldAngles   = src.HoldAngles;
	}

	// Called by KobManager after respawn — delegates to GiveStartingWeapons
	public void ResetWeaponState()
	{
		GiveStartingWeapons();
	}
}
