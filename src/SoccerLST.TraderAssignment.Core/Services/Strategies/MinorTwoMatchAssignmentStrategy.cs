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
        { "First", 4 },
        { "Member", 16 },
        { "Elite", 64 }
    };

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        var invalidMatches = matches.Where(m => m.MaxBet < 20000 || m.MaxBet >= 40000).ToList();
        if (invalidMatches.Any())
        {
            return (false, $"Found {invalidMatches.Count} matches outside M2 range (20K-40K). All M2 matches must have MaxBet between 20,000 and 40,000.");
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

        // M2: No skill validation needed

        // Check for conflicts with existing assignments
        if (HasAssignedJobConflict(trader, match))
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
                .Select(t => x[(t.Id, match.Id)])
                .ToArray();
            model.Add(LinearExpr.Sum(traderAssignments) <= 1);
        }

        // Constraint 2: Respect M2Capacity - trader can handle up to M2Capacity overlapping matches
        // This includes both new M2 matches AND existing M2/M3/M4 assigned jobs
        foreach (var trader in traderShifts)
        {
            // Build overlapping groups: find all matches that overlap with each other
            var processedMatches = new HashSet<string>();
            var overlappingGroups = new List<List<Match>>();

            for (int i = 0; i < matches.Count; i++)
            {
                if (processedMatches.Contains(matches[i].Id))
                    continue;

                var group = new List<Match> { matches[i] };
                processedMatches.Add(matches[i].Id);

                // Find all other matches that overlap with any match in the current group
                bool addedNewMatch;
                do
                {
                    addedNewMatch = false;
                    for (int j = 0; j < matches.Count; j++)
                    {
                        if (processedMatches.Contains(matches[j].Id))
                            continue;

                        // Check if matches[j] overlaps with any match in the current group
                        bool overlapsWithGroup = group.Any(m =>
                            matches[j].Start < m.End && m.Start < matches[j].End);

                        if (overlapsWithGroup)
                        {
                            group.Add(matches[j]);
                            processedMatches.Add(matches[j].Id);
                            addedNewMatch = true;
                        }
                    }
                } while (addedNewMatch);

                // Only add groups with 2+ matches (single match groups don't need capacity constraint)
                if (group.Count >= 2)
                {
                    overlappingGroups.Add(group);
                }
            }

            // For each overlapping group, add capacity constraint
            foreach (var group in overlappingGroups)
            {
                // Count existing M2/M3/M4 jobs that overlap with this match group
                int existingJobsCount = 0;
                if (trader.AssignedJobs != null)
                {
                    existingJobsCount = trader.AssignedJobs.Count(job =>
                    {
                        // Only count M2, M3, M4 jobs (these count toward M2 capacity)
                        if (job.Type != "M2" && job.Type != "M3" && job.Type != "M4")
                            return false;

                        // Check if job overlaps with any match in the group
                        return group.Any(m =>
                            job.Start < m.End && m.Start < job.End);
                    });
                }

                // Trader can be assigned to at most (M2Capacity - existingJobsCount) matches in this overlapping group
                var assignmentsInGroup = group
                    .Select(m => x[(trader.Id, m.Id)])
                    .ToArray();

                var availableCapacity = Math.Max(0, trader.M2Capacity - existingJobsCount);
                model.Add(LinearExpr.Sum(assignmentsInGroup) <= availableCapacity);
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

        foreach (var match in matches)
        {
            foreach (var trader in traderShifts)
            {
                var traderPoints = LevelPoints.GetValueOrDefault(trader.Level, 1);
                var traderPriority = GetTraderPriority(trader);

                var traderScore = traderPoints * traderPriority;
                var matchWeight = Math.Pow(match.MaxBet / 1000.0, 2);
                var finalScore = coverageBonus + (traderScore * matchWeight);

                var assignment = x[(trader.Id, match.Id)];
                objectiveTerms.Add(LinearExpr.Term(assignment, (long)Math.Round(finalScore)));
            }
        }

        // Add workload balancing penalty for concurrent matches
        // This encourages even distribution when multiple traders have capacity
        AddWorkloadBalancingPenalty(model, x, traderShifts, matches, objectiveTerms);

        return objectiveTerms;
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

            // Count traders with sufficient capacity for this concurrent group
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

                // Check if trader has capacity for all matches in group
                return t.M2Capacity >= (groupMatches.Count + existingJobs);
            }).ToList();

            // Special case: If only one trader has sufficient capacity for all matches in group,
            // strongly prefer that trader over partial assignments
            if (tradersWithCapacity.Count == 1)
            {
                var singleTrader = tradersWithCapacity[0];

                // Give large bonus for this trader handling all matches in group
                // This bonus should outweigh quality differences
                var bonusPerMatch = (long)(avgMatchWeight * 500000);

                foreach (var match in groupMatches)
                {
                    var assignment = x[(singleTrader.Id, match.Id)];
                    objectiveTerms.Add(LinearExpr.Term(assignment, bonusPerMatch));
                }

                continue;
            }

            // If no traders have capacity, skip balancing
            if (tradersWithCapacity.Count == 0)
                continue;

            // Calculate fair share: matches should be distributed evenly
            var fairShare = (int)Math.Ceiling(groupMatches.Count / (double)tradersWithCapacity.Count);

            // For each trader with capacity, penalize if they exceed fair share
            foreach (var trader in tradersWithCapacity)
            {
                // Count assignments in this concurrent group for this trader
                var assignmentsInGroup = groupMatches
                    .Select(m => x[(trader.Id, m.Id)])
                    .ToArray();

                var groupWorkload = model.NewIntVar(0, groupMatches.Count,
                    $"workload_group_{group.Key.Ticks}_{trader.Id}");
                model.Add(groupWorkload == LinearExpr.Sum(assignmentsInGroup));

                // Calculate excess over fair share
                var excess = model.NewIntVar(0, groupMatches.Count,
                    $"excess_group_{group.Key.Ticks}_{trader.Id}");
                model.Add(excess >= groupWorkload - fairShare);
                model.Add(excess >= 0);

                // Add penalty for excess workload
                // Penalty should be significant to encourage distribution
                // but not so large it overrides quality differences
                var penaltyPerExcess = (long)(avgMatchWeight * 100000);
                objectiveTerms.Add(LinearExpr.Term(excess, -penaltyPerExcess));
            }
        }
    }

    /// <summary>
    /// Override base implementation to add support for Second and Third trader levels
    /// For M2 matches, all manager traders are prioritized behind Third level non-managers
    ///
    /// Priority calculation: finalScore = levelPoints × priority × matchWeight
    /// To ensure Third non-manager (1 × 0.9) > Elite manager (64 × X), we need X < 0.014
    /// </summary>
    protected override double GetTraderPriority(TraderShift trader)
    {
        // For M2: Manager traders have very low priority (all below Third non-manager)
        // Third non-manager: 1 × 0.9 = 0.9
        // Elite manager: 64 × 0.01 = 0.64 (lower than Third)
        if (trader.IsManager)
        {
            return trader.Level switch
            {
                "Elite" => 0.010,      // 64 × 0.010 = 0.64
                "Member" => 0.009,     // 16 × 0.009 = 0.144
                "First" => 0.008,      // 4 × 0.008 = 0.032
                "Second" => 0.007,     // 2 × 0.007 = 0.014
                "Third" => 0.006,      // 1 × 0.006 = 0.006
                _ => 0
            };
        }

        // Non-manager priorities (normal hierarchy)
        return trader.Level switch
        {
            "Elite" => 2.0,
            "Member" => 1.3,
            "First" => 1.2,
            "Second" => 1.0,
            "Third" => 0.9,
            _ => 0
        };
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
            Success = status == CpSolverStatus.Optimal,
            Status = status.ToString(),
            TotalMatches = matches.Count,
            Assignments = new List<MatchAssignment>()
        };

        if (status == CpSolverStatus.Optimal)
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
                    // M2: Same trader handles both HDP and OU
                    assignment.Hdp = assignedTrader.Name;
                    assignment.HdpLevel = assignedTrader.Level;
                    assignment.Ou = assignedTrader.Name;
                    assignment.OuLevel = assignedTrader.Level;
                    assignment.PairType = "Single Trader (M2)";

                    var traderPoints = LevelPoints.GetValueOrDefault(assignedTrader.Level, 1);
                    var traderPriority = GetTraderPriority(assignedTrader);
                    var matchWeight = (match.MaxBet / 1000.0) * (match.MaxBet / 1000.0);
                    assignment.Score = (long)(traderPoints * traderPriority * matchWeight);
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
