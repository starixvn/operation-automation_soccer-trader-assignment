using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M1;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for Minor 1 match assignment with IgnoreLeague functionality
/// </summary>
[TestFixture]
public class MinorOneMatchAssignmentStrategyIgnoreLeagueTests
{
    private MinorOneMatchAssignmentStrategy _strategy = null!;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MinorOneMatchAssignmentStrategy();
    }

    [TearDown]
    public void TearDown()
    {
        _strategy = null!;
    }

    [Test]
    public void Assign_TraderIgnoresLeague_DoesNotAssignToThatLeague()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 2,
                    ignoreLeague: new List<string> { "EPL" }) // Trader ignores EPL
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: "EPL Championship") // EPL league
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(0, "trader ignores EPL league");
        response.Assignments.Should().HaveCount(1);
        response.Assignments[0].Hdp.Should().Be("N/A");
        response.Assignments[0].PairType.Should().Be("❌ UNASSIGNED");
    }

    [Test]
    public void Assign_TraderIgnoresLeague_CanStillWorkOtherLeagues()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 2,
                    ignoreLeague: new List<string> { "EPL" }) // Trader ignores EPL
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: "La Liga"), // Different league
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T14:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
                    45000,
                    leagueName: "EPL") // EPL league - should be ignored
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1, "only La Liga match should be assigned");
        response.Assignments.Should().HaveCount(2);

        // La Liga match should be assigned
        var laLigaAssignment = response.Assignments.First(a => a.Match == "100");
        laLigaAssignment.Hdp.Should().Be("TraderA");
        laLigaAssignment.Ou.Should().Be("TraderA");

        // EPL match should not be assigned
        var eplAssignment = response.Assignments.First(a => a.Match == "101");
        eplAssignment.Hdp.Should().Be("N/A");
        eplAssignment.PairType.Should().Be("❌ UNASSIGNED");
    }

    [Test]
    public void Assign_MultipleTraders_OnlyNonIgnoringTraderAssignedToLeague()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 2,
                    ignoreLeague: new List<string> { "EPL" }), // TraderA ignores EPL
                CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 2,
                    ignoreLeague: null) // TraderB works all leagues
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: "EPL Championship")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        response.Assignments.Should().HaveCount(1);
        response.Assignments[0].Hdp.Should().Be("TraderB", "only TraderB can work EPL");
        response.Assignments[0].Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_IgnoreLeagueWithPartialMatch_FiltersCorrectly()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 3,
                    ignoreLeague: new List<string> { "Premier" }) // Ignores leagues containing "Premier"
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: "English Premier League"), // Contains "Premier"
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T15:00:00Z"),
                    45000,
                    leagueName: "La Liga"), // Does not contain "Premier"
                CreateMatch("3", "102",
                    DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    40000,
                    leagueName: "Premier League Asia") // Contains "Premier"
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1, "only La Liga match should be assigned");

        var laLigaAssignment = response.Assignments.First(a => a.Match == "101");
        laLigaAssignment.Hdp.Should().Be("TraderA");

        var englishPremierAssignment = response.Assignments.First(a => a.Match == "100");
        englishPremierAssignment.Hdp.Should().Be("N/A");

        var premierAsiaAssignment = response.Assignments.First(a => a.Match == "102");
        premierAsiaAssignment.Hdp.Should().Be("N/A");
    }

    [Test]
    public void Assign_EmptyIgnoreLeagueList_AssignsToAllLeagues()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 2,
                    ignoreLeague: new List<string>()) // Empty list - no leagues ignored
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: "EPL"),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T14:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
                    45000,
                    leagueName: "La Liga")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(2, "trader can work all leagues");
        response.Assignments.Should().HaveCount(2);
        response.Assignments.Should().AllSatisfy(a => a.Hdp.Should().Be("TraderA"));
    }

    [Test]
    public void Assign_MatchWithNoLeagueName_CanBeAssignedEvenWithIgnoreList()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1,
                    ignoreLeague: new List<string> { "EPL" })
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: null) // No league name
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1, "match with no league name can be assigned");
        response.Assignments[0].Hdp.Should().Be("TraderA");
    }

    [Test]
    public void Assign_CaseInsensitiveLeagueMatching_FiltersCorrectly()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 2,
                    ignoreLeague: new List<string> { "epl" }) // Lowercase
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    50000,
                    leagueName: "EPL Championship"), // Uppercase EPL
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T14:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
                    45000,
                    leagueName: "Epl Asia") // Mixed case
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(0, "both matches contain 'epl' (case-insensitive)");
        response.Assignments.Should().AllSatisfy(a => a.Hdp.Should().Be("N/A"));
    }
}
