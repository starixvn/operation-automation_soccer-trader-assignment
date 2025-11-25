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
        var categoryM3EalLink5 = CategoryM3EalLink5(request.Matches);
        ProcessAssignTrader(request.TraderShifts, categoryM3EalLink5, response.Assignments, "hdp");
        ProcessAssignTrader(request.TraderShifts, categoryM3EalLink5, response.Assignments, "ou");
        return response;
    }


    private void ProcessAssignTrader(
        List<TraderShift> traderShifts, 
        IList<List<Match>> groupMatches, 
        List<MatchAssignment> assignments,
        string bettype)
    {
        var canAssign = true;
        foreach (var groupMatch in groupMatches)
        {
            foreach (var match in groupMatch)
            {
                foreach (var shift in traderShifts)
                {
                    if (!IsMatchWithinShift(shift, match))
                    {
                        canAssign = false;
                    }

                    if (canAssign && shift.AssignedJobs != null)
                    {
                        foreach (var assignedJob in shift.AssignedJobs)
                        {
                            if (HasTimeOverlap(match, assignedJob))
                            {
                                canAssign = false;
                            }
                        }
                    }

                    if (canAssign && IsLeagueIgnored(shift, match))
                    {
                        canAssign = false;
                    }
                    if (canAssign)
                    {
                        var assignment = assignments.FirstOrDefault(x => x.RowNumber == match.Id);
                        assignment = assignment ?? new MatchAssignment()
                        {
                            RowNumber = match.Id,
                        };

                        if (bettype == "hdp")
                        {
                            assignment.Hdp = shift.Name;
                        }

                        if (bettype == "ou")
                        {
                            assignment.Ou = shift.Name;
                        }
                        assignments.Add(assignment);

                        shift.AssignedJobs?.Add(new AssignedJob()
                        {
                            Type = "M3",
                            Start = match.Start,
                            End = match.End
                        });
                        break;
                    }
                }

            }
        }

    }
    private List<List<Match>> CategoryM3EalLink5(IList<Match> events)
    {
        // Group EAL events by Start time
        var groupsByStartTime = events
            .Where(e => e.IsEAL)
            .GroupBy(e => e.Start)
            .ToList();

        var validGroups = new List<List<Match>>();

        foreach (var group in groupsByStartTime)
        {
            var matches = group.ToList();

            if (matches.Count == RequiredGroupSize)
            {
                // Check if the group satisfies condition (*)
                if (SatisfiesCondition(matches))
                {
                    validGroups.Add(matches);
                }
            }
            else if (matches.Count > RequiredGroupSize)
            {
                // Find all non-overlapping subsets of RequiredGroupSize items that satisfy condition (*)
                // Each match can only appear in one group
                var nonOverlappingSubsets = FindNonOverlappingValidSubsets(matches, RequiredGroupSize);
                validGroups.AddRange(nonOverlappingSubsets);
            }
        }

        return validGroups;
    }

    /// <summary>
    /// Checks if a group of matches satisfies condition (*):
    /// Either all items have MaxBet < MaxBetThreshold, OR one match has MaxBet = MaxBetThreshold and others have MaxBet < MaxBetThreshold
    /// </summary>
    private bool SatisfiesCondition(List<Match> matches)
    {
        if (matches.Count != RequiredGroupSize)
        {
            return false;
        }

        var matchesWithMaxBetThreshold = matches.Count(m => m.MaxBet == MaxBetThreshold);
        var matchesWithMaxBetBelowThreshold = matches.Count(m => m.MaxBet < MaxBetThreshold);

        // Condition: Either all items have MaxBet < MaxBetThreshold, OR one match has MaxBet = MaxBetThreshold and others have MaxBet < MaxBetThreshold
        return matchesWithMaxBetBelowThreshold == RequiredGroupSize ||
               (matchesWithMaxBetThreshold == AllowedMatchesWithMaxBetThreshold && matchesWithMaxBetBelowThreshold == RequiredMatchesBelowThreshold);
    }

    /// <summary>
    /// Finds all subsets of size 'count' from matches that satisfy condition (*)
    /// </summary>
    private List<List<Match>> FindAllValidSubsets(List<Match> matches, int count)
    {
        var validSubsets = new List<List<Match>>();

        if (matches.Count < count)
        {
            return validSubsets;
        }

        // Generate all combinations of size 'count'
        var combinations = GenerateCombinations(matches, count);

        // Find all combinations that satisfy the condition
        foreach (var combination in combinations)
        {
            if (SatisfiesCondition(combination))
            {
                validSubsets.Add(combination);
            }
        }

        return validSubsets;
    }

    /// <summary>
    /// Finds non-overlapping subsets of size 'count' from matches that satisfy condition (*)
    /// Each match can only appear in one group
    /// </summary>
    private List<List<Match>> FindNonOverlappingValidSubsets(List<Match> matches, int count)
    {
        var nonOverlappingSubsets = new List<List<Match>>();
        var usedMatchIds = new HashSet<string>();

        if (matches.Count < count)
        {
            return nonOverlappingSubsets;
        }

        // Find all valid subsets
        var allValidSubsets = FindAllValidSubsets(matches, count);

        // Greedily select non-overlapping subsets
        foreach (var subset in allValidSubsets)
        {
            // Check if this subset overlaps with any already selected subset
            var subsetMatchIds = subset.Select(m => m.Id).ToHashSet();
            if (!subsetMatchIds.Any(id => usedMatchIds.Contains(id)))
            {
                // No overlap, add this subset
                nonOverlappingSubsets.Add(subset);
                // Mark all matches in this subset as used
                foreach (var matchId in subsetMatchIds)
                {
                    usedMatchIds.Add(matchId);
                }
            }
        }

        return nonOverlappingSubsets;
    }

    /// <summary>
    /// Generates all combinations of size 'count' from the list of matches
    /// </summary>
    private IEnumerable<List<Match>> GenerateCombinations(List<Match> matches, int count)
    {
        var result = new List<List<Match>>();

        if (count == 0)
        {
            result.Add(new List<Match>());
            return result;
        }

        if (count > matches.Count)
            return result;

        for (int i = 0; i <= matches.Count - count; i++)
        {
            var first = matches[i];
            var remainingMatches = matches.Skip(i + 1).ToList();
            var subCombinations = GenerateCombinations(remainingMatches, count - 1);

            foreach (var subCombination in subCombinations)
            {
                var combination = new List<Match> { first };
                combination.AddRange(subCombination);
                result.Add(combination);
            }
        }

        return result;
    }
}
