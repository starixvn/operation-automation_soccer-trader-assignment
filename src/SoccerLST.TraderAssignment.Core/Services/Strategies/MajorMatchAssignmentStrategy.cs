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
        { "First", 2 },
        { "Member", 8 },
        { "Elite", 64 }
    };

    private static readonly string[] Roles = { "HDP", "OU" };

    public override (bool isValid, string? errorMessage) ValidateMatches(List<Match> matches)
    {
        // If all matches have BetType explicitly set to "MJ", validation passes
        if (matches.All(m => m.BetType == "MJ"))
        {
            return (true, null);
        }

        // Otherwise, validate using MaxBet ranges as fallback
        var invalidMatches = matches.Where(m => m.BetType != "MJ" && m.MaxBet < 70000).ToList();
        if (invalidMatches.Any())
        {
            return (false, $"Found {invalidMatches.Count} matches with MaxBet < 70,000 and BetType != 'MJ'. All MJ matches must have BetType='MJ' or MaxBet >= 70,000.");
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
        if (!HasRequiredSkill(trader, role) && !match.IsUseAlternativeMJRule)
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

        // Check if trader's MaxBet is sufficient for this match
        if (trader.MaxBet < match.MaxBet)
            return false;

        // Check NonSoloLeagues constraint for solo matches
        if (match.IsSoloMatch && trader.NonSoloLeagues != null && trader.NonSoloLeagues.Any())
        {
            if (!string.IsNullOrEmpty(match.LeagueName))
            {
                // Case-insensitive substring matching
                foreach (var restrictedLeague in trader.NonSoloLeagues)
                {
                    if (match.LeagueName.Contains(restrictedLeague, StringComparison.OrdinalIgnoreCase))
                    {
                        return false; // Trader is restricted from handling solo matches in this league
                    }
                }
            }
        }

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
                        // For matches with at least one alternative: Trader can work on at most ONE match
                        // (but can have both roles on that one match if it's alternative)
                        // Create variables to track if trader is assigned to each match
                        var worksOnM1 = model.NewBoolVar($"{trader.Id}_works_on_{m1.Id}_{m2.Id}");
                        var worksOnM2 = model.NewBoolVar($"{trader.Id}_works_on_{m2.Id}_{m1.Id}");

                        // worksOnM1 = 1 if trader has ANY role on m1
                        var m1Assignments = Roles.Select(role => x[(trader.Id, m1.Id, role)]).ToArray();
                        model.AddMaxEquality(worksOnM1, m1Assignments);

                        // worksOnM2 = 1 if trader has ANY role on m2
                        var m2Assignments = Roles.Select(role => x[(trader.Id, m2.Id, role)]).ToArray();
                        model.AddMaxEquality(worksOnM2, m2Assignments);

                        // Trader can work on at most ONE of the overlapping matches
                        model.Add(worksOnM1 + worksOnM2 <= 1);
                    }
                }
            }
        }

        // Constraint 3: Trader cannot be both HDP and OU for same match
        // (only applies when IsUseAlternativeMJRule = false)
        foreach (var trader in traderShifts)
        {
            foreach (var match in matches)
            {
                // Skip this constraint for matches using alternative MJ rule
                if (match.IsUseAlternativeMJRule)
                    continue;

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

        var normalMatches = matches.Where(m => !m.IsUseAlternativeMJRule).ToList();
        var allAlternativeMatches = matches.Where(m => m.IsUseAlternativeMJRule).ToList();

        foreach (var match in normalMatches)
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

                    // EPL Elite boost: If EPL match has at least one Elite trader,
                    // upgrade Member points to Elite level (makes Member "score like Elite")
                    var eplEliteBoost = 1.0;
                    if (match.LeagueCode == "EPL" || isPreconfig)
                    {
                        var hdpIsElite = hdpTrader.Level == "Elite";
                        var ouIsElite = ouTrader.Level == "Elite";

                        if (match.LeagueCode == "EPL")
                        {
                            // Both Elite in EPL: 1.2x boost
                            eplEliteBoost = 1.3;
                        }

                        if (hdpIsElite || ouIsElite)
                        {
                            // One Elite + One Member: Upgrade Member points to Elite level
                            // Then apply 1.05x multiplier
                            if (!hdpIsElite)
                            {
                                hdpPoints = LevelPoints["Elite"]; // Upgrade Member to Elite points
                            }
                            if (!ouIsElite)
                            {
                                ouPoints = LevelPoints["Elite"]; // Upgrade Member to Elite points
                            }

                            if (hdpIsElite && ouIsElite)
                            {
                                if (match.LeagueCode == "EPL")
                                {
                                    eplEliteBoost = eplEliteBoost * 1.5; // Both Elite in EPL: additional 1.1x boost
                                }
                            }
                            else
                            {
                                if (match.LeagueCode == "EPL" && (hdpTrader.Level == "Member" || ouTrader.Level == "Member"))
                                {
                                    eplEliteBoost = eplEliteBoost * 1.05; // One Elite + One Member in EPL: 1.05x boost
                                }
                            }

                        }
                    }

                    // Apply PrioritySkill bonus (1.1x multiplier if trader's PrioritySkill matches their role)
                    var hdpPrioritySkillBonus = GetPrioritySkillBonus(hdpTrader, "HDP");
                    var ouPrioritySkillBonus = GetPrioritySkillBonus(ouTrader, "OU");

                    var hdpScore = hdpPoints * hdpPrioritySkillBonus;
                    var ouScore = ouPoints * ouPrioritySkillBonus;

                    var pairScore = hdpScore * ouScore * tierBonus * leagueBonus;
                    var matchWeight = (match.MaxBet / 1000.0) * (match.MaxBet / 1000.0);

                    // Add initial base score of 1,000,000 to ensure all matches have minimum value
                    const long initScore = 1000000;
                    var finalScore = initScore + (pairScore * matchWeight * eplEliteBoost);

                    var hdpVar = x[(hdpTrader.Id, match.Id, "HDP")];
                    var ouVar = x[(ouTrader.Id, match.Id, "OU")];

                    var pairAssigned = model.NewBoolVar($"pair_{hdpTrader.Id}_{ouTrader.Id}_{match.Id}");
                    model.AddMinEquality(pairAssigned, new[] { hdpVar, ouVar });

                    objectiveTerms.Add(LinearExpr.Term(pairAssigned, (long)Math.Round(finalScore)));
                }
            }
        }

        foreach (var match in allAlternativeMatches)
        {
            long initScore = 1000000;
            // Strategy 1: Score individual role assignments (base scoring with priority skill bonuses)
            foreach (var trader in traderShifts)
            {
                foreach (var role in Roles)
                {
                    var points = LevelPoints.GetValueOrDefault(trader.Level, 1);

                    // Apply priority skill bonus
                    var prioritySkillBonus = GetPrioritySkillBonus(trader, role);
                    var skillScore = points * prioritySkillBonus;

                    var score = skillScore * skillScore;
                    var matchWeight = match.MaxBet / 1000.0;

                    // Add initial base score of 1,000,000 to ensure all matches have minimum value
                    var finalScore = initScore + (score * matchWeight);

                    var varAssigned = x[(trader.Id, match.Id, role)];

                    objectiveTerms.Add(LinearExpr.Term(varAssigned, (long)Math.Round(finalScore)));
                }
            }

            // Strategy 2: Add coverage bonus for using 2 different traders (when redundant traders available)
            // This encourages the solver to use different traders when possible, maximizing trader utilization
            // Also includes preconfigured pair bonuses and priority skill bonuses
            foreach (var hdpTrader in traderShifts)
            {
                foreach (var ouTrader in traderShifts)
                {
                    if (hdpTrader.Id == ouTrader.Id) continue; // Skip same trader pairs

                    var hdpPoints = LevelPoints.GetValueOrDefault(hdpTrader.Level, 1);
                    var ouPoints = LevelPoints.GetValueOrDefault(ouTrader.Level, 1);

                    // Check if this is a preconfigured pair
                    var (isPreconfig, tierBonus, leagueBonus, _) =
                        GetPairConfig(hdpTrader.Name, ouTrader.Name, match.LeagueCode, pairs);

                    // Apply PrioritySkill bonus (1.1x multiplier if trader's PrioritySkill matches their role)
                    var hdpPrioritySkillBonus = GetPrioritySkillBonus(hdpTrader, "HDP");
                    var ouPrioritySkillBonus = GetPrioritySkillBonus(ouTrader, "OU");

                    var hdpVar = x[(hdpTrader.Id, match.Id, "HDP")];
                    var ouVar = x[(ouTrader.Id, match.Id, "OU")];

                    // Create a variable that's 1 only when both traders are assigned (forming a pair)
                    var pairAssigned = model.NewBoolVar($"alt_pair_{hdpTrader.Id}_{ouTrader.Id}_{match.Id}");
                    model.AddMinEquality(pairAssigned, new[] { hdpVar, ouVar });

                    // Calculate pair score with all bonuses
                    var hdpScore = hdpPoints * hdpPrioritySkillBonus;
                    var ouScore = ouPoints * ouPrioritySkillBonus;
                    var pairScore = 10000 * tierBonus * leagueBonus;

                    // Coverage bonus: significantly reward using 2 different traders
                    // This bonus should be large enough to overcome the point difference between levels
                    var matchWeight = match.MaxBet / 1000.0;
                    const long coverageBonus = 10000; // Large bonus to encourage 2-trader coverage

                    var finalScore = initScore + coverageBonus + pairScore + (hdpScore * ouScore * matchWeight);

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

                    // Apply PrioritySkill bonus
                    var hdpPrioritySkillBonus = GetPrioritySkillBonus(hdpTrader, "HDP");
                    var ouPrioritySkillBonus = GetPrioritySkillBonus(ouTrader, "OU");

                    var pairScore = (hdpPoints * hdpPriority * hdpPrioritySkillBonus) * (ouPoints * ouPriority * ouPrioritySkillBonus) * tierBonus * leagueBonus;
                    var matchWeight = (match.MaxBet / 1000.0) * (match.MaxBet / 1000.0);

                    // Note: We don't include EPL multiplier or Elite boost in displayed score
                    // Those are optimization weights only, not actual performance scores
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
            return (true, tierBonus, 1.2, tierName);

        return (true, tierBonus, 1.0, tierName);
    }

    /// <summary>
    /// Get priority skill bonus for trader
    /// Returns 1.1x multiplier if trader's PrioritySkill matches the role they're being assigned to
    /// Otherwise returns 1.0 (no bonus)
    /// </summary>
    private static double GetPrioritySkillBonus(TraderShift trader, string role)
    {
        if (string.IsNullOrEmpty(trader.PrioritySkill))
            return 1.0;

        return trader.PrioritySkill.Equals(role, StringComparison.OrdinalIgnoreCase) ? 1.1 : 1.0;
    }
}
