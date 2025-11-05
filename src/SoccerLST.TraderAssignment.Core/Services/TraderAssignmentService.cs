using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services;

/// <summary>
/// Orchestrates trader assignment operations by delegating to match-type-specific strategies
/// </summary>
public class TraderAssignmentService
{
    private readonly IMatchAssignmentStrategy _majorStrategy;
    private readonly IMatchAssignmentStrategy _minorOneStrategy;
    private readonly IMatchAssignmentStrategy _minorTwoStrategy;

    /// <summary>
    /// Initializes the service with injected strategies
    /// </summary>
    /// <param name="strategies">Collection of all registered assignment strategies</param>
    public TraderAssignmentService(IEnumerable<IMatchAssignmentStrategy> strategies)
    {
        var strategyList = strategies.ToList();

        _majorStrategy = strategyList.FirstOrDefault(s => s.MatchType == "MJ")
            ?? throw new InvalidOperationException("Major match strategy (MJ) not registered");

        _minorOneStrategy = strategyList.FirstOrDefault(s => s.MatchType == "M1")
            ?? throw new InvalidOperationException("Minor 1 match strategy (M1) not registered");

        _minorTwoStrategy = strategyList.FirstOrDefault(s => s.MatchType == "M2")
            ?? throw new InvalidOperationException("Minor 2 match strategy (M2) not registered");
    }

    /// <summary>
    /// Assigns traders to Major (MJ) matches (MaxBet >= 70,000)
    /// </summary>
    public AssignmentResponse MajorMatchAssignments(AssignmentRequest request)
    {
        return _majorStrategy.Assign(request);
    }

    /// <summary>
    /// Assigns traders to Minor 1 (M1) matches (MaxBet 40,000-70,000)
    /// </summary>
    public AssignmentResponse MinorOneMatchAssignments(AssignmentRequest request)
    {
        return _minorOneStrategy.Assign(request);
    }

    /// <summary>
    /// Assigns traders to Minor 2 (M2) matches (MaxBet 20,000-40,000)
    /// </summary>
    public AssignmentResponse MinorTwoMatchAssignments(AssignmentRequest request)
    {
        return _minorTwoStrategy.Assign(request);
    }
}
