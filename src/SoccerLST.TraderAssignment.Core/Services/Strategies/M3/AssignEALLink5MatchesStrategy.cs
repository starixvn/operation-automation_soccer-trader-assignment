using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies.M3
{
    /// <summary>
    /// assign trader to matches EAL has maxbet <=10000
    /// </summary>
    internal static class AssignEALLink5MatchesStrategy
    {
        private const int RequiredGroupSize = 5;
        private const int MaxBetThreshold = 10000;
        private const int AllowedMatchesWithMaxBetThreshold = 1;
        private const int RequiredMatchesBelowThreshold = 4;

        public static void Assign(AssignmentRequest request, IList<MatchAssignment> assignments)
        {
            var group = GroupFiveMatches(request.Matches);
            Utils.ProcessAssignTraderToGroupMatches(request.TraderShifts, group, assignments);
        }

        private static IList<IList<Match>> GroupFiveMatches(IList<Match> events)
        {
            // Group EAL events by Start time
            var groupsByStartTime = events
                .Where(e => e.IsEAL && !e.IsAlreadyHandled)
                .GroupBy(e => e.Start)
                .ToList();

            var validGroups = new List<IList<Match>>();

            foreach (var group in groupsByStartTime)
            {
                var matches = group.ToList();

                if (matches.Count == RequiredGroupSize)
                {
                    // Check if the group satisfies condition
                    if (SatisfiesCondition(matches))
                    {
                        validGroups.Add(matches);
                    }
                }
                else if (matches.Count > RequiredGroupSize)
                {
                    // Find all non-overlapping subsets of RequiredGroupSize items that satisfy condition
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
        private static bool SatisfiesCondition(List<Match> matches)
        {
            if (matches.Count != RequiredGroupSize)
            {
                return false;
            }

            var matchesWithMaxBetThreshold = matches.Count(m => m.MaxBet == MaxBetThreshold);
            var matchesWithMaxBetBelowThreshold = matches.Count(m => m.MaxBet < MaxBetThreshold);

            // Condition: Either all items have MaxBet < MaxBetThreshold, OR one match has MaxBet = MaxBetThreshold and others have MaxBet < MaxBetThreshold
            return matchesWithMaxBetBelowThreshold == RequiredGroupSize ||
                   matchesWithMaxBetThreshold == AllowedMatchesWithMaxBetThreshold && matchesWithMaxBetBelowThreshold == RequiredMatchesBelowThreshold;
        }


        /// <summary>
        /// Finds non-overlapping subsets of size 'count' from matches that satisfy condition (*)
        /// Each match can only appear in one group
        /// </summary>
        private static List<List<Match>> FindNonOverlappingValidSubsets(List<Match> matches, int count)
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
        /// Finds all subsets of size 'count' from matches that satisfy condition (*)
        /// </summary>
        private static List<List<Match>> FindAllValidSubsets(List<Match> matches, int count)
        {
            var validSubsets = new List<List<Match>>();

            if (matches.Count < count)
            {
                return validSubsets;
            }

            // Generate all combinations of size 'count'
            var combinations = Utils.GenerateCombinations(matches, count);

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

    }
}
