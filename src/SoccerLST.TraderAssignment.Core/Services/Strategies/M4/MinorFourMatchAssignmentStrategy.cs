using Google.OrTools.Sat;
using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies.M4;

/// <summary>
/// Assignment strategy for Minor 4 (M4) matches
/// Rules:
/// - MaxBet: 1500 - 8000
/// - No skill validation required
/// </summary>
public class MinorFourMatchAssignmentStrategy : RealMatchAssignmentStrategyBase
{
    public override string MatchType => "M4";

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
