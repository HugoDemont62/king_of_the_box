using Sandbox;

public class DecorationCategory
{
	[Property] public Model  Model           { get; set; }
	[Property] public int    Count           { get; set; } = 50;
	[Property] public float  MinScale        { get; set; } = 0.8f;
	[Property] public float  MaxScale        { get; set; } = 1.2f;
	[Property] public bool   RandomRotationY { get; set; } = true;
	[Property] public bool   IsUrban         { get; set; } = false;
}
