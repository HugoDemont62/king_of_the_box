using System.Text.Json.Serialization;

public class KobAchievementStats
{
	[JsonInclude] public int   TotalKills      { get; set; }
	[JsonInclude] public int   TotalDeaths     { get; set; }
	[JsonInclude] public int   TotalZonePoints { get; set; }
	[JsonInclude] public float TotalTimePlayed { get; set; }
	[JsonInclude] public bool  WelcomeDone     { get; set; }
}
