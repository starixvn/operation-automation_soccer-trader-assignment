using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services;

/// <summary>
/// Interface for different match type assignment strategies
/// Each match type (MJ, M1, M2, etc.) can have its own assignment rules and constraints
/// </summary>
public interface IMatchAssignmentStrategy
{
    /// <summary>
    /// Assigns traders to matches based on the specific strategy rules
    /// </summary>
    /// <param name="request">Assignment request with matches, traders, and pairs</param>
    /// <returns>Assignment response with optimized assignments</returns>
    AssignmentResponse Assign(AssignmentRequest request);

    /// <summary>
    /// Gets the match type this strategy handles (e.g., "MJ", "M1", "M2")
    /// </summary>
    string MatchType { get; }

    /// <summary>
    /// Validates that the matches are valid for this strategy
    /// </summary>
    /// <param name="matches">Matches to validate</param>
    /// <returns>Validation result with error messages if any</returns>
    (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches);
}
