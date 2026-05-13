using Sandbox;
using System;
using System.Linq;

public sealed class KobTerrainGenerator : Component
{
	[Property] public float NoiseScale       { get; set; } = 0.004f;
	[Property] public float HeightMultiplier { get; set; } = 0.7f;

	private bool     _syncPending;
	private Terrain  _terrain;
	private int      _res;
	private int      _seed;
	private ushort[] _heights;

	public static event Action<int, ushort[], int, Terrain> OnTerrainReady;

	protected override void OnStart()
	{
		if ( Networking.IsHost )
			GenerateTerrain( Game.Random.Int( 1, 999999 ) );
	}

	protected override void OnUpdate()
	{
		if ( !_syncPending ) return;
		_syncPending = false;

		// 1) Mise à jour collision (GPU → CPU → physique)
		_terrain?.SyncCPUTexture( Terrain.SyncFlags.Height, new RectInt( 0, 0, _res, _res ) );

		// 2) Maintenant que le collider est à jour on peut raycaster sur le terrain
		RepositionSpawnPoints();
		RepositionWaypoints();

		OnTerrainReady?.Invoke( _seed, _heights, _res, _terrain );
	}

	[Rpc.Broadcast]
	public void GenerateTerrain( int seed )
	{
		var terrain = Scene.GetAllComponents<Terrain>().FirstOrDefault();
		if ( terrain is null )
		{
			Log.Warning( "[KobTerrain] Aucun composant Terrain dans la scène !" );
			return;
		}

		var storage = terrain.Storage;
		int res = storage.Resolution;
		float ox = seed * 0.17f;
		float oy = seed * 0.31f;

		Log.Info( $"[KobTerrain] Génération {res}x{res} seed={seed}..." );

		var heights = storage.HeightMap;
		ushort minH = ushort.MaxValue, maxH = 0;

		for ( int y = 0; y < res; y++ )
		{
			for ( int x = 0; x < res; x++ )
			{
				ushort h = (ushort)(SampleHeight( x, y, res, ox, oy ) * ushort.MaxValue);
				heights[y * res + x] = h;
				if ( h < minH ) minH = h;
				if ( h > maxH ) maxH = h;
			}
		}

		terrain.SyncGPUTexture();

		_terrain  = terrain;
		_res      = res;
		_seed     = seed;
		_heights  = heights;
		_syncPending = true;

		Log.Info( $"[KobTerrain] OK — hauteurs {minH}–{maxH}/65535" );
	}

	// -------------------------------------------------------------------------
	// Spawn points
	// -------------------------------------------------------------------------

	private void RepositionSpawnPoints()
	{
		if ( _terrain is null || _heights is null ) return;

		// Zones normalisées [0,1]² pour chaque équipe.
		// Le terrain est divisé en 3 secteurs bien séparés.
		(float u0, float u1, float v0, float v1) ZoneOf( KobTeam team ) => team switch
		{
			KobTeam.Red   => (0.05f, 0.38f, 0.08f, 0.50f), // haut-gauche
			KobTeam.Blue  => (0.62f, 0.95f, 0.08f, 0.50f), // haut-droite
			KobTeam.Green => (0.30f, 0.70f, 0.58f, 0.92f), // bas-centre
			_             => (0.10f, 0.90f, 0.10f, 0.90f), // None : partout
		};

		var rng = new System.Random( _seed ^ 0x1234ABCD );
		int count = 0;

		foreach ( var sp in Scene.GetAllComponents<KobSpawnPoint>() )
		{
			var (u0, u1, v0, v1) = ZoneOf( sp.Team );
			var pos = FindSpawnPos( rng, u0, u1, v0, v1, 80 );
			if ( pos.HasValue )
			{
				sp.WorldPosition = pos.Value;
				count++;
			}
		}

		Log.Info( $"[KobTerrain] {count} spawn point(s) repositionné(s)." );
	}

