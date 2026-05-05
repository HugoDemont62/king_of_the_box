using Sandbox;
using System;

public sealed class KobHealth : Component
{
	[Property] public float MaxHealth { get; set; } = 100f;
	[Property] public float MaxArmor  { get; set; } = 50f;

	[Sync] public float Health { get; set; }
	[Sync] public float Armor  { get; set; }
	[Sync] public bool  IsDead { get; set; }

	public Action<KobPlayer> OnDeath;

	private KobPlayer _owner;
	private bool      _dying;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		_owner = Components.Get<KobPlayer>();
		if ( _owner is null )
			Log.Warning( $"KobHealth on {GameObject.Name} has no KobPlayer sibling." );
		Health = MaxHealth;
		Armor  = MaxArmor;
	}

	public void TakeDamage( float amount, KobPlayer attacker )
	{
		if ( IsProxy || IsDead || _dying ) return;

		float armorAbsorption = amount * 0.6f;
		float healthDmg       = amount - armorAbsorption;

		if ( Armor > 0f )
		{
			float absorbed = MathF.Min( Armor, armorAbsorption );
			Armor  -= absorbed;
			float leftover = armorAbsorption - absorbed;
			healthDmg += leftover;
		}
		else
		{
			healthDmg = amount;
		}

		Health = MathF.Max( 0f, Health - healthDmg );

		if ( Health <= 0f )
			Die( attacker );
	}

	public void ResetHealth()
	{
		if ( IsProxy ) return;
		Health = MaxHealth;
		Armor  = MaxArmor;
		IsDead = false;
		_dying = false;
	}

	private void Die( KobPlayer attacker )
	{
		_dying = true;
		IsDead = true;

		if ( _owner is not null ) _owner.Deaths++;
		if ( attacker is not null ) attacker.Kills++;

		OnDeath?.Invoke( attacker );
	}
}
