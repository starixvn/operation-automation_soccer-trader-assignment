using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Tests.Utils;

/// <summary>
/// Common test helper methods for creating test data
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a TraderShift for testing purposes
    /// </summary>
    public static TraderShift CreateTraderShift(
        string id,
        string name,
        string[] skills,
        string level,
        bool isManager,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        int m1Capacity = 1,
        int m2Capacity = 1,
        List<AssignedJob>? assignedJobs = null,
        List<string>? ignoreLeague = null,
        bool isEXPTMatchAssignable = true)
    {
        return new TraderShift
        {
            Id = id,
            Name = name,
            Start = start ?? DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            End = end ?? DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
            Skills = skills.ToList(),
            Level = level,
            IsManager = isManager,
            M1Capacity = m1Capacity,
            M2Capacity = m2Capacity,
            AssignedJobs = assignedJobs ?? new List<AssignedJob>(),
            IgnoreLeague = ignoreLeague,
            IsEXPTMatchAssignable = isEXPTMatchAssignable
        };
    }

    /// <summary>
    /// Creates a Match for testing purposes
    /// </summary>
    public static Match CreateMatch(
        string id,
        string matchNo,
        DateTimeOffset start,
        DateTimeOffset end,
        int maxBet,
        string? leagueCode = null,
        bool isAlreadyHandled = false,
        string? leagueName = null,
        bool isEXPT = false,
        bool isEAL = false)
    {
        return new Match
        {
            Id = id,
            MatchNo = matchNo,
            Start = start,
            End = end,
            MaxBet = maxBet,
            LeagueCode = leagueCode,
            IsAlreadyHandled = isAlreadyHandled,
            LeagueName = leagueName,
            IsEXPT = isEXPT,
            IsEAL = isEAL
        };
    }

    /// <summary>
    /// Creates an AssignedJob for testing purposes
    /// </summary>
    public static AssignedJob CreateAssignedJob(
        string type,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return new AssignedJob
        {
            Type = type,
            Start = start,
            End = end
        };
    }

    /// <summary>
    /// Creates a PreconfiguredPair for testing purposes
    /// </summary>
    public static PreconfiguredPair CreatePreconfiguredPair(
        string hdp,
        string ou,
        string tier,
        Dictionary<int, List<string>>? leaguePriority = null)
    {
        return new PreconfiguredPair
        {
            Hdp = hdp,
            Ou = ou,
            Tier = tier,
            LeaguePriority = leaguePriority
        };
    }
}
