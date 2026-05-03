using Sandbox;

public sealed class KobTeamSelector : Component
{
    [Sync] public bool TeamSelected { get; private set; }

    protected override void OnStart()
    {
        if ( IsProxy ) return;
        ShowTeamSelection();
    }

    protected override void OnUpdate()
    {
        if ( IsProxy || TeamSelected ) return;

        if ( Input.Pressed( "slot1" ) ) ChooseTeam( KobTeam.Red );
        if ( Input.Pressed( "slot2" ) ) ChooseTeam( KobTeam.Blue );
        if ( Input.Pressed( "slot3" ) ) ChooseTeam( KobTeam.Green );
    }

    void ShowTeamSelection()
    {
        var panel = Scene.GetAllComponents<KobTeamPanel>().FirstOrDefault();
        if ( panel is not null )
            panel.Enabled = true;
    }

    public void ChooseTeam( KobTeam team )
    {
        var panel = Scene.GetAllComponents<KobTeamPanel>().FirstOrDefault();
        if ( panel is not null )
            panel.Enabled = false;

        TeamSelected = true;

        var player = Scene.GetAllComponents<KobPlayer>().FirstOrDefault( p => !p.IsProxy );
        if ( player is not null )
            player.Team = team;

        SpawnPlayer( team );
    }

    void SpawnPlayer( KobTeam team )
    {
        var spawnPoints = Scene.GetAllComponents<KobSpawnPoint>()
            .Where( s => s.Team == team )
            .ToList();

        if ( spawnPoints.Count == 0 ) return;

        // Choisit un spawn aléatoire de l'équipe
        var spawn = Game.Random.FromList( spawnPoints );
        var player = Scene.GetAllComponents<KobPlayer>().FirstOrDefault( p => !p.IsProxy );
        if ( player is not null )
            player.WorldPosition = spawn.WorldPosition;
    }
}
