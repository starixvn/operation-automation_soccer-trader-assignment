using Google.OrTools.Sat;
using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies.M1;

/// <summary>
/// Assignment strategy for Minor 1 (M1) matches
/// Rules:
/// - MaxBet: 40,000 - 70,000
/// - One trader handles both HDP and OU
/// - Respects M1 capacity per trader (m1Capacity property)
/// - Considers ALL MJ matches as busy (higher priority)
/// - Considers M1 unchanged matches as busy
/// - 15-minute gap after PES/PK virtual matches (same as MJ)
/// - No skill validation required
/// </summary>
public class MinorOneMatchAssignmentStrategy : RealMatchAssignmentStrategyBase
{
    public override string MatchType => "M1";

    private static readonly Dictionary<string, int> LevelPoints = new()
    {
        { "Third", 1 },
        { "Second", 2 },
        { "First", 4 },
        { "Member", 16 },
        { "Elite", 64 }
    };

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        // If all matches have BetType explicitly set to "M1", validation passes
        if (matches.All(m => m.BetType == "M1"))
        {
            return (true, null);
        }

        // Otherwise, validate using MaxBet ranges as fallback
        var invalidMatches = matches.Where(m => m.BetType != "M1" && (m.MaxBet < 40000 || m.MaxBet >= 70000)).ToList();
        if (invalidMatches.Any())
        {
            return (false, $"Found {invalidMatches.Count} matches outside M1 range (40K-70K) and BetType != 'M1'. All M1 matches must have BetType='M1' or MaxBet between 40,000 and 70,000.");
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
        // M1: One trader handles both HDP and OU, so no role dimension needed
        var x = CreateDecisionVariables(model, traderShifts, matches);

        // Apply constraints
        ApplyConstraints(model, x, traderShifts, matches);

        // Build objective function
        var objectiveTerms = BuildObjective(x, traderShifts, matches);
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
                var key = (trader.Id, match.Id);

                if (CanTraderBeAssigned(trader, match))
                {
                    x[key] = model.NewBoolVar($"x_{trader.Id}_{match.Id}");
                }
                else
                {
                    x[key] = model.NewConstant(0);
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

        // M1: No skill validation needed

        // Check for conflicts with existing assignments
        if (HasAssignedJobConflict(trader, match))
            return false;

        // Check if trader's MaxBet is sufficient for this match
        if (trader.MaxBet < match.MaxBet)
            return false;

        return true;
    }

    // Override base implementation to handle M3/M4 capacity logic specific to M1
    private new bool HasAssignedJobConflict(TraderShift trader, Match match)
    {
        if (trader.AssignedJobs == null || !trader.AssignedJobs.Any())
            return false;

        // Count M3/M4 jobs with same start time as match (they can run concurrently if M1Capacity allows)
        var concurrentM3M4Jobs = trader.AssignedJobs
            .Where(j => (j.Type == "M3" || j.Type == "M4") && j.Start == match.Start)
            .Count();

        // If we have M3/M4 jobs at same start time, check if we have capacity
        // M1Capacity includes both M1 matches and M3/M4 jobs running concurrently
        if (concurrentM3M4Jobs > 0)
        {
            // Need at least (concurrentM3M4Jobs + 1) capacity to add this M1 match
            if (trader.M1Capacity < concurrentM3M4Jobs + 1)
                return true; // Not enough capacity
        }

        foreach (var job in trader.AssignedJobs)
        {
            // M3/M4 jobs with same start time don't cause overlap conflict (capacity handles it)
            if ((job.Type == "M3" || job.Type == "M4") && job.Start == match.Start)
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
                .Select(t => x[(t.Id, match.Id)])
                .ToArray();
            model.Add(LinearExpr.Sum(traderAssignments) <= 1);
        }

        // Constraint 2: Each trader can handle at most 1 M1 match at any given time
        // M1Capacity is only used for M3/M4 concurrent jobs (handled in HasAssignedJobConflict),
        // NOT for multiple M1 matches
        foreach (var trader in traderShifts)
        {
            // Find all pairs of overlapping matches
            for (int i = 0; i < matches.Count; i++)
            {
                for (int j = i + 1; j < matches.Count; j++)
                {
                    var m1 = matches[i];
                    var m2 = matches[j];

                    bool overlap = m1.Start < m2.End && m2.Start < m1.End;
                    if (overlap)
                    {
                        // If two M1 matches overlap, trader can be assigned to at most ONE of them
                        model.Add(x[(trader.Id, m1.Id)] + x[(trader.Id, m2.Id)] <= 1);
                    }
                }
            }
        }
    }

    private List<LinearExpr> BuildObjective(
        Dictionary<(string traderId, string matchId), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        var objectiveTerms = new List<LinearExpr>();

        // Base bonus to prioritize coverage - any assignment is better than no assignment
        const long coverageBonus = 1_000_000;
        const long consecutiveMatchBonus = 500; // Bonus for consecutive matches (0-2 hour gap)

        // 1. Individual match assignment scores
        foreach (var match in matches)
        {
            foreach (var trader in traderShifts)
            {
                var traderPoints = LevelPoints.GetValueOrDefault(trader.Level, 1);
                var priorityLeagueBonus = GetPriorityLeagueBonus(trader, match);

                var traderScore = traderPoints * priorityLeagueBonus;
                var matchWeight = Math.Pow(match.MaxBet / 1000.0, 2);
                var finalScore = coverageBonus + traderScore * matchWeight;

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
                    bool isConsecutive = match.Start == job.End || match.End == job.Start;

                    if (isConsecutive)
                    {
                        // If trader is assigned to this match, add bonus
                        // No multiplication needed - job is already assigned
                        objectiveTerms.Add(LinearExpr.Term(x[(trader.Id, match.Id)], consecutiveMatchBonus));
                    }
                }
            }
        }

        // 3. Shift boundary bonus: Match aligns perfectly with shift boundaries
        // Encourages efficient use of trader shifts
        const long shiftBoundaryBonus = 500; // Bonus for matches that align with shift start/end

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
                bool alignsWithShiftStart = match.Start == trader.Start;
                bool alignsWithShiftEnd = match.End == trader.End;

                if (alignsWithShiftStart || alignsWithShiftEnd)
                {
                    // Add bonus for perfect shift boundary alignment
                    objectiveTerms.Add(LinearExpr.Term(x[(trader.Id, match.Id)], shiftBoundaryBonus));
                }
            }
        }

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
                    solver.Value(x[(t.Id, match.Id)]) == 1);

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
                    // M1: Same trader handles both HDP and OU
                    assignment.Hdp = assignedTrader.Name;
                    assignment.HdpLevel = assignedTrader.Level;
                    assignment.Ou = assignedTrader.Name;
                    assignment.OuLevel = assignedTrader.Level;
                    assignment.PairType = "Single Trader (M1)";

                    var traderPoints = LevelPoints.GetValueOrDefault(assignedTrader.Level, 1);
                    var matchWeight = match.MaxBet / 1000.0 * (match.MaxBet / 1000.0);
                    assignment.Score = (long)(traderPoints * matchWeight);
                }

                response.Assignments.Add(assignment);
            }

            response.AssignedMatches = response.Assignments.Count(a => a.Hdp != "N/A");

            // M1: No preconfigured pairs, so summary is simple
            response.Summary.PrimaryPairs = 0;
            response.Summary.SecondaryPairs = 0;
            response.Summary.BackupPairs = 0;
            response.Summary.PreconfiguredPairsUsed = 0;
        }

        return response;
    }
}
