using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public class MajorMatchAssignmentStrategyAlternativeMJRuleTests
{
    private MajorMatchAssignmentStrategy _strategy;

    [SetUp]
    public void Setup()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_WithAlternativeMJRule_SingleTraderCanHandleBothRoles()
    {
        // Arrange - One match with IsUseAlternativeMJRule = true, one trader
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new[] { "HDP", "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000, "EPL", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed with alternative MJ rule, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(1, "the match should be assigned");

        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("EliteTrader", "single trader should handle HDP role");
        assignment.Ou.Should().Be("EliteTrader", "same trader should handle OU role");
        assignment.HdpLevel.Should().Be("Elite");
        assignment.OuLevel.Should().Be("Elite");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_TwoSpecialistTraders_BothAssignedToMatch()
    {
        // Arrange - One alternative MJ match, one HDP specialist, one OU specialist
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "HdpSpecialist", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "OuSpecialist", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed with alternative MJ rule, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(1, "the match should be assigned with both traders");

        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("HdpSpecialist", "HDP specialist should be assigned to HDP role");
        assignment.Ou.Should().Be("OuSpecialist", "OU specialist should be assigned to OU role");
        assignment.HdpLevel.Should().Be("Elite");
        assignment.OuLevel.Should().Be("Member");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_CoverageFirstStrategy_MaximizesMatchAssignments()
    {
        // Arrange - Two alternative MJ matches, one versatile trader, two specialists
        // Coverage-first should assign specialists separately to maximize match coverage
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "VersatileTrader", new[] { "HDP", "OU" }, "Elite", false),
                CreateTraderShift("2", "HdpSpecialist", new[] { "HDP" }, "Member", false),
                CreateTraderShift("3", "OuSpecialist", new[] { "OU" }, "First", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T14:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
                    70000, "ITA", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed with alternative MJ rule, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        // Verify that coverage-first strategy uses different traders to maximize match assignments
        var assignments = result.Assignments.OrderBy(a => a.RowNumber).ToList();

        // Each match should have both roles assigned
        assignments[0].Hdp.Should().NotBe("N/A", "first match should have HDP assigned");
        assignments[0].Ou.Should().NotBe("N/A", "first match should have OU assigned");
        assignments[1].Hdp.Should().NotBe("N/A", "second match should have HDP assigned");
        assignments[1].Ou.Should().NotBe("N/A", "second match should have OU assigned");

        // Count unique traders used across all assignments
        var tradersUsed = new HashSet<string>();
        if (assignments[0].Hdp != "N/A") tradersUsed.Add(assignments[0].Hdp);
        if (assignments[0].Ou != "N/A") tradersUsed.Add(assignments[0].Ou);
        if (assignments[1].Hdp != "N/A") tradersUsed.Add(assignments[1].Hdp);
        if (assignments[1].Ou != "N/A") tradersUsed.Add(assignments[1].Ou);

        tradersUsed.Count.Should().BeGreaterThanOrEqualTo(2,
            "coverage-first strategy should utilize multiple traders when available");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_TwoEliteTraders_TwoMatches_EachTraderGetsOneMatch()
    {
        // Arrange - Two OVERLAPPING alternative MJ matches with same MaxBet, two Elite versatile traders
        // When matches overlap, each trader can only handle one match completely
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader1", new[] { "HDP", "OU" }, "Elite", false),
                CreateTraderShift("2", "EliteTrader2", new[] { "HDP", "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T11:00:00Z"),  // Overlaps with Match 1
                    DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true) // Same MaxBet
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed with alternative MJ rule, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        var assignments = result.Assignments.OrderBy(a => a.RowNumber).ToList();

        // Each match should have both roles assigned
        assignments[0].Hdp.Should().NotBe("N/A", "first match should have HDP assigned");
        assignments[0].Ou.Should().NotBe("N/A", "first match should have OU assigned");
        assignments[1].Hdp.Should().NotBe("N/A", "second match should have HDP assigned");
        assignments[1].Ou.Should().NotBe("N/A", "second match should have OU assigned");

        // Verify: When matches overlap, each trader can only handle one complete match
        // Match 1 should be handled by one trader (both HDP and OU)
        var match1Trader = assignments[0].Hdp == assignments[0].Ou;
        var match2Trader = assignments[1].Hdp == assignments[1].Ou;

        match1Trader.Should().BeTrue("first match should be handled by single trader (both HDP and OU) due to overlap constraint");
        match2Trader.Should().BeTrue("second match should be handled by single trader (both HDP and OU) due to overlap constraint");

        // The two matches should use different traders (due to overlap constraint)
        assignments[0].Hdp.Should().NotBe(assignments[1].Hdp,
            "overlapping matches must use different traders to avoid conflicts");
    }

    [Test]
    public void Assign_WithMixedOverlappingMatches_AlternativeAndNormal_BothAssigned()
    {
        // Arrange - One alternative match + one normal match (overlapping), 3 traders with different levels
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new[] { "HDP", "OU" }, "Elite", false),
                CreateTraderShift("2", "MemberTrader", new[] { "HDP", "OU" }, "Member", false),
                CreateTraderShift("3", "FirstTrader", new[] { "HDP", "OU" }, "First", false)
            },
            Matches = new List<Match>
            {
                // Alternative MJ match
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true),
                // Normal MJ match (overlaps with Match 1)
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T11:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
                    85000, "ITA", isUseAlternativeMJRule: false)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed with mixed overlapping matches, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        var assignments = result.Assignments.OrderBy(a => a.RowNumber).ToList();

        // Both matches should have both roles assigned
        assignments[0].Hdp.Should().NotBe("N/A", "alternative match should have HDP assigned");
        assignments[0].Ou.Should().NotBe("N/A", "alternative match should have OU assigned");
        assignments[1].Hdp.Should().NotBe("N/A", "normal match should have HDP assigned");
        assignments[1].Ou.Should().NotBe("N/A", "normal match should have OU assigned");

        // Verify: Alternative match can use same trader for both roles
        var match1SameTrader = assignments[0].Hdp == assignments[0].Ou;

        // Verify: Normal match must use different traders for HDP and OU
        var match2DifferentTraders = assignments[1].Hdp != assignments[1].Ou;
        match2DifferentTraders.Should().BeTrue("normal match must have different traders for HDP and OU");

        // Count unique traders used
        var tradersUsed = new HashSet<string>();
        tradersUsed.Add(assignments[0].Hdp);
        tradersUsed.Add(assignments[0].Ou);
        tradersUsed.Add(assignments[1].Hdp);
        tradersUsed.Add(assignments[1].Ou);

        tradersUsed.Count.Should().BeGreaterThanOrEqualTo(2,
            "at least 2 traders should be used due to overlapping constraint");

        // Verify no trader works on both overlapping matches
        var match1Traders = new HashSet<string> { assignments[0].Hdp, assignments[0].Ou };
        var match2Traders = new HashSet<string> { assignments[1].Hdp, assignments[1].Ou };

        match1Traders.Intersect(match2Traders).Should().BeEmpty(
            "no trader should work on both overlapping matches");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_PreconfiguredPair_PrimaryTier_AppliesBonuses()
    {
        // Arrange - Alternative MJ match with preconfigured pair (Primary tier)
        // Test that the system correctly identifies and applies bonuses to preconfigured pairs
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "JohnDoe", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "JaneSmith", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "RandomHdp", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("4", "RandomOu", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "JohnDoe",
                    Ou = "JaneSmith",
                    Tier = "Primary",
                    LeaguePriority = new Dictionary<int, List<string>>
                    {
                        { 1, new List<string> { "EPL" } },
                        { 2, new List<string> { "ITA" } },
                        { 3, new List<string> { "GER" } }
                    }
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(1, "the match should be assigned");

        var assignment = result.Assignments.First();

        // With same skill level, coverage bonus + preconfigured pair bonus should prefer the configured pair
        assignment.Hdp.Should().Be("JohnDoe", "preconfigured HDP trader should be assigned");
        assignment.Ou.Should().Be("JaneSmith", "preconfigured OU trader should be assigned");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_PreconfiguredPair_SecondaryTier_AppliesTierBonus()
    {
        // Arrange - Alternative MJ match with preconfigured pair (Secondary tier)
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "AliceHdp", new[] { "HDP" }, "Member", false),
                CreateTraderShift("2", "BobOu", new[] { "OU" }, "Member", false),
                CreateTraderShift("3", "RandomHdp", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "RandomOu", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "ITA", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "AliceHdp",
                    Ou = "BobOu",
                    Tier = "Secondary",
                    LeaguePriority = new Dictionary<int, List<string>>
                    {
                        { 1, new List<string> { "ITA" } },
                        { 2, new List<string> { "GER" } },
                        { 3, new List<string> { "FRA" } }
                    }
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(1, "the match should be assigned");

        var assignment = result.Assignments.First();

        // Secondary tier bonus (1.3x) should make the preconfigured pair more attractive
        assignment.Hdp.Should().Be("AliceHdp", "preconfigured HDP trader should be assigned");
        assignment.Ou.Should().Be("BobOu", "preconfigured OU trader should be assigned");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_LeaguePriority_AppliesLeagueBonus()
    {
        // Arrange - Alternative MJ match with preconfigured pair having league priority
        // Match in EPL, pair has EPL as 1st priority (1.5x bonus)
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EPLExpertHdp", new[] { "HDP" }, "Member", false),
                CreateTraderShift("2", "EPLExpertOu", new[] { "OU" }, "Member", false),
                CreateTraderShift("3", "GeneralistHdp", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "GeneralistOu", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "EPLExpertHdp",
                    Ou = "EPLExpertOu",
                    Tier = "Primary",
                    LeaguePriority = new Dictionary<int, List<string>>
                    {
                        { 1, new List<string> { "EPL" } }, // EPL is 1st priority
                        { 2, new List<string> { "ITA" } },
                        { 3, new List<string> { "GER" } }
                    }
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(1, "the match should be assigned");

        var assignment = result.Assignments.First();

        // League priority bonus (1.5x for 1st priority) should favor the EPL experts
        assignment.Hdp.Should().Be("EPLExpertHdp", "EPL expert HDP trader should be assigned");
        assignment.Ou.Should().Be("EPLExpertOu", "EPL expert OU trader should be assigned");
    }

    [Test]
    public void Assign_WithAlternativeMJRule_CombinedBonuses_PreconfiguredPairWithPrioritySkill()
    {
        // Arrange - Alternative MJ match testing all bonuses combined:
        // - Coverage bonus (10,000)
        // - Tier bonus (Primary: 1.5x)
        // - League bonus (1st priority: 1.5x)
        // - Priority skill bonuses (Elite non-manager: 2.0x)
        // Use same level traders to ensure bonuses make a difference
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "PairEliteHdp", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "PairEliteOu", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "RandomHdp", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("4", "RandomOu", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    80000, "EPL", isUseAlternativeMJRule: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "PairEliteHdp",
                    Ou = "PairEliteOu",
                    Tier = "Primary", // 1.5x tier bonus
                    LeaguePriority = new Dictionary<int, List<string>>
                    {
                        { 1, new List<string> { "EPL" } }, // 1.5x league bonus for 1st priority
                        { 2, new List<string> { "ITA" } },
                        { 3, new List<string> { "GER" } }
                    }
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(1, "the match should be assigned");

        var assignment = result.Assignments.First();

        // Combined bonuses should favor the preconfigured pair when skill levels are equal
        // Coverage bonus + tier bonus + league bonus + priority skill bonuses
        assignment.Hdp.Should().Be("PairEliteHdp", "preconfigured pair should be assigned with all bonuses");
        assignment.Ou.Should().Be("PairEliteOu", "preconfigured pair should be assigned with all bonuses");
        assignment.HdpLevel.Should().Be("Elite");
        assignment.OuLevel.Should().Be("Elite");
    }

    // Helper methods
    private static TraderShift CreateTraderShift(
        string id,
        string name,
        string[] skills,
        string level,
        bool isManager,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null)
    {
        return new TraderShift
        {
            Id = id,
            Name = name,
            Start = start ?? DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            End = end ?? DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
            Skills = skills.ToList(),
            Level = level,
            IsManager = isManager,
            IgnoreLeague = new List<string>(),
            AssignedJobs = new List<AssignedJob>(),
            MaxBet = 1000000
        };
    }

    private static Match CreateMatch(
        string id,
        string matchNo,
        DateTimeOffset start,
        DateTimeOffset end,
        int maxBet,
        string? leagueCode = null,
        bool isUseAlternativeMJRule = false,
        string? leagueName = null)
    {
        var matchLeagueName = leagueName ?? leagueCode switch
        {
            "EPL" => "English Premier League",
            "ITA" => "Italian Serie A",
            "GER" => "German Bundesliga",
            "FRA" => "French Ligue 1",
            _ => leagueCode
        };

        return new Match
        {
            Id = id,
            MatchNo = matchNo,
            Start = start,
            End = end,
            MaxBet = maxBet,
            LeagueCode = leagueCode,
            LeagueName = matchLeagueName,
            BetType = "MJ",
            IsUseAlternativeMJRule = isUseAlternativeMJRule,
            IsAlreadyHandled = false
        };
    }
}
