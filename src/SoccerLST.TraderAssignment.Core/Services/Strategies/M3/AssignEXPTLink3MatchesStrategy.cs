using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies.M3
{
    /// <summary>
    /// matches EXPT maxbet<=12000
    /// trader with 3 linked matches: can be 1 Expt League + 2 EAL League, or 2 Expt League + 1 EAL League.																									
    /// </summary>
    internal static class AssignEXPTLink3MatchesStrategy
    {
        private const int RequiredGroupSize = 3;


        public static void Assign(AssignmentRequest request, IList<MatchAssignment> assignments)
        {
            var group = GroupThreeMatches(request.Matches);
            Utils.ProcessAssignTraderToGroupMatches(request.TraderShifts, group, assignments);
        }

        private static IList<IList<Match>> GroupThreeMatches(IList<Match> events)
        {
            // Group 3 events by Start time
            var groupsByStartTime = events
                .Where(e => e is { IsAlreadyHandled: false })
                .GroupBy(e => e.Start)
                .ToList();

            var validGroups = new List<IList<Match>>();

            foreach (var matches in groupsByStartTime.Select(group => group.ToList()))
            {
                switch (matches.Count)
                {
                    case RequiredGroupSize:
                    {
                        // Check if the group satisfies condition
                        if (SatisfiesCondition(matches))
                        {
                            validGroups.Add(matches);
                        }

                        break;
                    }
                    case > RequiredGroupSize:
                    {
                        // Find all non-overlapping subsets of RequiredGroupSize items that satisfy condition
                        // Each match can only appear in one group
                        var nonOverlappingSubsets = FindNonOverlappingValidSubsets(matches, RequiredGroupSize);
                        validGroups.AddRange(nonOverlappingSubsets);
                        break;
                    }
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

            var matchesEXPT = matches.Count(m => m.IsEXPT);
            var matchesEAL = matches.Count(m => m.IsEAL);

            // Condition: 1 Expt League + 2 EAL League, or 2 Expt League + 1 EAL League.
            return (matchesEXPT==1 && matchesEAL==2) ||(matchesEXPT==2 && matchesEAL==1);
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
