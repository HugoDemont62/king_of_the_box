using Sandbox;

public sealed class KobPlayer : Component
{
	[Property] public float WalkSpeed { get; set; } = 300f;
	[Property] public float JumpForce { get; set; } = 300f;

	[Sync] public KobTeam Team { get; set; } = KobTeam.None;

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		var ctrl = Components.Get<PlayerController>();
		if ( ctrl is null ) return;
		ctrl.WalkSpeed = WalkSpeed;
		ctrl.RunSpeed  = WalkSpeed * 1.5f;
		ctrl.JumpSpeed = JumpForce;
		ctrl.UseInputControls = false;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		var ctrl = Components.Get<PlayerController>();
		if ( ctrl is not null )
			ctrl.UseInputControls = Team != KobTeam.None;
	}
}
