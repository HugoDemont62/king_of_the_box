using Sandbox;

public sealed class KobCaptureZone : Component
{
	[Property] public int   PointsPerSecond { get; set; } = 1;
	[Property] public float CaptureRadius   { get; set; } = 150f;

	[Sync] public KobTeam ControllingTeam { get; set; } = KobTeam.None;
	[Sync] public float   CaptureProgress { get; set; } = 0f;
	[Sync] public bool    IsContested     { get; set; } = false;

	private float         _pointTimer;
	private ModelRenderer _disc;

	protected override void OnStart()
	{
		// Créer le disque visuel comme enfant
		var go = new GameObject( true, "ZoneDisc" );
		go.Parent = GameObject;
		go.LocalPosition = Vector3.Zero;
		go.LocalRotation = Rotation.FromPitch( 90 );

		_disc = go.Components.Create<ModelRenderer>();
		_disc.Model = Model.Load( "models/dev/plane.vmdl" );

		float scale = CaptureRadius / 50f;
		go.LocalScale = new Vector3( scale, scale, 1f );
	}

	protected override void OnUpdate()
	{
		if ( _disc is null ) return;

		// Pulsation douce
		float pulse = 0.7f + MathF.Sin( Time.Now * 2.5f ) * 0.15f;

		_disc.Tint = ControllingTeam switch
		{
			KobTeam.Red   => new Color( 0.9f, 0.2f, 0.2f, pulse ),
			KobTeam.Blue  => new Color( 0.2f, 0.4f, 0.9f, pulse ),
			KobTeam.Green => new Color( 0.2f, 0.85f, 0.2f, pulse ),
			_             => IsContested
				? new Color( 0.9f, 0.85f, 0.1f, pulse )
				: new Color( 0.8f, 0.8f, 0.8f, pulse * 0.6f ),
		};
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		var playersInZone = Scene.GetAllComponents<KobPlayer>()
			.Where( p => p.Team != KobTeam.None &&
			             Vector3.DistanceBetween( p.WorldPosition, WorldPosition ) < CaptureRadius )
			.ToList();

		int red   = playersInZone.Count( p => p.Team == KobTeam.Red );
		int blue  = playersInZone.Count( p => p.Team == KobTeam.Blue );
		int green = playersInZone.Count( p => p.Team == KobTeam.Green );

		int teamsPresent = ( red > 0 ? 1 : 0 ) + ( blue > 0 ? 1 : 0 ) + ( green > 0 ? 1 : 0 );

		IsContested     = teamsPresent > 1;
		ControllingTeam = teamsPresent == 1
			? ( red > 0 ? KobTeam.Red : blue > 0 ? KobTeam.Blue : KobTeam.Green )
			: KobTeam.None;

		if ( IsContested || ControllingTeam == KobTeam.None )
		{
			_pointTimer     = 0f;
			CaptureProgress = 0f;
			return;
		}

		float interval = 1f / PointsPerSecond;
		_pointTimer += Time.Delta;
		CaptureProgress = _pointTimer / interval;

		while ( _pointTimer >= interval )
		{
			_pointTimer     -= interval;
			CaptureProgress  = 0f;
			KobManager.Instance?.AddPoint( ControllingTeam );
		}
	}
}
