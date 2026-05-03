using Sandbox;

public sealed class KobManager : Component
{
	public static KobManager Instance { get; private set; }

	[Property] public int ScoreToWin { get; set; } = 200;

	[Sync] public int ScoreRed   { get; set; }
	[Sync] public int ScoreBlue  { get; set; }
	[Sync] public int ScoreGreen { get; set; }

	protected override void OnStart()
	{
		Instance = this;
	}

	public void AddPoint( KobTeam team )
	{
		if ( IsProxy ) return;

		switch ( team )
		{
			case KobTeam.Red:   ScoreRed++;   break;
			case KobTeam.Blue:  ScoreBlue++;  break;
			case KobTeam.Green: ScoreGreen++; break;
		}

		if ( ScoreRed   >= ScoreToWin ) { EndGame( KobTeam.Red );   return; }
		if ( ScoreBlue  >= ScoreToWin ) { EndGame( KobTeam.Blue );  return; }
		if ( ScoreGreen >= ScoreToWin ) { EndGame( KobTeam.Green ); return; }
	}

	[Rpc.Broadcast]
	void EndGame( KobTeam winner )
	{
		Log.Info( $"Game over! {winner} wins!" );
	}
}
