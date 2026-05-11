using Sandbox;

public sealed class KobAchievementTracker : Component
{
	private const string SaveFile = "achievement_stats.json";

	private KobPlayer           _player;
	private KobAchievementStats _stats;

	private int   _lastKills;
	private int   _lastDeaths;
	private int   _lastZonePoints;
	private float _lastTimePlayed;
	private float _saveTimer;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		_player = Components.Get<KobPlayer>();
		_stats  = Load();

		_lastKills      = _player.Kills;
		_lastDeaths     = _player.Deaths;
		_lastZonePoints = _player.ZonePoints;
		_lastTimePlayed = _player.TimePlayed;

		CheckWelcome();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || _player is null ) return;

		bool dirty = false;

		dirty |= TrackKills();
		dirty |= TrackDeaths();
		dirty |= TrackZonePoints();
		TrackTime();

		_saveTimer += Time.Delta;
		if ( dirty || _saveTimer >= 60f )
		{
			Save();
			_saveTimer = 0f;
		}
	}

	// ── Trackers ─────────────────────────────────────────────────────────────

	private bool TrackKills()
	{
		int delta = _player.Kills - _lastKills;
		if ( delta <= 0 ) return false;

		_stats.TotalKills += delta;
		_lastKills         = _player.Kills;

		CheckMilestone( _stats.TotalKills,
			(1,   "first_blood"),
			(10,  "killer_10"),
			(50,  "killer_50"),
			(100, "killer_100") );

		return true;
	}

	private bool TrackDeaths()
	{
		int delta = _player.Deaths - _lastDeaths;
		if ( delta <= 0 ) return false;

		_stats.TotalDeaths += delta;
		_lastDeaths         = _player.Deaths;

		CheckMilestone( _stats.TotalDeaths,
			(1,   "death_1"),
			(10,  "death_10"),
			(50,  "death_50"),
			(100, "death_100") );

		return true;
	}

	private bool TrackZonePoints()
	{
		int delta = _player.ZonePoints - _lastZonePoints;
		if ( delta <= 0 ) return false;

		_stats.TotalZonePoints += delta;
		_lastZonePoints         = _player.ZonePoints;

		CheckMilestone( _stats.TotalZonePoints,
			(10,  "zone_10"),
			(50,  "zone_50"),
			(200, "zone_200"),
			(500, "zone_500") );

		return true;
	}

	private void TrackTime()
	{
		float delta = _player.TimePlayed - _lastTimePlayed;
		if ( delta <= 0f ) return;

		_stats.TotalTimePlayed += delta;
		_lastTimePlayed         = _player.TimePlayed;

		CheckMilestone( (int)_stats.TotalTimePlayed,
			(300,   "time_5m"),
			(1800,  "time_30m"),
			(7200,  "time_2h"),
			(36000, "time_10h") );
	}

	private void CheckWelcome()
	{
		if ( _stats.WelcomeDone ) return;
		_stats.WelcomeDone = true;
		Unlock( "welcome" );
		Save();
	}

	// ── Helpers ──────────────────────────────────────────────────────────────

	private static void CheckMilestone( int current, params (int threshold, string id)[] milestones )
	{
		foreach ( var (threshold, id) in milestones )
			if ( current >= threshold )
				Unlock( id );
	}

	private static void Unlock( string id )
	{
		Sandbox.Services.Achievements.Unlock( id );
	}

	private static KobAchievementStats Load()
	{
		if ( FileSystem.Data.FileExists( SaveFile ) )
			return FileSystem.Data.ReadJson<KobAchievementStats>( SaveFile ) ?? new();
		return new();
	}

	private void Save()
	{
		FileSystem.Data.WriteJson( SaveFile, _stats );
	}
}
