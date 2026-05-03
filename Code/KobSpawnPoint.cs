using Sandbox;

public sealed class KobSpawnPoint : Component
{
    [Property] public KobTeam Team { get; set; } = KobTeam.None;
}
