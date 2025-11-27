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
    public override string MatchType => "M3";

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        return (true, null);
    }

    public override AssignmentResponse Assign(AssignmentRequest request)
    {
        var response = new AssignmentResponse();
        //assign trader to matches is not EAL/EXPT
        AssignNonEALAndEXPTMatchesStrategy.Assign(request,response.Assignments);

        //assign trader to matches EAL has maxbet <=10000
        AssignEALLink5MatchesStrategy.Assign(request,response.Assignments);

        //assign trader to matches EAL has 10000 <= maxbet <= 12000
        AssignEALLink4MatchesStrategy.Assign(request,response.Assignments);
        return response;
    }
}
