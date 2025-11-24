using Google.OrTools.Sat;
using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies;

/// <summary>
/// Assignment strategy for Minor 3 (M3) matches
/// Rules:
/// - MaxBet: 8000 - 20,000
/// - No skill validation required
/// </summary>
public class MinorThreeMatchAssignmentStrategy : RealMatchAssignmentStrategyBase
{
    public override string MatchType => "M3";

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        return (true, null);
    }

    public override AssignmentResponse Assign(AssignmentRequest request)
    {
        // TODO
        return new AssignmentResponse();
    }
}
