namespace SoccerLST.TraderAssignment.Core.Models;

public class AssignmentResponse
{
    public bool Success { get; set; }

    public string Status { get; set; } = string.Empty;

    public int TotalMatches { get; set; }

    public int AssignedMatches { get; set; }

    public List<MatchAssignment> Assignments { get; set; } = new();

    public AssignmentSummary Summary { get; set; } = new();
}

public class AssignmentSummary
{
    public int PreconfiguredPairsUsed { get; set; }

    public int PrimaryPairs { get; set; }

    public int SecondaryPairs { get; set; }

    public int BackupPairs { get; set; }
}
