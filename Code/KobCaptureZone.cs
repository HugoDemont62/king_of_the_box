using Sandbox;

public sealed class KobCaptureZone : Component
{
	[Property] public int   PointsPerSecond { get; set; } = 1;
	[Property] public float CaptureRadius   { get; set; } = 150f;

	[Sync] public KobTeam ControllingTeam  { get; set; } = KobTeam.None;
	[Sync] public float   CaptureProgress  { get; set; } = 0f;
	[Sync] public bool    IsContested      { get; set; } = false;

	private float         _pointTimer;
	private ModelRenderer _renderer;

	protected override void OnStart()
	{
		_renderer = GetComponent<ModelRenderer>();
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

		UpdateVisuals();

		if ( IsContested || ControllingTeam == KobTeam.None )
		{
			_pointTimer    = 0f;
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

	void UpdateVisuals()
	{
		if ( _renderer is null ) return;

		_renderer.Tint = ControllingTeam switch
		{
			KobTeam.Red   => new Color( 0.8f, 0.2f, 0.2f ),
			KobTeam.Blue  => new Color( 0.2f, 0.2f, 0.8f ),
			KobTeam.Green => new Color( 0.2f, 0.8f, 0.2f ),
			_             => IsContested ? new Color( 0.8f, 0.8f, 0.2f ) : Color.White
		};
	}
}
