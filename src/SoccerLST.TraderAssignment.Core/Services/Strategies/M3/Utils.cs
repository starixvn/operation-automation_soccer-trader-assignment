using SoccerLST.TraderAssignment.Core.Models;

public static class Utils
{
    /// <summary>
    /// Generates all combinations of size 'count' from the list of matches
    /// </summary>
    public static IEnumerable<List<Match>> GenerateCombinations(List<Match> matches, int count)
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

    public static bool IsMatchWithinShift(TraderShift trader, Match match)
    {
        return match.Start >= trader.Start && match.End <= trader.End;
    }

    /// <summary>
    /// Check if trader should ignore this match based on league preferences
    /// Returns true if the match league is in the trader's ignore list
    /// </summary>
    public static bool IsLeagueIgnored(TraderShift trader, Match match)
    {
        // If trader has no ignore list, they can work any league
        if (trader.IgnoreLeague == null || !trader.IgnoreLeague.Any())
            return false;

        // If match has no league name, can't be ignored
        if (string.IsNullOrWhiteSpace(match.LeagueName))
            return false;

        // Check if match league contains any of the trader's ignore patterns (case-insensitive)
        return trader.IgnoreLeague.Any(ignoredLeague =>
            match.LeagueName.Contains(ignoredLeague, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if match time overlaps with assigned job time
    /// </summary>
    public static bool HasTimeOverlap(Match match, AssignedJob job)
    {
        return match.Start < job.End && job.Start < match.End;
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
                        assignments.Add(new MatchAssignment()
                        {
                            RowNumber = match.Id,
                            Hdp = shift.Name,
                            Ou = shift.Name
                        });

                        if (shift.AssignedJobs is null)
                        {
                            shift.AssignedJobs = new List<AssignedJob>();
                        }
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