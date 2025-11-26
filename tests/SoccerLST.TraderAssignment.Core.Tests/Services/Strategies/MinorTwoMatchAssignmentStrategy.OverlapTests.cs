using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M2;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for overlapping match scenarios in MinorTwoMatchAssignmentStrategy
/// Ensures optimizer correctly prioritizes concurrent same-start-time matches over overlapping different-start-time matches
/// </summary>
[TestFixture]
public class MinorTwoMatchAssignmentStrategyOverlapTests
{
    private MinorTwoMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MinorTwoMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_ShouldPreferTwoConcurrentMatchesOverOneOverlappingMatch()
    {
        // Arrange - Trader with M2Capacity=2 can handle 2 concurrent matches
        // Should choose Match 107 + Match 108 (both 04:30-06:30, total 60k MaxBet)
        // Over Match 106 (04:00-06:00, 30k MaxBet) which blocks both 107 and 108
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "CLGSern", [], "Second", false,
                start: DateTimeOffset.Parse("2025-11-10T00:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T09:00:00Z"),
                m2Capacity: 2,
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            // Match 106: overlaps with 107 and 108, different start time
            CreateMatch("106", "106",
                DateTimeOffset.Parse("2025-11-10T04:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:00:00Z"),
                30000, "TEST"),

            // Match 107 and 108: same start time, can both be assigned with M2Capacity=2
            CreateMatch("107", "107",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                30000, "TEST"),

            CreateMatch("108", "108",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                30000, "TEST")
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
        response.AssignedMatches.Should().Be(2, "should assign both concurrent matches 107 and 108");

        var assignedMatchNos = response.Assignments
            .Where(a => a.Hdp != "N/A")
            .Select(a => a.Match)
            .OrderBy(m => m)
            .ToList();

        assignedMatchNos.Should().Contain("107", "Match 107 should be assigned");
        assignedMatchNos.Should().Contain("108", "Match 108 should be assigned");
        assignedMatchNos.Should().NotContain("106", "Match 106 should NOT be assigned as it blocks 107 and 108");

        // Verify all assignments go to CLGSern
        var assignments = response.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignments.Should().AllSatisfy(a => a.Hdp.Should().Be("CLGSern"));

        // Verify scoring: 2 matches × 30k = 60k total MaxBet > 1 match × 30k
        var totalMaxBet = assignments.Sum(a => a.MaxBet);
        totalMaxBet.Should().Be(60000, "total MaxBet of assigned matches should be 60k (2×30k)");
    }

