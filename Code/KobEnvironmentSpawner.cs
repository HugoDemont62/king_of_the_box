using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class KobEnvironmentSpawner : Component
{
	[Property] public List<DecorationCategory> Categories       { get; set; } = new();
	[Property] public float                    MaxSlopeVariance { get; set; } = 0.003f;
	[Property] public float                    WallThickness    { get; set; } = 200f;
	[Property] public int   CityCount           { get; set; } = 3;
	[Property] public float CityRadius          { get; set; } = 2500f;
	[Property] public float CityFlatVarianceMax { get; set; } = 0.0005f;
	[Property] public Model RoadSegmentModel    { get; set; }
	[Property] public float RoadSegmentLength   { get; set; } = 400f;

	private readonly List<GameObject> _decorations = new();
	private List<Vector3> _cityCenters = new();
	private ushort[]                  _heights;
	private int                       _res;
	private Terrain                   _terrain;

	protected override void OnStart()
	{
		KobTerrainGenerator.OnTerrainReady += HandleTerrainReady;
	}

	protected override void OnDestroy()
	{
		KobTerrainGenerator.OnTerrainReady -= HandleTerrainReady;
	}

	private void HandleTerrainReady( int seed, ushort[] heights, int res, Terrain terrain )
	{
		_heights = heights;
		_res     = res;
		_terrain = terrain;
		CreateBoundaryWalls( terrain );
		SpawnDecorations( seed );
	}

	private void CreateBoundaryWalls( Terrain terrain )
	{
		foreach ( var child in GameObject.Children.ToList() )
		{
			if ( child.Name == "BoundaryWall" )
				child.Destroy();
		}

		float tX    = terrain.WorldPosition.x;
		float tY    = terrain.WorldPosition.y;
		float tZ    = terrain.WorldPosition.z;
		float size  = terrain.TerrainSize;
		float wallH = terrain.TerrainHeight * 2f;
		float half  = WallThickness * 0.5f;
		float mid   = size * 0.5f;
		float midZ  = tZ + wallH * 0.5f;

		(Vector3 pos, Vector3 boxSize)[] walls =
		{
			(new Vector3( tX - half,                  tY + mid,               midZ ), new Vector3( WallThickness,           size + WallThickness * 2f, wallH )),
			(new Vector3( tX + size + half,            tY + mid,               midZ ), new Vector3( WallThickness,           size + WallThickness * 2f, wallH )),
			(new Vector3( tX + mid,                   tY - half,              midZ ), new Vector3( size + WallThickness * 2f, WallThickness,           wallH )),
			(new Vector3( tX + mid,                   tY + size + half,       midZ ), new Vector3( size + WallThickness * 2f, WallThickness,           wallH )),
		};

		foreach ( var (pos, boxSize) in walls )
		{
			var go = new GameObject( true, "BoundaryWall" );
			go.Parent        = GameObject;
			go.WorldPosition = pos;

			var box  = go.Components.Create<BoxCollider>();
			box.Scale = boxSize;
		}

		Log.Info( "[KobEnv] 4 murs invisibles créés." );
	}

	private void SpawnDecorations( int seed )
	{
		foreach ( var go in _decorations )
			go.Destroy();
		_decorations.Clear();
		_cityCenters.Clear();

		if ( _terrain is null || _heights is null || Categories.Count == 0 ) return;

		float tX      = _terrain.WorldPosition.x;
		float tY      = _terrain.WorldPosition.y;
		float tZ      = _terrain.WorldPosition.z;
		float tSize   = _terrain.TerrainSize;
		float tHeight = _terrain.TerrainHeight;

		var cityRng = new System.Random( seed ^ 0x3C17_A000 );
		_cityCenters = FindCityCenters( cityRng, tX, tY, tZ, tSize, tHeight );
		Log.Info( $"[KobEnv] {_cityCenters.Count} centre(s) de ville trouvé(s)." );

		int categorySeed = seed ^ 0x7DEC_0001;

		foreach ( var category in Categories )
		{
			if ( category.Model is null ) continue;

			var rng    = new System.Random( categorySeed++ );
			int placed;

			if ( category.IsUrban && _cityCenters.Count > 0 )
				placed = SpawnUrbanCategory( category, rng, tX, tY, tSize, tHeight );
			else
				placed = SpawnNatureCategory( category, rng, tX, tY, tZ, tSize, tHeight );

			Log.Info( $"[KobEnv] {placed}/{category.Count} '{category.Model.ResourceName}' placé(s)." );
		}

		SpawnRoads( tHeight );
	}

	private void SpawnDecoObject( DecorationCategory category, System.Random rng, Vector3 position )
	{
		var go = new GameObject( true, $"Deco_{category.Model.ResourceName}" );
		go.Parent        = GameObject;
		go.WorldPosition = position;

		float s = category.MinScale + (float)rng.NextDouble() * (category.MaxScale - category.MinScale);
		go.WorldScale = Vector3.One * s;

		if ( category.RandomRotationY )
			go.WorldRotation = Rotation.FromYaw( (float)(rng.NextDouble() * 360.0) );

		go.Tags.Add( "deco" );

		var renderer  = go.Components.Create<ModelRenderer>();
		renderer.Model = category.Model;

		var collider  = go.Components.Create<ModelCollider>();
		collider.Model = category.Model;

		_decorations.Add( go );
	}

	private List<Vector3> FindCityCenters( System.Random rng, float tX, float tY, float tZ, float tSize, float tHeight )
	{
		var centers  = new List<Vector3>();
		int attempts = 0;

		while ( centers.Count < CityCount && attempts < CityCount * 80 )
		{
			attempts++;

			float u = 0.1f + (float)rng.NextDouble() * 0.8f;
			float v = 0.1f + (float)rng.NextDouble() * 0.8f;

			int   px = (int)(u * (_res - 1));
			int   py = (int)(v * (_res - 1));
			float h  = _heights[py * _res + px] / (float)ushort.MaxValue;

			if ( h < 0.06f || h > 0.55f ) continue;
			if ( KobTerrainUtils.GetVariance( _heights, _res, px, py, 20 ) > CityFlatVarianceMax ) continue;

			float wx  = tX + u * tSize;
			float wy  = tY + v * tSize;
			float wz  = tZ + h * tHeight + 500f;
			var   ray = new Ray( new Vector3( wx, wy, wz ), Vector3.Down );
			var   tr  = Scene.Trace.Ray( ray, tHeight + 1000f ).WithoutTags( "deco" ).Run();
			if ( !tr.Hit || tr.HitPosition.z < tZ ) continue;

			bool tooClose = false;
			foreach ( var c in centers )
			{
				if ( (tr.HitPosition - c).Length < CityRadius * 2.5f )
				{
					tooClose = true;
					break;
				}
			}
			if ( tooClose ) continue;

			centers.Add( tr.HitPosition );
		}

		return centers;
	}

	private int SpawnUrbanCategory( DecorationCategory category, System.Random rng, float tX, float tY, float tSize, float tHeight )
	{
		int total   = 0;
		int perCity = System.Math.Max( 1, category.Count / _cityCenters.Count );

		foreach ( var center in _cityCenters )
		{
			int placed = 0;
			int maxTry = perCity * 20;

			for ( int attempt = 0; attempt < maxTry && placed < perCity; attempt++ )
			{
				float angle  = (float)(rng.NextDouble() * System.Math.PI * 2.0);
				float radius = (float)(rng.NextDouble() * CityRadius);
				float wx     = center.x + System.MathF.Cos( angle ) * radius;
				float wy     = center.y + System.MathF.Sin( angle ) * radius;

				float u = (wx - tX) / tSize;
				float v = (wy - tY) / tSize;
				if ( u < 0f || u > 1f || v < 0f || v > 1f ) continue;

				int   px = (int)(u * (_res - 1));
				int   py = (int)(v * (_res - 1));
				float h  = _heights[py * _res + px] / (float)ushort.MaxValue;
				if ( h < 0.06f || h > 0.58f ) continue;
				if ( KobTerrainUtils.GetVariance( _heights, _res, px, py, 10 ) > MaxSlopeVariance ) continue;

				float wz  = _terrain.WorldPosition.z + h * tHeight + 500f;
				var   ray = new Ray( new Vector3( wx, wy, wz ), Vector3.Down );
				var   tr  = Scene.Trace.Ray( ray, tHeight + 1000f ).WithoutTags( "deco" ).Run();
				if ( !tr.Hit || tr.HitPosition.z < _terrain.WorldPosition.z ) continue;

				SpawnDecoObject( category, rng, tr.HitPosition );
				placed++;
			}

			total += placed;
		}

		return total;
	}

	private int SpawnNatureCategory( DecorationCategory category, System.Random rng, float tX, float tY, float tZ, float tSize, float tHeight )
	{
		int placed = 0;
		int maxTry = category.Count * 15;

		for ( int attempt = 0; attempt < maxTry && placed < category.Count; attempt++ )
		{
			float u = (float)rng.NextDouble();
			float v = (float)rng.NextDouble();

			int   px = (int)(u * (_res - 1));
			int   py = (int)(v * (_res - 1));
			float h  = _heights[py * _res + px] / (float)ushort.MaxValue;

			if ( h < 0.06f || h > 0.58f ) continue;
			if ( KobTerrainUtils.GetVariance( _heights, _res, px, py, 10 ) > MaxSlopeVariance ) continue;

			float wx  = tX + u * tSize;
			float wy  = tY + v * tSize;
			float wz  = tZ + h * tHeight + 500f;
			var   ray = new Ray( new Vector3( wx, wy, wz ), Vector3.Down );
			var   tr  = Scene.Trace.Ray( ray, tHeight + 1000f ).WithoutTags( "deco" ).Run();
			if ( !tr.Hit || tr.HitPosition.z < tZ ) continue;

			SpawnDecoObject( category, rng, tr.HitPosition );
			placed++;
		}

		return placed;
	}

	private void SpawnRoads( float tHeight )
	{
		if ( RoadSegmentModel is null || _cityCenters.Count < 2 ) return;

		for ( int i = 0; i < _cityCenters.Count - 1; i++ )
		{
			Vector3 from    = _cityCenters[i];
			Vector3 to      = _cityCenters[i + 1];
			Vector3 flatDir = (to - from).WithZ( 0f );
			float   distance = flatDir.Length;
			if ( distance < 1f ) continue;

			Vector3  dir = flatDir / distance;
			Rotation rot = Rotation.LookAt( from + dir, Vector3.Up );

			float t = 0f;
			while ( t < distance )
			{
				float frac = t / distance;
				float wx   = from.x + dir.x * t;
				float wy   = from.y + dir.y * t;
				float wz   = from.z + (to.z - from.z) * frac + 500f;

				var ray    = new Ray( new Vector3( wx, wy, wz ), Vector3.Down );
				var tr     = Scene.Trace.Ray( ray, tHeight + 1000f ).WithoutTags( "deco" ).Run();
				float minZ = _terrain.WorldPosition.z;

				if ( tr.Hit && tr.HitPosition.z >= minZ )
				{
					var go = new GameObject( true, "RoadSegment" );
					go.Tags.Add( "deco" );
					go.Parent        = GameObject;
					go.WorldPosition = tr.HitPosition;
					go.WorldRotation = rot;

					var renderer  = go.Components.Create<ModelRenderer>();
					renderer.Model = RoadSegmentModel;

					_decorations.Add( go );
				}

				t += RoadSegmentLength;
			}
		}

		Log.Info( $"[KobEnv] Routes tracées entre {_cityCenters.Count} villes." );
	}
}
