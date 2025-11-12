using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for MaxBet constraint in MinorTwoMatchAssignmentStrategy
/// Ensures that traders can only be assigned to matches where trader.MaxBet >= match.MaxBet
/// </summary>
[TestFixture]
public class MinorTwoMatchAssignmentStrategyMaxBetTests
{
    private MinorTwoMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MinorTwoMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_TraderWithSufficientMaxBet_ShouldBeAssigned()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "MediumLimitTrader", ["HDP", "OU"], "Member", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m2Capacity: 1,
                maxBet: 35000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                30000, "EPL") // M2 match (20k-40k range)
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
        response.Assignments[0].Hdp.Should().Be("MediumLimitTrader");
    }

    [Test]
    public void Assign_TraderWithInsufficientMaxBet_ShouldNotBeAssigned()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "VeryLowLimitTrader", ["HDP", "OU"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m2Capacity: 1,
                maxBet: 25000) // Below match MaxBet
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                35000, "EPL") // M2 match
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
                m2Capacity: 3,
                maxBet: 38000),
            CreateTraderShift("2", "LowLimitTrader", ["HDP", "OU"], "First", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m2Capacity: 3,
                maxBet: 22000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                35000, "EPL"), // Only HighLimitTrader eligible
            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-01-15T14:30:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:30:00Z"),
                38000, "SPA"), // Only HighLimitTrader eligible (exact match)
            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-01-15T15:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T17:00:00Z"),
                21000, "ITA") // Both traders eligible
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

        // Verify all assignments respect MaxBet constraint (filter out unassigned matches)
        foreach (var assignment in response.Assignments.Where(a => a.Hdp != "N/A"))
        {
            var match = matches.First(m => m.Id == assignment.RowNumber);
            var trader = traders.First(t => t.Name == assignment.Hdp);

            trader.MaxBet.Should().BeGreaterThanOrEqualTo(match.MaxBet,
                $"Trader {trader.Name} (MaxBet: {trader.MaxBet}) should handle match {match.Id} (MaxBet: {match.MaxBet})");
        }
    }

    [Test]
    public void Assign_TraderWithUnlimitedMaxBet_CanHandleAnyM2Match()
    {
        // Arrange
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "UnlimitedTrader", ["HDP", "OU"], "Elite", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m2Capacity: 1,
                maxBet: 1000000) // Unlimited
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                39000, "EPL") // High M2 match
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

    [Test]
    public void Assign_M2Capacity_WithMaxBetConstraint_BothApply()
    {
        // Arrange: Trader has capacity for 2 matches but MaxBet limits eligibility
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "LimitedTrader", ["HDP", "OU"], "Member", false,
                start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
                end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
                m2Capacity: 2,
                maxBet: 30000) // Can handle 2 concurrent but limited by MaxBet
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                25000, "EPL"), // Within limit
            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-01-15T14:30:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:30:00Z"),
                35000, "SPA"), // Exceeds limit - should not be assigned
            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-01-15T15:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T17:00:00Z"),
                28000, "ITA") // Within limit
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

        // Should assign up to 2 matches that are within MaxBet limit
        response.AssignedMatches.Should().BeLessThanOrEqualTo(2);

        // Verify Match 102 (35k) is NOT assigned (appears as unassigned)
        var match102 = response.Assignments.FirstOrDefault(a => a.RowNumber == "102");
        if (match102 != null)
        {
            match102.Hdp.Should().Be("N/A", "Match 102 exceeds trader MaxBet");
        }

        // Verify all assigned matches respect MaxBet constraint
        foreach (var assignment in response.Assignments.Where(a => a.Hdp != "N/A"))
        {
            var match = matches.First(m => m.Id == assignment.RowNumber);
            var trader = traders.First(t => t.Name == assignment.Hdp);

            trader.MaxBet.Should().BeGreaterThanOrEqualTo(match.MaxBet);
        }
    }
}