    [Test]
    public void Assign_ThreeConcurrentMatches_ShouldRespectM2Capacity()
    {
        // Arrange - Trader with M2Capacity=2 can only handle 2 of 3 concurrent matches
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "Trader1", [], "Elite", false,
                start: DateTimeOffset.Parse("2025-11-10T00:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T09:00:00Z"),
                m2Capacity: 2,
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            // All 3 matches at same start time
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                20000, "TEST"),

            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                30000, "TEST"), // Higher MaxBet

            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                25000, "TEST")
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
        response.AssignedMatches.Should().Be(2, "M2Capacity=2 limits to 2 concurrent matches");

        var assignedMatchNos = response.Assignments
            .Where(a => a.Hdp != "N/A")
            .Select(a => a.Match)
            .OrderBy(m => m)
            .ToList();

        // Should prefer higher MaxBet matches (102: 30k, 103: 25k over 101: 20k)
        assignedMatchNos.Should().Contain("102", "highest MaxBet should be assigned");
        assignedMatchNos.Should().HaveCount(2);
    }

    [Test]
    public void Assign_OverlappingDifferentStartTimes_CanOnlyAssignOne()
    {
        // Arrange - Two matches with different start times but overlapping
        // Constraint: trader can only be assigned to ONE
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "Trader1", [], "Member", false,
                start: DateTimeOffset.Parse("2025-11-10T00:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T09:00:00Z"),
                m2Capacity: 5, // High capacity doesn't help with different start times
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-11-10T03:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T05:00:00Z"),
                20000, "TEST"),

            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-11-10T04:00:00Z"), // Overlaps with 101
                DateTimeOffset.Parse("2025-11-10T06:00:00Z"),
                30000, "TEST") // Higher MaxBet
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
        response.AssignedMatches.Should().Be(1, "can only assign one overlapping match with different start time");

        var assignedMatch = response.Assignments.First(a => a.Hdp != "N/A");
        assignedMatch.Match.Should().Be("102", "should prefer higher MaxBet match");
        assignedMatch.MaxBet.Should().Be(30000);
    }

    [Test]
    public void Assign_CombinationOfConcurrentAndOverlapping_OptimizesCorrectly()
    {
        // Arrange - Complex scenario:
        // - Match 101, 102, 103 at 03:00-05:00 (3 concurrent, but M2Capacity=2)
        // - Match 104, 105 at 04:30-06:30 (2 concurrent, overlap with 101-103)
        // Optimal: Choose 104 + 105 (both 30k) = 60k total
        // Over: 101 + 102 (20k + 25k) = 45k total
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "Trader1", [], "First", false,
                start: DateTimeOffset.Parse("2025-11-10T00:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T09:00:00Z"),
                m2Capacity: 2,
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            // Group 1: 03:00-05:00 (3 concurrent)
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-11-10T03:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T05:00:00Z"),
                20000, "TEST"),

            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-11-10T03:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T05:00:00Z"),
                25000, "TEST"),

            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-11-10T03:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T05:00:00Z"),
                15000, "TEST"),

            // Group 2: 04:30-06:30 (2 concurrent, overlaps with Group 1)
            CreateMatch("104", "104",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                30000, "TEST"),

            CreateMatch("105", "105",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                30000, "TEST")
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
        response.AssignedMatches.Should().Be(2);

        var assignedMatchNos = response.Assignments
            .Where(a => a.Hdp != "N/A")
            .Select(a => a.Match)
            .OrderBy(m => m)
            .ToList();

        // Should choose 104 + 105 (both 30k MaxBet at same start time)
        // Over any combination from 101, 102, 103 (which have different start times from 104/105)
        assignedMatchNos.Should().Contain("104");
        assignedMatchNos.Should().Contain("105");

        var totalMaxBet = response.Assignments.Where(a => a.Hdp != "N/A").Sum(a => a.MaxBet);
        totalMaxBet.Should().Be(60000, "optimal choice is 104 + 105 = 60k total");
    }

    [Test]
    public void Assign_WithAssignedJobs_ReducesAvailableCapacity()
    {
        // Arrange - Trader has M2Capacity=2 but 1 existing job at same start time
        // Can only assign 1 more match at that time
        var existingJob = CreateAssignedJob("M2",
            DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
            DateTimeOffset.Parse("2025-11-10T06:30:00Z"));

        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "Trader1", [], "Member", false,
                start: DateTimeOffset.Parse("2025-11-10T00:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T09:00:00Z"),
                m2Capacity: 2,
                assignedJobs: new List<AssignedJob> { existingJob },
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                25000, "TEST"),

            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                30000, "TEST"), // Higher MaxBet

            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-11-10T04:30:00Z"),
                DateTimeOffset.Parse("2025-11-10T06:30:00Z"),
                20000, "TEST")
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
        response.AssignedMatches.Should().Be(1, "M2Capacity=2 - 1 existing job = 1 available slot");

        var assignedMatch = response.Assignments.First(a => a.Hdp != "N/A");
        assignedMatch.Match.Should().Be("102", "should assign highest MaxBet match");
        assignedMatch.MaxBet.Should().Be(30000);
    }

    [Test]
    public void Assign_PICThirdLevelTrader_TwoConcurrent30kMatches_ShouldAssignBoth()
    {
        // Arrange - One PIC Third level trader with M2Capacity=2
        // Two concurrent 30k MaxBet matches at the same time
        // Expected: Both matches should be assigned to the same trader
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "TestTraderPIC", [], "Third", true, // isManager = true for PIC
                start: DateTimeOffset.Parse("2025-11-10T10:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T16:00:00Z"),
                m2Capacity: 2,
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "1",
                DateTimeOffset.Parse("2025-11-10T12:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T14:00:00Z"),
                30000, "TEST1"),

            CreateMatch("102", "2",
                DateTimeOffset.Parse("2025-11-10T12:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T14:00:00Z"),
                30000, "TEST2")
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
        response.AssignedMatches.Should().Be(2, "both concurrent matches should be assigned");

        var assignments = response.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignments.Should().HaveCount(2);

        // Both should be assigned to TestTraderPIC
        assignments.Should().AllSatisfy(a =>
        {
            a.Hdp.Should().Be("TestTraderPIC");
            a.Ou.Should().Be("TestTraderPIC");
            a.PairType.Should().Be("Single Trader (M2)");
        });

        // Verify match numbers
        var assignedMatchNos = assignments.Select(a => a.Match).OrderBy(m => m).ToList();
        assignedMatchNos.Should().Equal("1", "2");

        // Verify both matches have 30k MaxBet
        assignments.Should().AllSatisfy(a => a.MaxBet.Should().Be(30000));

        // Verify total MaxBet is 60k (2 × 30k)
        var totalMaxBet = assignments.Sum(a => a.MaxBet);
        totalMaxBet.Should().Be(60000);
    }

    [Test]
    public void Assign_EliteAndPICThird_TwoConcurrentMatches_EachTraderGetsOne()
    {
        // Arrange - Two traders: Elite and PIC Third level
        // Two concurrent 30k MaxBet matches at the same time
        // Expected: Each trader should get one match (Elite has higher priority)
        var traders = new List<TraderShift>
        {
            CreateTraderShift("1", "EliteTrader", [], "Elite", false,
                start: DateTimeOffset.Parse("2025-11-10T10:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T16:00:00Z"),
                m2Capacity: 1,
                maxBet: 1000000),

            CreateTraderShift("2", "PICThirdTrader", [], "Third", true, // isManager = true for PIC
                start: DateTimeOffset.Parse("2025-11-10T10:00:00Z"),
                end: DateTimeOffset.Parse("2025-11-10T16:00:00Z"),
                m2Capacity: 1,
                maxBet: 1000000)
        };

        var matches = new List<Match>
        {
            CreateMatch("101", "1",
                DateTimeOffset.Parse("2025-11-10T12:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T14:00:00Z"),
                30000, "TEST1"),

            CreateMatch("102", "2",
                DateTimeOffset.Parse("2025-11-10T12:00:00Z"),
                DateTimeOffset.Parse("2025-11-10T14:00:00Z"),
                30000, "TEST2")
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
        response.AssignedMatches.Should().Be(2, "both matches should be assigned");

        var assignments = response.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignments.Should().HaveCount(2);

        // Each trader should get one match
        var traderNames = assignments.Select(a => a.Hdp).Distinct().OrderBy(n => n).ToList();
        traderNames.Should().HaveCount(2, "both traders should be used");
        traderNames.Should().Contain("EliteTrader");
        traderNames.Should().Contain("PICThirdTrader");

        // Both should use Single Trader (M2) pattern
        assignments.Should().AllSatisfy(a =>
        {
            a.Hdp.Should().Be(a.Ou, "HDP and OU should be the same trader");
            a.PairType.Should().Be("Single Trader (M2)");
        });

        // Verify both matches have 30k MaxBet
        assignments.Should().AllSatisfy(a => a.MaxBet.Should().Be(30000));

        // Verify total MaxBet is 60k (2 × 30k)
        var totalMaxBet = assignments.Sum(a => a.MaxBet);
        totalMaxBet.Should().Be(60000);
    }
}
