using Sandbox;
using System.Collections.Generic;

public sealed class KobManager : Component, Component.INetworkListener
{
	public static KobManager Instance { get; private set; }

	[Property] public int ScoreToWin { get; set; } = 200;
	[Property] public GameObject PlayerPrefab { get; set; }

	[Sync] public int ScoreRed   { get; set; }
	[Sync] public int ScoreBlue  { get; set; }
	[Sync] public int ScoreGreen { get; set; }

	private readonly Dictionary<ulong, GameObject> _players = new();

	protected override void OnStart()
	{
		Instance = this;
	}

	void INetworkListener.OnActive( Connection conn )
	{
		if ( !Networking.IsHost || PlayerPrefab is null ) return;

		var player = PlayerPrefab.Clone( WorldPosition );
		player.NetworkSpawn( conn );
		_players[conn.SteamId] = player;
	}

	void INetworkListener.OnDisconnected( Connection conn )
	{
		if ( !_players.TryGetValue( conn.SteamId, out var player ) ) return;
		player.Destroy();
		_players.Remove( conn.SteamId );
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
