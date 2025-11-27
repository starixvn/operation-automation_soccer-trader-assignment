using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies.M3
{
    /// <summary>
    /// assign trader to matches EAL has 10000 <= maxbet <=12000
    /// </summary>
    internal static class AssignEALLink4MatchesStrategy
    {
        private const int RequiredGroupSize = 4;
        private const int MaxBetThreshold10K = 10000;
        private const int MaxBetThreshold12K = 12000;


        public static void Assign(AssignmentRequest request, IList<MatchAssignment> assignments)
        {
            var group = GroupFourMatches(request.Matches);
            Utils.ProcessAssignTraderToGroupMatches(request.TraderShifts, group, assignments);
        }

        private static IList<IList<Match>> GroupFourMatches(IList<Match> events)
        {
            // Group EAL events by Start time
            var groupsByStartTime = events
                .Where(e => e is { IsEAL: true, IsAlreadyHandled: false })
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
        /// Checks if a group of matches satisfies condition:
        /// Max count matches have mb = 10K, or 12K is 2, two matches others must less than 10K
        /// </summary>
        private static bool SatisfiesCondition(List<Match> matches)
        {
            if (matches.Count != RequiredGroupSize)
            {
                return false;
            }

            var matchesWithMaxBetThreshold10K = matches.Count(m => m.MaxBet == MaxBetThreshold10K);
            var matchesWithMaxBetThreshold12K = matches.Count(m => m.MaxBet < MaxBetThreshold12K);
            var matchesWithMaxBetThresholdLessThan10K= matches.Count(m => m.MaxBet < MaxBetThreshold10K);

            // Condition: Max count matches hâve mb = 10K, or 12K is 2, two matches others must less than 10K
            return (matchesWithMaxBetThreshold10K==2 || matchesWithMaxBetThreshold12K==2) && matchesWithMaxBetThresholdLessThan10K == 2;
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
            validSubsets.AddRange(combinations.Where(SatisfiesCondition));

            return validSubsets;
        }

    }
}
