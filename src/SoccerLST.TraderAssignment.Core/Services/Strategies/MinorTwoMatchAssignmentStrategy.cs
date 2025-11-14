using Google.OrTools.Sat;
using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies;

/// <summary>
/// Assignment strategy for Minor 2 (M2) matches
/// Rules:
/// - MaxBet: 20,000 - 40,000
/// - One trader handles both HDP and OU
/// - Respects M2 capacity per trader (m2Capacity property)
/// - Considers ALL MJ and M1 matches as busy (higher priority)
/// - Considers M2 unchanged matches as busy
/// - 15-minute gap after PES/PK virtual matches (same as MJ)
/// - No skill validation required
/// </summary>
public class MinorTwoMatchAssignmentStrategy : RealMatchAssignmentStrategyBase
{
    public override string MatchType => "M2";

    private static readonly Dictionary<string, int> LevelPoints = new()
    {
        { "Third", 1 },
        { "Second", 2 },
        { "First", 3 },
        { "Member", 4 },
        { "Elite", 5 }
    };

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        // If all matches have BetType explicitly set to "M2", validation passes
        if (matches.All(m => m.BetType == "M2"))
        {
            return (true, null);
        }

        // Otherwise, validate using MaxBet ranges as fallback
        var invalidMatches = matches.Where(m => m.BetType != "M2" && (m.MaxBet < 20000 || m.MaxBet >= 40000)).ToList();
        if (invalidMatches.Any())
        {
            return (false, $"Found {invalidMatches.Count} matches outside M2 range (20K-40K) and BetType != 'M2'. All M2 matches must have BetType='M2' or MaxBet between 20,000 and 40,000.");
        }
        return (true, null);
    }

    public override AssignmentResponse Assign(AssignmentRequest request)
    {
        var traderShifts = request.TraderShifts;
        var matches = request.Matches.Where(m => !m.IsAlreadyHandled).ToList();

        // Build CP-SAT model
        var model = new CpModel();

        // Create decision variables: x[traderId, matchId]
        // M2: One trader handles both HDP and OU, so no role dimension needed
        var x = CreateDecisionVariables(model, traderShifts, matches);

        // Apply constraints
        ApplyConstraints(model, x, traderShifts, matches);

        // Build objective function
        var objectiveTerms = BuildObjective(model, x, traderShifts, matches);
        model.Maximize(LinearExpr.Sum(objectiveTerms));

        // Solve
        var solver = new CpSolver();
        solver.StringParameters = "max_time_in_seconds:105.0"; // 1 minute 45 seconds timeout
        var status = solver.Solve(model);

        // Build response
        return BuildResponse(status, solver, x, traderShifts, matches);
    }

    private Dictionary<(string traderId, string matchId), IntVar> CreateDecisionVariables(
        CpModel model,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        var x = new Dictionary<(string traderId, string matchId), IntVar>();

        foreach (var trader in traderShifts)
        {
            foreach (var match in matches)
            {
                // Only create variable if assignment is valid
                if (CanTraderBeAssigned(trader, match))
                {
                    var key = (trader.Id, match.Id);
                    x[key] = model.NewBoolVar($"x_{trader.Id}_{match.Id}");
                }
            }
        }

        return x;
    }

    private bool CanTraderBeAssigned(TraderShift trader, Match match)
    {
        // Check if match is within shift time
        if (!IsMatchWithinShift(trader, match))
            return false;

        // Check if trader ignores this league
        if (IsLeagueIgnored(trader, match))
            return false;

        // Check if trader cannot handle EXPT matches
        if (IsEXPTMatchRestricted(trader, match))
            return false;

        // M2: No skill validation needed

        // Check for conflicts with existing assignments
        if (HasAssignedJobConflict(trader, match))
            return false;

        // Check if trader's MaxBet is sufficient for this match
        if (trader.MaxBet < match.MaxBet)
            return false;

        return true;
    }

    // Override base implementation to handle M2/M3/M4 capacity logic specific to M2
    private new bool HasAssignedJobConflict(TraderShift trader, Match match)
    {
        if (trader.AssignedJobs == null || !trader.AssignedJobs.Any())
            return false;

        // Count M2/M3/M4 jobs with same start time as match (they can run concurrently if M2Capacity allows)
        var concurrentCapacityJobs = trader.AssignedJobs
            .Where(j => (j.Type == "M2" || j.Type == "M3" || j.Type == "M4") && j.Start == match.Start)
            .Count();

        // If we have M2/M3/M4 jobs at same start time, check if we have capacity
        // M2Capacity includes both M2 matches and M2/M3/M4 jobs running concurrently
        if (concurrentCapacityJobs > 0)
        {
            // Need at least (concurrentCapacityJobs + 1) capacity to add this M2 match
            if (trader.M2Capacity < concurrentCapacityJobs + 1)
                return true; // Not enough capacity
        }

        foreach (var job in trader.AssignedJobs)
        {
            // M2/M3/M4 jobs with same start time don't cause overlap conflict (capacity handles it)
            if ((job.Type == "M2" || job.Type == "M3" || job.Type == "M4") && job.Start == match.Start)
                continue;

            if (HasTimeOverlap(match, job))
                return true;

            if (ViolatesVirtualMatchGapRule(match, job))
                return true;
        }

        return false;
    }

    private void ApplyConstraints(
        CpModel model,
        Dictionary<(string traderId, string matchId), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        // Constraint 1: Each match assigned to at most 1 trader
        // Uses <= 1 to allow partial assignments when not enough traders
        foreach (var match in matches)
        {
            var traderAssignments = traderShifts
                .Where(t => x.ContainsKey((t.Id, match.Id)))
                .Select(t => x[(t.Id, match.Id)])
                .ToArray();

            if (traderAssignments.Length > 0)
            {
                model.Add(LinearExpr.Sum(traderAssignments) <= 1);
            }
        }

        // Constraint 2: Respect M2Capacity - trader can handle up to M2Capacity CONCURRENT matches
        // IMPORTANT: M2Capacity only applies to matches with the SAME start time
        // Overlapping matches with DIFFERENT start times are NOT allowed, regardless of capacity
        foreach (var trader in traderShifts)
        {
            // Group matches by start time (concurrent matches)
            var concurrentGroups = matches
                .GroupBy(m => m.Start)
                .Where(g => g.Count() > 1)  // Only groups with 2+ matches need capacity constraint
                .ToList();

            // Apply M2Capacity constraint to each concurrent group (same start time)
            foreach (var group in concurrentGroups)
            {
                var groupMatches = group.ToList();

                // Count existing M2/M3/M4 jobs at this exact start time
                int existingJobsCount = 0;
                if (trader.AssignedJobs != null)
                {
                    existingJobsCount = trader.AssignedJobs.Count(job =>
                        (job.Type == "M2" || job.Type == "M3" || job.Type == "M4") &&
                        job.Start == group.Key);
                }

                // Trader can be assigned to at most (M2Capacity - existingJobsCount) matches in this concurrent group
                var assignmentsInGroup = groupMatches
                    .Where(m => x.ContainsKey((trader.Id, m.Id)))
                    .Select(m => x[(trader.Id, m.Id)])
                    .ToArray();

                if (assignmentsInGroup.Length > 0)
                {
                    var availableCapacity = Math.Max(0, trader.M2Capacity - existingJobsCount);
                    model.Add(LinearExpr.Sum(assignmentsInGroup) <= availableCapacity);
                }
            }

            // Constraint 3: Prevent assignment to overlapping matches with DIFFERENT start times
            // These are NOT allowed even if trader has M2Capacity > 1
            for (int i = 0; i < matches.Count; i++)
            {
                for (int j = i + 1; j < matches.Count; j++)
                {
                    var match1 = matches[i];
                    var match2 = matches[j];

                    // Skip if same start time (already handled by capacity constraint above)
                    if (match1.Start == match2.Start)
                        continue;

                    // Check if matches overlap (different start times but overlapping duration)
                    bool overlaps = match1.Start < match2.End && match2.Start < match1.End;

                    if (overlaps)
                    {
                        // Trader cannot be assigned to both overlapping matches with different start times
                        // Only add constraint if both assignments are possible
                        if (x.ContainsKey((trader.Id, match1.Id)) && x.ContainsKey((trader.Id, match2.Id)))
                        {
                            model.Add(x[(trader.Id, match1.Id)] + x[(trader.Id, match2.Id)] <= 1);
                        }
                    }
                }
            }
        }
    }

    private List<LinearExpr> BuildObjective(
        CpModel model,
        Dictionary<(string traderId, string matchId), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        var objectiveTerms = new List<LinearExpr>();

        // Base bonus to prioritize coverage - any assignment is better than no assignment
        const long coverageBonus = 1_000_000;
        const long consecutiveMatchBonus = 50_000; // Bonus for consecutive matches (0-240 min gap)

        // 1. Individual match assignment scores
        foreach (var match in matches)
        {
            // Pre-filter eligible traders for this match to avoid KeyNotFoundException
            var eligibleTraders = traderShifts
                .Where(t => x.ContainsKey((t.Id, match.Id)))
                .ToList();

            foreach (var trader in eligibleTraders)
            {
                var traderPoints = LevelPoints.GetValueOrDefault(trader.Level, 1);
                var priorityLeagueBonus = GetPriorityLeagueBonus(trader, match);

                var traderScore = traderPoints * priorityLeagueBonus;
                var matchWeight = Math.Pow(match.MaxBet / 1000.0, 2);
                var finalScore = coverageBonus + (traderScore * matchWeight);

                var assignment = x[(trader.Id, match.Id)];
                objectiveTerms.Add(LinearExpr.Term(assignment, (long)Math.Round(finalScore)));
            }
        }

        // 2. Consecutive bonus: ONLY assignedJob-to-match (no match-to-match)
        // Removed match-to-match bonus to eliminate multiplication constraints for performance
        // Only consider gaps between existing assigned jobs and new matches
        // This requires NO multiplication since jobs are already assigned
        foreach (var trader in traderShifts)
        {
            // Skip if trader has no assigned jobs
            if (trader.AssignedJobs == null || trader.AssignedJobs.Count == 0)
                continue;

            // Pre-filter matches that are within this trader's shift
            var traderMatches = matches.Where(m => IsMatchWithinShift(trader, m)).ToList();
            if (traderMatches.Count == 0)
                continue;

            // Bonus for assignedJob-to-match assignments only
            foreach (var job in trader.AssignedJobs)
            {
                foreach (var match in traderMatches)
                {
                    // Check both directions:
                    // 1. Job → Match: match.Start == job.End (match starts right after job ends)
                    // 2. Match → Job: match.End == job.Start (match ends right before job starts)
                    bool isConsecutive = (match.Start == job.End) || (match.End == job.Start);

                    if (isConsecutive)
                    {
                        // If trader is assigned to this match, add bonus
                        // No multiplication needed - job is already assigned
                        if (x.ContainsKey((trader.Id, match.Id)))
                        {
                            objectiveTerms.Add(LinearExpr.Term(x[(trader.Id, match.Id)], consecutiveMatchBonus));
                        }
                    }
                }
            }
        }

        // 3. Shift boundary bonus: Match aligns perfectly with shift boundaries
        // Encourages efficient use of trader shifts
        const long shiftBoundaryBonus = 30_000; // Bonus for matches that align with shift start/end

        foreach (var trader in traderShifts)
        {
            // Pre-filter matches that are within this trader's shift
            var traderMatches = matches.Where(m => IsMatchWithinShift(trader, m)).ToList();
            if (traderMatches.Count == 0)
                continue;

            foreach (var match in traderMatches)
            {
                // Check if match aligns with shift boundaries:
                // 1. Match starts exactly when shift starts
                // 2. Match ends exactly when shift ends
                bool alignsWithShiftStart = (match.Start == trader.Start);
                bool alignsWithShiftEnd = (match.End == trader.End);

                if (alignsWithShiftStart || alignsWithShiftEnd)
                {
                    // Add bonus for perfect shift boundary alignment
                    if (x.ContainsKey((trader.Id, match.Id)))
                    {
                        objectiveTerms.Add(LinearExpr.Term(x[(trader.Id, match.Id)], shiftBoundaryBonus));
                    }
                }
            }
        }

        // 4. Add workload balancing penalty for concurrent matches
        // This encourages even distribution when multiple traders have capacity
        AddWorkloadBalancingPenalty(model, x, traderShifts, matches, objectiveTerms);

        return objectiveTerms;
    }


    /// <summary>
    /// Get priority league bonus for trader
    /// Returns 1.05x multiplier if match's league is in trader's PriorityLeagues list
    /// Otherwise returns 1.0 (no bonus)
    /// Uses case-insensitive substring matching (similar to league include logic)
    /// </summary>
    private static double GetPriorityLeagueBonus(TraderShift trader, Match match)
    {
        if (trader.PriorityLeagues == null || trader.PriorityLeagues.Count == 0)
            return 1.0;

        if (string.IsNullOrEmpty(match.LeagueName))
            return 1.0;

        var matchLeague = match.LeagueName.ToUpperInvariant().Trim();

        foreach (var priorityLeague in trader.PriorityLeagues)
        {
            var normalizedPriority = priorityLeague.ToUpperInvariant().Trim();

            // Check if match league contains priority league or vice versa
            if (matchLeague.Contains(normalizedPriority) || normalizedPriority.Contains(matchLeague))
            {
                return 1.05; // 1.05x bonus for priority league match
            }
        }

        return 1.0;
    }

    /// <summary>
    /// Adds workload balancing penalty to encourage even distribution of concurrent matches.
    /// For matches with the same start time, penalizes traders who exceed their fair share.
    /// </summary>
    private void AddWorkloadBalancingPenalty(
        CpModel model,
        Dictionary<(string traderId, string matchId), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches,
        List<LinearExpr> objectiveTerms)
    {
        // Group matches by start time (concurrent matches)
        var concurrentGroups = matches
            .GroupBy(m => m.Start)
            .Where(g => g.Count() > 1)  // Only care about groups with 2+ matches
            .ToList();

        foreach (var group in concurrentGroups)
        {
            var groupMatches = group.ToList();
            var avgMatchWeight = groupMatches.Average(m => Math.Pow(m.MaxBet / 1000.0, 2));

            // Count traders with capacity to handle ALL matches in this concurrent group
            // This is used for the special case bonus when only one trader can handle everything
            var tradersWithFullCapacity = traderShifts.Where(t =>
            {
                // Check if trader is available for these matches
                if (!IsMatchWithinShift(t, groupMatches[0]))
                    return false;

                // Count existing M2/M3/M4 jobs at this start time
                int existingJobs = 0;
                if (t.AssignedJobs != null)
                {
                    existingJobs = t.AssignedJobs.Count(job =>
                        (job.Type == "M2" || job.Type == "M3" || job.Type == "M4") &&
                        job.Start == groupMatches[0].Start);
                }

                // Check if trader has capacity for all matches in group
                return t.M2Capacity >= (groupMatches.Count + existingJobs);
            }).ToList();

            // Special case: If only one trader has sufficient capacity for all matches in group,
            // strongly prefer that trader over partial assignments
            if (tradersWithFullCapacity.Count == 1)
            {
                var singleTrader = tradersWithFullCapacity[0];

                // Give large bonus for this trader handling all matches in group
                // This bonus should outweigh quality differences
                var bonusPerMatch = (long)(avgMatchWeight * 500000);

                foreach (var match in groupMatches)
                {
                    if (x.ContainsKey((singleTrader.Id, match.Id)))
                    {
                        var assignment = x[(singleTrader.Id, match.Id)];
                        objectiveTerms.Add(LinearExpr.Term(assignment, bonusPerMatch));
                    }
                }

                continue;
            }

            // Count traders with ANY available capacity for workload balancing penalty
            // (different from tradersWithFullCapacity - includes traders who can't handle all matches)
            var tradersWithCapacity = traderShifts.Where(t =>
            {
                // Check if trader is available for these matches
                if (!IsMatchWithinShift(t, groupMatches[0]))
                    return false;

                // Count existing M2/M3/M4 jobs at this start time
                int existingJobs = 0;
                if (t.AssignedJobs != null)
                {
                    existingJobs = t.AssignedJobs.Count(job =>
                        (job.Type == "M2" || job.Type == "M3" || job.Type == "M4") &&
                        job.Start == groupMatches[0].Start);
                }

                // Check if trader has any capacity available (not already at M2Capacity limit)
                return t.M2Capacity > existingJobs;
            }).ToList();

            // If no traders have any capacity, skip balancing
            if (tradersWithCapacity.Count == 0)
                continue;

            // Penalize traders who get more than 1 match in this time group
            // This encourages distribution across multiple traders
            foreach (var trader in tradersWithCapacity)
            {
                // Count assignments in this concurrent group for this trader
                var assignmentsInGroup = groupMatches
                    .Where(m => x.ContainsKey((trader.Id, m.Id)))
                    .Select(m => x[(trader.Id, m.Id)])
                    .ToArray();

                if (assignmentsInGroup.Length == 0)
                    continue;

                var groupWorkload = model.NewIntVar(0, groupMatches.Count,
                    $"workload_group_{group.Key.Ticks}_{trader.Id}");
                model.Add(groupWorkload == LinearExpr.Sum(assignmentsInGroup));

                // Calculate excess over 1 match
                var excess = model.NewIntVar(0, groupMatches.Count,
                    $"excess_group_{group.Key.Ticks}_{trader.Id}");
                model.Add(excess >= groupWorkload - 1);
                model.Add(excess >= 0);

                // Penalize 900,000 points for each match beyond the first
                objectiveTerms.Add(LinearExpr.Term(excess, -900_000));
            }
        }
    }


    private AssignmentResponse BuildResponse(
        CpSolverStatus status,
        CpSolver solver,
        Dictionary<(string traderId, string matchId), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        var response = new AssignmentResponse
        {
            Success = status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible,
            Status = status.ToString(),
            TotalMatches = matches.Count,
            Assignments = new List<MatchAssignment>()
        };

        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            foreach (var match in matches.OrderByDescending(m => m.MaxBet))
            {
                var assignedTrader = traderShifts.FirstOrDefault(t =>
                    x.ContainsKey((t.Id, match.Id)) && solver.Value(x[(t.Id, match.Id)]) == 1);

                var assignment = new MatchAssignment
                {
                    RowNumber = match.Id,
                    Match = match.MatchNo,
                    League = match.LeagueCode ?? "N/A",
                    MaxBet = match.MaxBet,
                    Time = $"{match.Start:MM-dd HH:mm} - {match.End:HH:mm}"
                };

                if (assignedTrader == null)
                {
                    assignment.Hdp = "N/A";
                    assignment.HdpLevel = "N/A";
                    assignment.Ou = "N/A";
                    assignment.OuLevel = "N/A";
                    assignment.PairType = "❌ UNASSIGNED";
                    assignment.Score = 0;
                }
                else
                {
                    // M2: Same trader handles both HDP and OU
                    assignment.Hdp = assignedTrader.Name;
                    assignment.HdpLevel = assignedTrader.Level;
                    assignment.Ou = assignedTrader.Name;
                    assignment.OuLevel = assignedTrader.Level;
                    assignment.PairType = "Single Trader (M2)";

                    var traderPoints = LevelPoints.GetValueOrDefault(assignedTrader.Level, 1);
                    var matchWeight = (match.MaxBet / 1000.0) * (match.MaxBet / 1000.0);
                    assignment.Score = (long)(traderPoints * matchWeight);
                }

                response.Assignments.Add(assignment);
            }

            response.AssignedMatches = response.Assignments.Count(a => a.Hdp != "N/A");

            // M2: No preconfigured pairs, so summary is simple
            response.Summary.PrimaryPairs = 0;
            response.Summary.SecondaryPairs = 0;
            response.Summary.BackupPairs = 0;
            response.Summary.PreconfiguredPairsUsed = 0;
        }

        return response;
    }
}
