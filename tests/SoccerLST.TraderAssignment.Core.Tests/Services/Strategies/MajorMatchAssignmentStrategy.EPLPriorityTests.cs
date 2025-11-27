using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public class MajorMatchAssignmentStrategyEPLPriorityTests
{
    private MajorMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_WithEPLAndGERSameMaxBet_PrioritizesEPL()
    {
        // Arrange - EPL and GER matches at same time with same MaxBet
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "EPL"),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "GER")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var eplAssignment = result.Assignments.First(a => a.League == "EPL");
        var gerAssignment = result.Assignments.First(a => a.League == "GER");

        eplAssignment.HdpLevel.Should().Be("Elite", "EPL should get Elite traders when MaxBet is equal");
        eplAssignment.OuLevel.Should().Be("Elite", "EPL should get Elite traders when MaxBet is equal");
        gerAssignment.HdpLevel.Should().Be("Member", "GER should get remaining Member traders");
        gerAssignment.OuLevel.Should().Be("Member", "GER should get remaining Member traders");
    }

    [Test]
    public void Assign_WithEPLAndITASameMaxBet_PrioritizesEPL()
    {
        // Arrange - EPL and ITA matches at same time with same MaxBet
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "EPL"),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "ITA")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var eplAssignment = result.Assignments.First(a => a.League == "EPL");
        var itaAssignment = result.Assignments.First(a => a.League == "ITA");

        eplAssignment.HdpLevel.Should().Be("Elite", "EPL should get Elite traders when MaxBet is equal");
        eplAssignment.OuLevel.Should().Be("Elite", "EPL should get Elite traders when MaxBet is equal");
        itaAssignment.HdpLevel.Should().Be("Member", "ITA should get remaining Member traders");
        itaAssignment.OuLevel.Should().Be("Member", "ITA should get remaining Member traders");
    }

    [Test]
    public void Assign_WithEPLVsPreconfiguredITASameMaxBet_PrioritizesEPL()
    {
        // Arrange - EPL vs ITA with preconfigured pair for ITA, same MaxBet
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "EPL"),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "ITA")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "MemberHDP",
                    Ou = "MemberOU",
                    Tier = "primary",
                    LeaguePriority = new Dictionary<int, List<string>>
                    {
                        { 1, new List<string> { "ITA" } }
                    }
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var eplAssignment = result.Assignments.First(a => a.League == "EPL");
        var itaAssignment = result.Assignments.First(a => a.League == "ITA");

        eplAssignment.HdpLevel.Should().Be("Elite", "EPL should get Elite traders even when ITA has preconfigured pair");
        eplAssignment.OuLevel.Should().Be("Elite", "EPL should get Elite traders even when ITA has preconfigured pair");
    }

    [Test]
    public void Assign_WithMultipleMatchesSameMaxBet_EPLGetsEliteTraders()
    {
        // Arrange - Multiple matches at same time with same MaxBet
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false),
                CreateTraderShift("5", "FirstHDP", new[] { "HDP" }, "First", false),
                CreateTraderShift("6", "FirstOU", new[] { "OU" }, "First", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "EPL"),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "GER"),
                CreateMatch("3", "102",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "FRA")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3);

        var eplAssignment = result.Assignments.First(a => a.League == "EPL");

        eplAssignment.HdpLevel.Should().Be("Elite", "EPL should get the highest level (Elite) traders");
        eplAssignment.OuLevel.Should().Be("Elite", "EPL should get the highest level (Elite) traders");
    }

    [Test]
    public void Assign_WithEPLVsNonEPLDifferentTimes_MaxBetDominates()
    {
        // Arrange - EPL and GER at different times, GER has higher MaxBet
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    100000, "EPL"),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T15:00:00Z"),
                    150000, "GER")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var gerAssignment = result.Assignments.First(a => a.League == "GER");

        gerAssignment.HdpLevel.Should().Be("Elite", "Higher MaxBet match should get Elite traders regardless of league");
        gerAssignment.OuLevel.Should().Be("Elite", "Higher MaxBet match should get Elite traders regardless of league");
    }

    [Test]
    public void Assign_TwoConcurrentMatches_EPLAndITA_EPLShouldGetEliteTrader()
    {
        // Arrange - Simplified scenario with 2 concurrent matches
        // 1 EPL + 1 ITA match, both 120k MaxBet at same time
        // 4 traders: 1 Elite HDP with EPL restriction, 1 Elite OU (normal), 2 Members
        // Expected: Normal Elite OU + one of the other traders should go to EPL
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                // Elite HDP with EPL restriction - can only work ITA
                CreateTraderShift("1", "EliteRestrictedHDP", new[] { "HDP" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-10-26T20:00:00Z"),
                    end: DateTimeOffset.Parse("2025-10-27T04:00:00Z"),
                    ignoreLeague: new List<string> { "English Premier League" }),

                // Normal Elite OU - should be assigned to EPL
                CreateTraderShift("2", "EliteNormalOU", new[] { "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-10-26T20:00:00Z"),
                    end: DateTimeOffset.Parse("2025-10-27T04:00:00Z")),

                // Member traders - one should pair with EliteNormalOU for EPL
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false,
                    start: DateTimeOffset.Parse("2025-10-26T20:00:00Z"),
                    end: DateTimeOffset.Parse("2025-10-27T04:00:00Z")),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false,
                    start: DateTimeOffset.Parse("2025-10-26T20:00:00Z"),
                    end: DateTimeOffset.Parse("2025-10-27T04:00:00Z"))
            },
            Matches = new List<Match>
            {
                // EPL match
                CreateMatch("1", "1",
                    DateTimeOffset.Parse("2025-10-26T22:00:00Z"),
                    DateTimeOffset.Parse("2025-10-27T00:00:00Z"),
                    120000, "EPL"),

                // ITA match
                CreateMatch("2", "2",
                    DateTimeOffset.Parse("2025-10-26T22:00:00Z"),
                    DateTimeOffset.Parse("2025-10-27T00:00:00Z"),
                    120000, "ITA")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue($"solver should succeed, but got status: {result.Status}");
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        var eplAssignment = result.Assignments.First(a => a.League == "EPL");
        var itaAssignment = result.Assignments.First(a => a.League == "ITA");

        // EPL should get at least one Elite trader (EliteNormalOU)
        // Expected: MemberHDP + EliteNormalOU (because EPL has Elite boost)
        eplAssignment.OuLevel.Should().Be("Elite", "EPL match should have Elite OU trader");
        eplAssignment.Ou.Should().Be("EliteNormalOU", "EPL should get non-restricted Elite OU");

        // ITA should get the Elite HDP with EPL restriction + Member OU
        itaAssignment.HdpLevel.Should().Be("Elite", "ITA match should have Elite HDP trader (restricted from EPL)");
        itaAssignment.Hdp.Should().Be("EliteRestrictedHDP", "ITA should get EPL-restricted Elite HDP");
    }

    // Helper methods
    private static TraderShift CreateTraderShift(
        string id,
        string name,
        string[] skills,
        string level,
        bool isManager,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        List<string>? ignoreLeague = null)
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
            IgnoreLeague = ignoreLeague ?? new List<string>(),
            AssignedJobs = new List<AssignedJob>(),
            MaxBet = 1000000 // High enough to handle any test match
        };
    }

    private static Match CreateMatch(
        string id,
        string matchNo,
        DateTimeOffset start,
        DateTimeOffset end,
        int maxBet,
        string? leagueCode = null,
        bool isAlreadyHandled = false,
        string? leagueName = null)
    {
        // If leagueName not provided, derive from leagueCode
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
            IsAlreadyHandled = isAlreadyHandled
        };
    }
}
