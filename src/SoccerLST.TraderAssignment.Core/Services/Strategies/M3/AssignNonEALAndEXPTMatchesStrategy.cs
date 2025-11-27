using SoccerLST.TraderAssignment.Core.Models;

/// <summary>
/// assign trader to matches is not EAL/EXPT
/// </summary>
public static class AssignNonEALAndEXPTMatchesStrategy
{
    public static void Assign(AssignmentRequest request, IList<MatchAssignment> assignments)
    {
        var group = GroupTwoMatches(request.Matches);
        ProcessAssignTraderToGroupMatches(request.TraderShifts,group, assignments);
    }

    private static IList<IList<Match>> GroupTwoMatches(IList<Match> events)
    {
        const int groupSize = 2;

        // Group EAL events by Start time
        var groupsByStartTime = events
            .Where(m => m is
            {
                IsAlreadyHandled: false, IsEAL: false, IsEXPT: false
            })
            .GroupBy(e => e.Start)
            .ToList();

        var validGroups = new List<IList<Match>>();

        foreach (var group in groupsByStartTime)
        {
            var matches = group.ToList();

            switch (matches.Count)
            {
                case groupSize:
                    // Exactly 2 matches - add as a group
                    validGroups.Add(matches);
                    break;
                case > groupSize:
                {
                    // More than 2 matches - find non-overlapping pairs
                    var nonOverlappingPairs = FindNonOverlappingPairs(matches);
                    validGroups.AddRange(nonOverlappingPairs);
                    break;
                }
            }
        }

        return validGroups;
    }

    /// <summary>
    /// Finds non-overlapping pairs of matches (each match can only appear in one pair)
    /// </summary>
    private static List<List<Match>> FindNonOverlappingPairs(List<Match> matches)
    {
        const int pairSize = 2;
        var nonOverlappingPairs = new List<List<Match>>();
        var usedMatchIds = new HashSet<string>();

        if (matches.Count < pairSize)
        {
            return nonOverlappingPairs;
        }

        // Generate all pairs
        var allPairs = GenerateCombinations(matches, pairSize);

        // Greedily select non-overlapping pairs
        foreach (var pair in allPairs)
        {
            var pairMatchIds = pair.Select(m => m.Id).ToHashSet();
            if (!pairMatchIds.Any(id => usedMatchIds.Contains(id)))
            {
                // No overlap, add this pair
                nonOverlappingPairs.Add(pair);
                // Mark matches as used
                foreach (var matchId in pairMatchIds)
                {
                    usedMatchIds.Add(matchId);
                }
            }
        }

        return nonOverlappingPairs;
    }

    /// <summary>
    /// Generates all combinations of size 'count' from the list of matches
    /// </summary>
    private static IEnumerable<List<Match>> GenerateCombinations(List<Match> matches, int count)
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

    public static void ProcessAssignTraderToGroupMatches(
        IList<TraderShift> traderShifts,
        IList<IList<Match>> groupMatches,
        IList<MatchAssignment> assignments)
    {
        var canAssign = true;
        foreach (var groupMatch in groupMatches)
        {
            foreach (var match in groupMatch)
            {
                foreach (var shift in traderShifts)
                {
                    if (!Utils.IsMatchWithinShift(shift, match))
                    {
                        canAssign = false;
                    }

                    if (canAssign && shift.AssignedJobs != null)
                    {
                        foreach (var assignedJob in shift.AssignedJobs)
                        {
                            if (Utils.HasTimeOverlap(match, assignedJob))
                            {
                                canAssign = false;
                            }
                        }
                    }

                    if (!shift.IsEXPTMatchAssignable)
                    {
                        canAssign = false;
                    }

                    if (canAssign && Utils.IsLeagueIgnored(shift, match))
                    {
                        canAssign = false;
                    }
                    if (canAssign)
                    {
                        assignments.Add(new MatchAssignment()
                        {
                            RowNumber = match.Id,
                            Hdp = shift.Name,
                            Ou = shift.Name
                        });

                        shift.AssignedJobs ??= [];
                        shift.AssignedJobs.Add(new AssignedJob()
                        {
                            Type = "M3",
                            Start = match.Start,
                            End = match.End
                        });
                        match.IsAlreadyHandled = true;
                        break;

                    }
                }
            }
        }
    }
}