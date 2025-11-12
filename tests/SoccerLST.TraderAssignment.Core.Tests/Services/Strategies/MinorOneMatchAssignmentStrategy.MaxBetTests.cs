using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for MaxBet constraint in MinorOneMatchAssignmentStrategy
/// Ensures that traders can only be assigned to matches where trader.MaxBet >= match.MaxBet
/// </summary>
[TestFixture]
public class MinorOneMatchAssignmentStrategyMaxBetTests
{
    private MinorOneMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MinorOneMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_TraderWithSufficientMaxBet_ShouldBeAssigned()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "HighLimitTrader", ["HDP", "OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m1Capacity: 1,
                maxBet: 60000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                50000, "EPL") // M1 match (40k-70k range)
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
        response.Assignments[0].Hdp.Should().Be("HighLimitTrader");
    }

    [Test]
    public void Assign_TraderWithInsufficientMaxBet_ShouldNotBeAssigned()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "LowLimitTrader", ["HDP", "OU"], "Member", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m1Capacity: 1,
                maxBet: 35000) // Below match MaxBet
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                50000, "EPL") // M1 match
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
    }

    [Test]
    public void Assign_MultipleTraders_OnlyEligibleOnesAssigned()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "HighLimitTrader", ["HDP", "OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m1Capacity: 2,
                maxBet: 65000),
            CreateTraderShift("2", "LowLimitTrader", ["HDP", "OU"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m1Capacity: 2,
                maxBet: 45000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                60000, "EPL"), // Only HighLimitTrader eligible
            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T18:00:00Z"),
                42000, "SPA") // Both traders eligible
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
        foreach (var assignment in response.Assignments)
        {
            var match = matches.First(m => m.Id == assignment.RowNumber);
            var trader = traders.First(t => t.Name == assignment.Hdp);

            trader.MaxBet.Should().BeGreaterThanOrEqualTo(match.MaxBet,
                $"Trader {trader.Name} (MaxBet: {trader.MaxBet}) should handle match {match.Id} (MaxBet: {match.MaxBet})");
        }
    }

    [Test]
    public void Assign_TraderWithUnlimitedMaxBet_CanHandleAnyM1Match()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "UnlimitedTrader", ["HDP", "OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m1Capacity: 1,
                maxBet: 1000000) // Unlimited
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                69000, "EPL") // High M1 match
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
    }
}
