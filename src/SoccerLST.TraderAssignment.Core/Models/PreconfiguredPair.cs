namespace SoccerLST.TraderAssignment.Core.Models;

public class PreconfiguredPair
{
    public string Hdp { get; set; } = string.Empty;

    public string Ou { get; set; } = string.Empty;

    public string Tier { get; set; } = "primary"; // "primary", "secondary", "backup"

    public Dictionary<int, List<string>>? LeaguePriority { get; set; }
}