	private Vector3? FindSpawnPos( System.Random rng, float u0, float u1, float v0, float v1, int maxTries )
	{
		float tX = _terrain.WorldPosition.x;
		float tY = _terrain.WorldPosition.y;
		float tZ = _terrain.WorldPosition.z;
		float tSize   = _terrain.TerrainSize;
		float tHeight = _terrain.TerrainHeight;

		for ( int i = 0; i < maxTries; i++ )
		{
			float u = u0 + (float)rng.NextDouble() * (u1 - u0);
			float v = v0 + (float)rng.NextDouble() * (v1 - v0);

			int px = (int)(u * (_res - 1));
			int py = (int)(v * (_res - 1));

			float h = _heights[py * _res + px] / (float)ushort.MaxValue;

			// Exclure les rivières (trop bas) et les pics (trop haut)
			if ( h < 0.06f || h > 0.58f ) continue;

			// Exclure les pentes trop raides (falaises)
			if ( GetVariance( px, py, 10 ) > 0.003f ) continue;

			float wx = tX + u * tSize;
			float wy = tY + v * tSize;
			float wz = tZ + h * tHeight + 600f; // point de départ du raycast, bien au-dessus

			// Raycast vers le bas pour trouver la surface exacte
			var ray = new Ray( new Vector3( wx, wy, wz ), Vector3.Down );
			var tr  = Scene.Trace.Ray( ray, tHeight + 1000f ).Run();

			if ( tr.Hit )
				return tr.HitPosition + Vector3.Up * 80f;
		}

		return null;
	}

	// Variance des hauteurs dans un rayon → mesure de planéité.
	private float GetVariance( int cx, int cy, int radius )
	{
		float sum = 0f, sumSq = 0f;
		int   n   = 0;

		for ( int dy = -radius; dy <= radius; dy++ )
		{
			for ( int dx = -radius; dx <= radius; dx++ )
			{
				int nx = cx + dx, ny = cy + dy;
				if ( nx < 0 || nx >= _res || ny < 0 || ny >= _res ) continue;
				float h = _heights[ny * _res + nx] / (float)ushort.MaxValue;
				sum  += h;
				sumSq += h * h;
				n++;
			}
		}

		if ( n == 0 ) return 1f;
		float mean = sum / n;
		return sumSq / n - mean * mean;
	}

	private void RepositionWaypoints()
	{
		var mover = Scene.GetAllComponents<KobZoneMover>().FirstOrDefault();
		if ( mover is null || mover.Waypoints.Count == 0 ) return;

		// Triangle bien réparti sur la map pour donner du mouvement intéressant à la zone.
		(float u0, float u1, float v0, float v1)[] zones =
		{
			(0.10f, 0.40f, 0.10f, 0.45f), // haut-gauche
			(0.60f, 0.90f, 0.10f, 0.45f), // haut-droite
			(0.30f, 0.70f, 0.58f, 0.90f), // bas-centre
		};

		var rng = new System.Random( _seed ^ 0x5EED0001 );
		int count = 0;

		for ( int i = 0; i < mover.Waypoints.Count; i++ )
		{
			var (u0, u1, v0, v1) = zones[i % zones.Length];
			var pos = FindSpawnPos( rng, u0, u1, v0, v1, 80 );
			if ( !pos.HasValue ) continue;

			mover.Waypoints[i].WorldPosition = pos.Value;
			count++;
		}

		// Remet la zone sur le premier waypoint avec l'état interne réinitialisé.
		mover.ResetToFirstWaypoint();

		Log.Info( $"[KobTerrain] {count} waypoint(s) repositionné(s)." );
	}

	// -------------------------------------------------------------------------
	// Génération de hauteurs
	// -------------------------------------------------------------------------

