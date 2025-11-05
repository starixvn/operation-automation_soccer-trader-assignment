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
    public void Assign_WithITAHigherMaxBetThanEPL_PrioritizesITA()
    {
        // Arrange - ITA has higher MaxBet than EPL
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
                    120000, "ITA")
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

        itaAssignment.HdpLevel.Should().Be("Elite", "ITA should get Elite traders when it has higher MaxBet");
        itaAssignment.OuLevel.Should().Be("Elite", "ITA should get Elite traders when it has higher MaxBet");
        eplAssignment.HdpLevel.Should().Be("Member", "EPL should get Member traders when ITA has higher MaxBet");
        eplAssignment.OuLevel.Should().Be("Member", "EPL should get Member traders when ITA has higher MaxBet");
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
            AssignedJobs = new List<AssignedJob>()
        };
    }

    private static Match CreateMatch(
        string id,
        string matchNo,
        DateTimeOffset start,
        DateTimeOffset end,
        int maxBet,
        string? leagueCode = null,
        bool isAlreadyHandled = false)
    {
        return new Match
        {
            Id = id,
            MatchNo = matchNo,
            Start = start,
            End = end,
            MaxBet = maxBet,
            LeagueCode = leagueCode,
            IsAlreadyHandled = isAlreadyHandled
        };
    }
}
