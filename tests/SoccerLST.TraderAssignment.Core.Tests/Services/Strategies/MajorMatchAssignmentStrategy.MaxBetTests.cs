using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.MJ;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for MaxBet constraint in MajorMatchAssignmentStrategy
/// Ensures that traders can only be assigned to matches where trader.MaxBet >= match.MaxBet
/// </summary>
[TestFixture]
public class MajorMatchAssignmentStrategyMaxBetTests
{
    private MajorMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_TraderWithSufficientMaxBet_ShouldBeAssigned()
    {
        // Arrange - MJ requires 2 separate traders
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "HighLimitTraderA", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 150000),
            CreateTraderShift("2", "HighLimitTraderB", ["OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 150000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL") // Match MaxBet = 120k
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        response.Assignments.Should().HaveCount(1);
        response.Assignments[0].Hdp.Should().Be("HighLimitTraderA");
        response.Assignments[0].Ou.Should().Be("HighLimitTraderB");
    }

    [Test]
    public void Assign_TraderWithInsufficientMaxBet_ShouldNotBeAssigned()
    {
        // Arrange - MJ requires 2 separate traders
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "LowLimitTraderA", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 50000), // Trader MaxBet = 50k
            CreateTraderShift("2", "LowLimitTraderB", ["OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 50000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL") // Match MaxBet = 120k (exceeds trader limit)
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(0); // No assignments due to MaxBet constraint
        response.Assignments.Should().HaveCount(1); // Match still appears as unassigned
        response.Assignments[0].Hdp.Should().Be("N/A");
        response.Assignments[0].Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_MultipleTraders_OnlyEligibleOnesAssigned()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "HighLimitTrader", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 150000),
            CreateTraderShift("2", "MediumLimitTrader", ["OU"], "Member", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 80000),
            CreateTraderShift("3", "LowLimitTraderA", ["HDP"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 50000),
            CreateTraderShift("4", "LowLimitTraderB", ["OU"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 50000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL") // Only HighLimitTrader (150k) can handle this
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(0); // Can't assign - need both HDP and OU, but only HDP trader qualifies
        response.Assignments.Should().HaveCount(1); // Match still appears as unassigned
        response.Assignments[0].Hdp.Should().Be("N/A");
        response.Assignments[0].Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_MultipleMatches_DifferentMaxBets_AssignsBasedOnLimits()
    {
        // Arrange - MJ requires 2 separate traders
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "HighLimitTraderA", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 150000),
            CreateTraderShift("2", "HighLimitTraderB", ["OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 150000),
            CreateTraderShift("3", "MediumLimitTraderA", ["HDP"], "Member", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 80000),
            CreateTraderShift("4", "MediumLimitTraderB", ["OU"], "Member", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 80000),
            CreateTraderShift("5", "LowLimitTraderA", ["HDP"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 50000),
            CreateTraderShift("6", "LowLimitTraderB", ["OU"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 50000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL"), // Only HighLimitTrader eligible
            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-01-15T17:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T19:00:00Z"),
                75000, "SPA"), // HighLimit and MediumLimit eligible
            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-01-15T20:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T22:00:00Z"),
                40000, "ITA") // All traders eligible
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().BeGreaterThan(0);

        // Verify all assignments respect MaxBet constraint
        foreach (var assignment in response.Assignments.Where(a => a.Hdp != "N/A"))
        {
            var match = matches.First(m => m.Id == assignment.RowNumber);
            var hdpTrader = traders.First(t => t.Name == assignment.Hdp);
            var ouTrader = traders.First(t => t.Name == assignment.Ou);

            hdpTrader.MaxBet.Should().BeGreaterThanOrEqualTo(match.MaxBet,
                $"HDP trader {hdpTrader.Name} (MaxBet: {hdpTrader.MaxBet}) should handle match {match.Id} (MaxBet: {match.MaxBet})");
            ouTrader.MaxBet.Should().BeGreaterThanOrEqualTo(match.MaxBet,
                $"OU trader {ouTrader.Name} (MaxBet: {ouTrader.MaxBet}) should handle match {match.Id} (MaxBet: {match.MaxBet})");
        }
    }

    [Test]
    public void Assign_TraderWithUnlimitedMaxBet_CanHandleAnyMatch()
    {
        // Arrange - MJ requires 2 separate traders
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "UnlimitedTraderA", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 1000000), // Default unlimited
            CreateTraderShift("2", "UnlimitedTraderB", ["OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                500000, "EPL") // Very high MaxBet
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        response.Assignments.Should().HaveCount(1);
        response.Assignments[0].Hdp.Should().Be("UnlimitedTraderA");
        response.Assignments[0].Ou.Should().Be("UnlimitedTraderB");
    }

    [Test]
    public void Assign_TraderWithExactMatchMaxBet_ShouldBeAssigned()
    {
        // Arrange - MJ requires 2 separate traders
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "ExactLimitTraderA", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 100000),
            CreateTraderShift("2", "ExactLimitTraderB", ["OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 100000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                100000, "EPL") // Exact match
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        response.Assignments.Should().HaveCount(1);
        response.Assignments[0].Hdp.Should().Be("ExactLimitTraderA");
        response.Assignments[0].Ou.Should().Be("ExactLimitTraderB");
    }

    [Test]
    public void Assign_TraderMaxBetOneBelow_ShouldNotBeAssigned()
    {
        // Arrange - MJ requires 2 separate traders
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "AlmostEnoughTraderA", ["HDP"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 99999),
            CreateTraderShift("2", "AlmostEnoughTraderB", ["OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                maxBet: 99999)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                100000, "EPL") // Just 1 more than trader limit
        };

        var request = new AssignmentRequest
        {
            TraderShifts = traders,
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(0);
        response.Assignments.Should().HaveCount(1); // Match still appears as unassigned
        response.Assignments[0].Hdp.Should().Be("N/A");
        response.Assignments[0].Ou.Should().Be("N/A");
    }
}
