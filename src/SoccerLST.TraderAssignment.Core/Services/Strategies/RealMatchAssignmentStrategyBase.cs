using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies;

/// <summary>
/// Base class for real match assignment strategies (MJ, M1, M2, etc.)
/// Contains common helper methods for match-trader validation and time overlap checks
/// Implements IMatchAssignmentStrategy interface with abstract methods for derived classes
/// </summary>
public abstract class RealMatchAssignmentStrategyBase : IMatchAssignmentStrategy
{
    /// <summary>
    /// Gets the match type this strategy handles (e.g., "MJ", "M1", "M2")
    /// Must be implemented by derived classes
    /// </summary>
    public abstract string MatchType { get; }

    /// <summary>
    /// Assigns traders to matches based on the specific strategy rules
    /// Must be implemented by derived classes
    /// </summary>
    public abstract AssignmentResponse Assign(AssignmentRequest request);

    /// <summary>
    /// Validates that the matches are valid for this strategy
    /// Must be implemented by derived classes
    /// </summary>
    public abstract (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches);

    /// <summary>
    /// Virtual match types that require gap rules
    /// </summary>
    protected static readonly string[] VirtualMatchTypes = { "PES5MIN", "PES24", "PKIF", "PKCF" };

    /// <summary>
    /// Required gap in minutes after virtual matches
    /// </summary>
    protected const int VirtualMatchGapMinutes = 15;

    /// <summary>
    /// Check if match is within trader's shift time
    /// </summary>
    protected static bool IsMatchWithinShift(TraderShift trader, Match match)
    {
        return match.Start >= trader.Start && match.End <= trader.End;
    }

    /// <summary>
    /// Check if trader should ignore this match based on league preferences
    /// Returns true if the match league is in the trader's ignore list
    /// </summary>
    protected static bool IsLeagueIgnored(TraderShift trader, Match match)
    {
        // If trader has no ignore list, they can work any league
        if (trader.IgnoreLeague == null || !trader.IgnoreLeague.Any())
            return false;

        // If match has no league name, can't be ignored
        if (string.IsNullOrWhiteSpace(match.LeagueName))
            return false;

        // Check if match league contains any of the trader's ignore patterns (case-insensitive)
        return trader.IgnoreLeague.Any(ignoredLeague =>
            match.LeagueName.Contains(ignoredLeague, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if trader cannot be assigned to this EXPT match
    /// Returns true if the match is EXPT and the trader cannot handle EXPT matches
    /// </summary>
    protected static bool IsEXPTMatchRestricted(TraderShift trader, Match match)
    {
        // If match is EXPT and trader cannot handle EXPT matches, restrict assignment
        return match.IsEXPT && !trader.IsEXPTMatchAssignable;
    }

    /// <summary>
    /// Check if match overlaps with any of trader's assigned jobs
    /// </summary>
    protected bool HasAssignedJobConflict(TraderShift trader, Match match)
    {
        if (trader.AssignedJobs == null || !trader.AssignedJobs.Any())
            return false;

        foreach (var job in trader.AssignedJobs)
        {
            if (HasTimeOverlap(match, job))
                return true;

            if (ViolatesVirtualMatchGapRule(match, job))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if match time overlaps with assigned job time
    /// </summary>
    protected static bool HasTimeOverlap(Match match, AssignedJob job)
    {
        return match.Start < job.End && job.Start < match.End;
    }

    /// <summary>
    /// Check if match violates the 15-minute gap rule after virtual matches
    /// </summary>
    protected static bool ViolatesVirtualMatchGapRule(Match match, AssignedJob job)
    {
        // Requires 15-minute gap after PES/PK virtual matches
        if (!VirtualMatchTypes.Contains(job.Type))
            return false;

        if (job.End > match.Start)
            return false;

        var gap = match.Start - job.End;
        return gap.TotalMinutes < VirtualMatchGapMinutes;
    }

    /// <summary>
    /// Get trader priority multiplier based on level and manager status
    /// Base implementation supports Elite, Member, and First levels
    /// Can be overridden by derived classes to support additional levels (Second, Third, etc.)
    /// </summary>
    protected virtual double GetTraderPriority(TraderShift trader)
    {
        return trader.Level switch
        {
            "Elite" => trader.IsManager ? 1.8 : 2.0,
            "Member" => trader.IsManager ? 1.1 : 1.3,
            "First" => trader.IsManager ? 1.0 : 1.2,
            _ => 1.0
        };
    }
}
