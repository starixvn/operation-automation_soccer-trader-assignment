using System.Globalization;
using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies.M3;

/// <summary>
/// Assignment strategy for Minor 3 (M3) matches
/// Rules:
/// - MaxBet: 8000 - 20,000
/// - No skill validation required
/// </summary>
public class MinorThreeMatchAssignmentStrategy : RealMatchAssignmentStrategyBase
{
    private const int RequiredGroupSize = 5;
    private const int MaxBetThreshold = 10000;
    private const int AllowedMatchesWithMaxBetThreshold = 1;
    private const int RequiredMatchesBelowThreshold = 4;

    public override string MatchType => "M3";

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        return (true, null);
    }

    public override AssignmentResponse Assign(AssignmentRequest request)
    {
        var response = new AssignmentResponse();
        AssignNonEALAndEXPTMatchesStrategy.Assign(request,response.Assignments);
        AssignEALLink5MatchesStrategy.Assign(request,response.Assignments);
        return response;
    }
}
