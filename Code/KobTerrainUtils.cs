public static class KobTerrainUtils
{
	public static float GetVariance( ushort[] heights, int res, int cx, int cy, int radius )
	{
		float sum = 0f, sumSq = 0f;
		int   n   = 0;
		for ( int dy = -radius; dy <= radius; dy++ )
		{
			for ( int dx = -radius; dx <= radius; dx++ )
			{
				int nx = cx + dx, ny = cy + dy;
				if ( nx < 0 || nx >= res || ny < 0 || ny >= res ) continue;
				float h = heights[ny * res + nx] / (float)ushort.MaxValue;
				sum   += h;
				sumSq += h * h;
				n++;
			}
		}
		if ( n == 0 ) return 1f;
		float mean = sum / n;
		return sumSq / n - mean * mean;
	}
}