	private float SampleHeight( int x, int y, int res, float ox, float oy )
	{
		float nx = x * NoiseScale + ox;
		float ny = y * NoiseScale + oy;

		// Forme continentale (très basse fréquence)
		float continent = Fbm( nx * 0.25f, ny * 0.25f, 4 );

		// Chaînes de montagnes (bruit ridgé)
		float mountain = RidgedFbm( nx, ny, 6 );
		mountain *= mountain;

		// Collines
		float hills = Fbm( nx * 3f, ny * 3f, 4 ) * 0.22f;

		// Détail fin
		float detail = Fbm( nx * 8f, ny * 8f, 3 ) * 0.05f;

		// Mélange : plaines → montagnes selon l'altitude continentale
		float blend = Math.Min( 1f, Math.Max( 0f, (continent - 0.42f) * 3.5f ) );
		float h = Lerp( continent * 0.35f + hills, mountain * 0.85f + hills * 0.4f, blend );
		h += detail;
		h = Math.Max( 0f, Math.Min( 1f, h ) );

		// Creusement de rivières (Worley)
		float river = WorleyNoise( nx * 0.55f, ny * 0.55f );
		if ( river < 0.13f && h > 0.04f )
		{
			float carve = (0.13f - river) / 0.13f;
			h = Math.Max( 0.02f, h - carve * carve * 0.4f );
		}

		return h * HeightMultiplier;
	}

	private static float Fbm( float x, float y, int octaves )
	{
		float total = 0f, amplitude = 1f, maxVal = 0f, frequency = 1f;
		for ( int i = 0; i < octaves; i++ )
		{
			total  += ValueNoise( x * frequency, y * frequency ) * amplitude;
			maxVal += amplitude;
			amplitude *= 0.5f;
			frequency *= 2f;
		}
		return total / maxVal;
	}

	private static float RidgedFbm( float x, float y, int octaves )
	{
		float total = 0f, amplitude = 1f, maxVal = 0f, frequency = 1f;
		for ( int i = 0; i < octaves; i++ )
		{
			float n = ValueNoise( x * frequency, y * frequency );
			n       = 1f - Math.Abs( n * 2f - 1f );
			total  += n * n * amplitude;
			maxVal += amplitude;
			amplitude *= 0.5f;
			frequency *= 2.1f;
		}
		return total / maxVal;
	}

	private static float WorleyNoise( float x, float y )
	{
		int   ix      = (int)Math.Floor( x ), iy = (int)Math.Floor( y );
		float minDist = float.MaxValue;

		for ( int dy = -2; dy <= 2; dy++ )
		{
			for ( int dx = -2; dx <= 2; dx++ )
			{
				int cx = ix + dx, cy = iy + dy;
				float px = cx + Rand2( cx * 3 + 1,  cy * 7 + 2  );
				float py = cy + Rand2( cx * 5 + 11, cy * 3 + 13 );
				float ddx = x - px, ddy = y - py;
				float d = (float)Math.Sqrt( ddx * ddx + ddy * ddy );
				if ( d < minDist ) minDist = d;
			}
		}
		return Math.Min( 1f, minDist );
	}

	private static float ValueNoise( float x, float y )
	{
		int   ix = (int)Math.Floor( x ), iy = (int)Math.Floor( y );
		float fx = x - ix,              fy = y - iy;
		float u  = fx * fx * (3f - 2f * fx);
		float v  = fy * fy * (3f - 2f * fy);
		return Lerp(
			Lerp( Rand2( ix,     iy     ), Rand2( ix + 1, iy     ), u ),
			Lerp( Rand2( ix,     iy + 1 ), Rand2( ix + 1, iy + 1 ), u ),
			v );
	}

	private static float Lerp( float a, float b, float t ) => a + (b - a) * t;

	private static float Rand2( int x, int y )
	{
		uint n = (uint)(x + y * 1619);
		n = (n << 13) ^ n;
		return ((n * (n * n * 60493u + 19990303u) + 1376312589u) & 0x7fffffffu) / (float)0x7fffffff;
	}
}
