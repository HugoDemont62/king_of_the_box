using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class KobManager : Component, Component.INetworkListener
{
	public static KobManager Instance { get; private set; }

	[Property] public int ScoreToWin { get; set; } = 200;
	[Property] public GameObject PlayerPrefab { get; set; }

	[Sync] public int ScoreRed   { get; set; }
	[Sync] public int ScoreBlue  { get; set; }
	[Sync] public int ScoreGreen { get; set; }

	[Sync] public int PlayersRed { get; set; }
	[Sync] public int PlayersBlue { get; set; }
	[Sync] public int PlayersGreen { get; set; }

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

		// Broadcast l'arrivée du joueur
		OnPlayerSpawned( conn.DisplayName );
	}

	void INetworkListener.OnDisconnected( Connection conn )
	{
		if ( !_players.TryGetValue( conn.SteamId, out var player ) ) return;
		player.Destroy();
		_players.Remove( conn.SteamId );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		UpdatePlayerCounts();
	}

	private void UpdatePlayerCounts()
	{
		var allPlayers = Scene.GetAllComponents<KobPlayer>();
		PlayersRed = allPlayers.Count( p => p.Team == KobTeam.Red );
		PlayersBlue = allPlayers.Count( p => p.Team == KobTeam.Blue );
		PlayersGreen = allPlayers.Count( p => p.Team == KobTeam.Green );
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
	void OnPlayerSpawned( string playerName )
	{
		Log.Info( $"⚔️ {playerName} a rejoint le jeu!" );
	}

	[Rpc.Broadcast]
	void EndGame( KobTeam winner )
	{
		Log.Info( $"🏆 Game over! {winner} team wins! 🏆" );
	}
}
