namespace SoccerLST.TraderAssignment.Core.Models;

public class TraderShift
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }

    public List<string> Skills { get; set; } = new();

    public string Level { get; set; } = string.Empty; // "First", "Member", "Elite"

    public bool IsManager { get; set; }

    public int M1Capacity { get; set; } = 1; // Number of M1 (Minor 1) matches trader can handle simultaneously

    public int M2Capacity { get; set; } = 1; // Number of M2 (Minor 2) matches trader can handle simultaneously

    public List<string>? IgnoreLeague { get; set; }

    public List<string>? NonSoloLeagues { get; set; }

    public List<AssignedJob>? AssignedJobs { get; set; }

    public bool IsEXPTMatchAssignable { get; set; } = true;

    public int MaxBet { get; set; } = 1000000;

    /// <summary>
    /// Priority skill for trader (e.g., "HDP", "OU", null)
    /// When assigned, this trader gets a 1.1x bonus multiplier for Major match assignments in their priority skill
    /// </summary>
    public string? PrioritySkill { get; set; }
}
