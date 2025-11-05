using Google.OrTools.Sat;
using SoccerLST.TraderAssignment.Core.Models;

namespace SoccerLST.TraderAssignment.Core.Services.Strategies;

/// <summary>
/// Assignment strategy for Major (MJ) matches
/// Rules:
/// - MaxBet >= 70,000
/// - Elite/Member/First level traders
/// - EPL and high-priority leagues get best pairs
/// - 15-minute gap after PES/PK virtual matches
/// </summary>
public class MajorMatchAssignmentStrategy : RealMatchAssignmentStrategyBase
{
    public override string MatchType => "MJ";

    private static readonly Dictionary<string, int> LevelPoints = new()
    {
        { "First", 4 },
        { "Member", 16 },
        { "Elite", 256 }
    };

    private static readonly string[] Roles = { "HDP", "OU" };

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        var invalidMatches = matches.Where(m => m.MaxBet < 70000).ToList();
        if (invalidMatches.Any())
        {
            return (false, $"Found {invalidMatches.Count} matches with MaxBet < 70,000. All MJ matches must have MaxBet >= 70,000.");
        }
        return (true, null);
    }

    public override AssignmentResponse Assign(AssignmentRequest request)
    {
        var traderShifts = request.TraderShifts;
        var matches = request.Matches.Where(m => !m.IsAlreadyHandled).ToList();

        // Build CP-SAT model
        var model = new CpModel();

        // Create decision variables
        var x = CreateDecisionVariables(model, traderShifts, matches);

        // Apply constraints
        ApplyConstraints(model, x, traderShifts, matches);

        // Build objective function
        var objectiveTerms = BuildObjective(model, x, traderShifts, matches, request.PreconfiguredPairs);
        model.Maximize(LinearExpr.Sum(objectiveTerms));

        // Solve
        var solver = new CpSolver();
        var status = solver.Solve(model);

        // Build response
        return BuildResponse(status, solver, x, traderShifts, matches, request.PreconfiguredPairs);
    }

    private Dictionary<(string traderId, string matchId, string role), IntVar> CreateDecisionVariables(
        CpModel model,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        var x = new Dictionary<(string traderId, string matchId, string role), IntVar>();

        foreach (var trader in traderShifts)
        {
            foreach (var match in matches)
            {
                foreach (var role in Roles)
                {
                    var key = (trader.Id, match.Id, role);

                    if (CanTraderBeAssigned(trader, match, role))
                    {
                        x[key] = model.NewBoolVar($"x_{trader.Id}_{match.Id}_{role}");
                    }
                    else
                    {
                        x[key] = model.NewConstant(0);
                    }
                }
            }
        }

        return x;
    }

    private bool CanTraderBeAssigned(TraderShift trader, Match match, string role)
    {
        // Check basic eligibility
        if (!HasRequiredSkill(trader, role))
            return false;

        if (!IsMatchWithinShift(trader, match))
            return false;

        // Check if trader ignores this league
        if (IsLeagueIgnored(trader, match))
            return false;

        // Check if trader cannot handle EXPT matches
        if (IsEXPTMatchRestricted(trader, match))
            return false;

        // Check for conflicts with existing assignments
        if (HasAssignedJobConflict(trader, match))
            return false;

        return true;
    }

    private static bool HasRequiredSkill(TraderShift trader, string role)
    {
        return trader.Skills.Contains(role);
    }

    private void ApplyConstraints(
        CpModel model,
        Dictionary<(string traderId, string matchId, string role), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches)
    {
        // Constraint 1: Each match has at most 1 HDP + 1 OU
        foreach (var match in matches)
        {
            foreach (var role in Roles)
            {
                var roleAssignments = traderShifts
                    .Select(t => x[(t.Id, match.Id, role)])
                    .ToArray();
                model.Add(LinearExpr.Sum(roleAssignments) <= 1);
            }
        }

        // Constraint 2: Trader cannot work overlapping matches (in any role)
        foreach (var trader in traderShifts)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                for (int j = i + 1; j < matches.Count; j++)
                {
                    var m1 = matches[i];
                    var m2 = matches[j];

                    bool overlap = m1.Start < m2.End && m2.Start < m1.End;
                    if (overlap)
                    {
                        // Trader can be in at most ONE of the two matches (regardless of role)
                        // This prevents: HDP in m1 + OU in m2, or HDP in m1 + HDP in m2, etc.
                        var m1Assignments = Roles.Select(role => x[(trader.Id, m1.Id, role)]).ToArray();
                        var m2Assignments = Roles.Select(role => x[(trader.Id, m2.Id, role)]).ToArray();
                        model.Add(LinearExpr.Sum(m1Assignments) + LinearExpr.Sum(m2Assignments) <= 1);
                    }
                }
            }
        }

        // Constraint 3: Trader cannot be both HDP and OU for same match
        foreach (var trader in traderShifts)
        {
            foreach (var match in matches)
            {
                model.Add(x[(trader.Id, match.Id, "HDP")] + x[(trader.Id, match.Id, "OU")] <= 1);
            }
        }

        // Constraint 4: Match must have both HDP and OU or neither
        foreach (var match in matches)
        {
            var hdpAssigned = traderShifts.Select(t => x[(t.Id, match.Id, "HDP")]).ToArray();
            var ouAssigned = traderShifts.Select(t => x[(t.Id, match.Id, "OU")]).ToArray();

            model.Add(LinearExpr.Sum(hdpAssigned) == LinearExpr.Sum(ouAssigned));
        }
    }

    private List<LinearExpr> BuildObjective(
        CpModel model,
        Dictionary<(string traderId, string matchId, string role), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches,
        List<PreconfiguredPair> pairs)
    {
        var objectiveTerms = new List<LinearExpr>();

        foreach (var match in matches)
        {
            foreach (var hdpTrader in traderShifts)
            {
                foreach (var ouTrader in traderShifts)
                {
                    if (hdpTrader.Id == ouTrader.Id) continue;

                    var (isPreconfig, tierBonus, leagueBonus, _) =
                        GetPairConfig(hdpTrader.Name, ouTrader.Name, match.LeagueCode, pairs);

                    var hdpPoints = LevelPoints.GetValueOrDefault(hdpTrader.Level, 1);
                    var ouPoints = LevelPoints.GetValueOrDefault(ouTrader.Level, 1);

                    var hdpPriority = GetTraderPriority(hdpTrader);
                    var ouPriority = GetTraderPriority(ouTrader);

                    var hdpScore = hdpPoints * hdpPriority;
                    var ouScore = ouPoints * ouPriority;

                    var pairScore = hdpScore * ouScore * tierBonus * leagueBonus;
                    var matchWeight = (match.MaxBet / 1000.0) * (match.MaxBet / 1000.0);

                    // EPL gets priority bonus to attract elite traders when MaxBet is equal
                    var leagueMultiplier = match.LeagueCode == "EPL" ? 1.1 : 1.0;

                    var finalScore = pairScore * matchWeight * leagueMultiplier;

                    var hdpVar = x[(hdpTrader.Id, match.Id, "HDP")];
                    var ouVar = x[(ouTrader.Id, match.Id, "OU")];

                    var pairAssigned = model.NewBoolVar($"pair_{hdpTrader.Id}_{ouTrader.Id}_{match.Id}");
                    model.AddMinEquality(pairAssigned, new[] { hdpVar, ouVar });

                    objectiveTerms.Add(LinearExpr.Term(pairAssigned, (long)Math.Round(finalScore)));
                }
            }
        }

        return objectiveTerms;
    }

    private AssignmentResponse BuildResponse(
        CpSolverStatus status,
        CpSolver solver,
        Dictionary<(string traderId, string matchId, string role), IntVar> x,
        List<TraderShift> traderShifts,
        List<Match> matches,
        List<PreconfiguredPair> pairs)
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
            foreach (var match in matches.OrderBy(m => m.LeagueCode ?? "ZZZ").ThenByDescending(m => m.MaxBet))
            {
                var hdpTrader = traderShifts.FirstOrDefault(t =>
                    solver.Value(x[(t.Id, match.Id, "HDP")]) == 1);
                var ouTrader = traderShifts.FirstOrDefault(t =>
                    solver.Value(x[(t.Id, match.Id, "OU")]) == 1);

                var assignment = new MatchAssignment
                {
                    RowNumber = match.Id,
                    Match = match.MatchNo,
                    League = match.LeagueCode ?? "N/A",
                    MaxBet = match.MaxBet,
                    Time = $"{match.Start:MM-dd HH:mm} - {match.End:HH:mm}"
                };

                if (hdpTrader == null || ouTrader == null)
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
                    var (isPreconfig, tierBonus, leagueBonus, tierName) =
                        GetPairConfig(hdpTrader.Name, ouTrader.Name, match.LeagueCode, pairs);

                    string pairType = "Regular";
                    if (isPreconfig)
                    {
                        pairType = $"{tierName} (League {(leagueBonus > 1.0 ? "✓" : "-")})";
                    }

                    var hdpPoints = LevelPoints.GetValueOrDefault(hdpTrader.Level, 1);
                    var ouPoints = LevelPoints.GetValueOrDefault(ouTrader.Level, 1);
                    var hdpPriority = GetTraderPriority(hdpTrader);
                    var ouPriority = GetTraderPriority(ouTrader);

                    var pairScore = (hdpPoints * hdpPriority) * (ouPoints * ouPriority) * tierBonus * leagueBonus;
                    var matchWeight = (match.MaxBet / 1000.0) * (match.MaxBet / 1000.0);

                    assignment.Hdp = hdpTrader.Name;
                    assignment.HdpLevel = hdpTrader.Level;
                    assignment.Ou = ouTrader.Name;
                    assignment.OuLevel = ouTrader.Level;
                    assignment.PairType = pairType;
                    assignment.Score = (long)(pairScore * matchWeight);
                }

                response.Assignments.Add(assignment);
            }

            response.AssignedMatches = response.Assignments.Count(a => a.Hdp != "N/A");

            // Calculate summary
            response.Summary.PrimaryPairs = response.Assignments.Count(a => a.PairType.Contains("Primary"));
            response.Summary.SecondaryPairs = response.Assignments.Count(a => a.PairType.Contains("Secondary"));
            response.Summary.BackupPairs = response.Assignments.Count(a => a.PairType.Contains("Backup"));
            response.Summary.PreconfiguredPairsUsed =
                response.Summary.PrimaryPairs +
                response.Summary.SecondaryPairs +
                response.Summary.BackupPairs;
        }

        return response;
    }

    private static (bool isPreconfig, double tierBonus, double leagueBonus, string tierName) GetPairConfig(
        string hdpName, string ouName, string? matchLeague, List<PreconfiguredPair> pairs)
    {
        var pair = pairs.FirstOrDefault(p => p.Hdp == hdpName && p.Ou == ouName);
        if (pair == null)
            return (false, 1.0, 1.0, string.Empty);

        var tierBonus = pair.Tier switch
        {
            "primary" => 1.5,
            "secondary" => 1.3,
            "backup" => 1.2,
            _ => 1.0
        };

        var tierName = pair.Tier switch
        {
            "primary" => "Primary",
            "secondary" => "Secondary",
            "backup" => "Backup",
            _ => "Unknown"
        };

        if (pair.LeaguePriority == null || matchLeague == null)
            return (true, tierBonus, 1.0, tierName);

        if (pair.LeaguePriority.TryGetValue(1, out var firstPriority) && firstPriority.Contains(matchLeague!))
            return (true, tierBonus, 1.5, tierName);

        if (pair.LeaguePriority.TryGetValue(2, out var secondPriority) && secondPriority.Contains(matchLeague!))
            return (true, tierBonus, 1.3, tierName);

        if (pair.LeaguePriority.TryGetValue(3, out var thirdPriority) && thirdPriority.Contains(matchLeague!))
            return (true, tierBonus, 1.1, tierName);

        return (true, tierBonus, 1.0, tierName);
    }
}
