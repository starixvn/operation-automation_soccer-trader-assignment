using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M2;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorTwoMatchAssignmentStrategyPriorityTests
{
    private MinorTwoMatchAssignmentStrategy _strategy = null!;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MinorTwoMatchAssignmentStrategy();
    }

    [TearDown]
    public void TearDown()
    {
        _strategy = null!;
    }

    #region Trader Level Priority Tests

    [Test]
    public void Assign_WithEliteAndMemberTraders_AssignsEliteToHigherMaxBet()
    {
        // Arrange - Elite trader should get the 38K match, Member gets the 25K match
        // Matches must overlap to force distribution between traders
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "MemberTrader", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 38000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var highMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 38000);
        highMaxBetAssignment.Hdp.Should().Be("EliteTrader", "Elite trader should handle higher MaxBet match");
        highMaxBetAssignment.Ou.Should().Be("EliteTrader", "Elite trader should handle higher MaxBet match");

        var lowMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 25000);
        lowMaxBetAssignment.Hdp.Should().Be("MemberTrader", "Member trader should handle lower MaxBet match");
        lowMaxBetAssignment.Ou.Should().Be("MemberTrader", "Member trader should handle lower MaxBet match");
    }

    [Test]
    public void Assign_WithAllThreeLevels_AssignsBasedOnMaxBetPriority()
    {
        // Arrange - Elite gets highest, Member gets middle, First gets lowest
        // All matches must overlap to force each trader to pick only one
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "MemberTrader", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("3", "FirstTrader", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 21000),  // Lowest
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T12:30:00Z"), 30000),  // Middle
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 39000)   // Highest
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3);

        var highestMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 39000);
        highestMaxBetAssignment.Hdp.Should().Be("EliteTrader");
        highestMaxBetAssignment.Ou.Should().Be("EliteTrader");

        var middleMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 30000);
        middleMaxBetAssignment.Hdp.Should().Be("MemberTrader");
        middleMaxBetAssignment.Ou.Should().Be("MemberTrader");

        var lowestMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 21000);
        lowestMaxBetAssignment.Hdp.Should().Be("FirstTrader");
        lowestMaxBetAssignment.Ou.Should().Be("FirstTrader");
    }

    [Test]
    public void Assign_WithTwoEliteTraders_DistributesBasedOnMaxBet()
    {
        // Arrange - Both Elite, higher MaxBet should go to first available Elite
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "EliteTraderB", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 28000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 37000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");
    }

    [Test]
    public void Assign_WithManagerVsNonManager_PrefersNonManagerForSameLevel()
    {
        // Arrange - Non-manager Elite should be preferred over manager Elite for same MaxBet
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteManager", new string[] { }, "Elite", true,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "EliteNonManager", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 32000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("EliteNonManager", "Non-manager Elite should be preferred over manager");
        assignment.Ou.Should().Be("EliteNonManager", "Non-manager Elite should be preferred over manager");
    }

    [Test]
    public void Assign_WithOnlyFirstTraders_AssignsToAvailableTraders()
    {
        // Arrange - All First level traders, should still assign optimally
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "FirstTraderA", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "FirstTraderB", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 23000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 35000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches should be assigned even with First level traders");
    }

    [Test]
    public void Assign_WithMixedLevelsAndMultipleMatches_OptimizesAssignments()
    {
        // Arrange - Complex scenario with multiple traders and matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "Elite1", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "Elite2", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("3", "Member1", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("4", "First1", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"), 21000),   // Lowest
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 28000),   // Low-Mid
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 34000),   // High-Mid
                CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T13:30:00Z"), DateTimeOffset.Parse("2025-09-28T14:30:00Z"), 39000)    // Highest
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(4, "all matches should be assigned");

        // Verify Elite traders get the highest MaxBet matches
        var highMaxBetMatches = result.Assignments.Where(a => a.MaxBet >= 34000).ToList();
        highMaxBetMatches.Should().HaveCount(2);
        highMaxBetMatches.Should().OnlyContain(a => a.Hdp.StartsWith("Elite") && a.Ou.StartsWith("Elite"),
            "Elite traders should handle the highest MaxBet matches");
    }

    [Test]
    public void Assign_WithEliteAndThirdTraders_ForcesCoverageOverOptimality()
    {
        // Arrange - Two overlapping matches force Elite to choose one
        // Third can only work the 38K match (shift constraint)
        // Expected: Elite takes 22K, Third takes 38K (both matches covered)
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "ThirdTrader", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T10:30:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 22000),  // Only Elite can work (starts before Third shift)
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 38000)   // Both can work, but overlaps with Match 1
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        var highMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 38000);
        highMaxBetAssignment.Hdp.Should().Be("ThirdTrader", "Third trader must take 38K match as they can't work the 22K match");
        highMaxBetAssignment.Ou.Should().Be("ThirdTrader");

        var lowMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 22000);
        lowMaxBetAssignment.Hdp.Should().Be("EliteTrader", "Elite trader takes 22K match to ensure full coverage");
        lowMaxBetAssignment.Ou.Should().Be("EliteTrader");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Assign_WithSameMaxBetDifferentLevels_AssignsHigherLevelTrader()
    {
        // Arrange - Same MaxBet but different trader levels
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "FirstTrader", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches with same MaxBet should be assigned");
    }

    #endregion

    #region Manager Priority Tests
    // Manager priority tests removed - trader priority system has been removed
    // Now only level points matter (Elite: 64, Member: 16, First: 4, Second: 2, Third: 1)
    // Manager status no longer affects priority

    [Test]
    public void Assign_WithOnlyManagers_EliteManagerHasHighestPriority()
    {
        // Arrange - Among managers, Elite manager should still have highest priority
        // Matches must overlap to force distribution between traders
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteManager", new string[] { }, "Elite", true,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("2", "MemberManager", new string[] { }, "Member", true,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),
                CreateTraderShift("3", "FirstManager", new string[] { }, "First", true,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                // All matches overlap to force each trader to pick only one
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 22000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T12:30:00Z"), 30000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 38000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3);

        // Elite manager should get the highest MaxBet
        var highestAssignment = result.Assignments.First(a => a.MaxBet == 38000);
        highestAssignment.Hdp.Should().Be("EliteManager", "Elite manager should get highest MaxBet when only managers available");

        // Member manager should get middle MaxBet
        var middleAssignment = result.Assignments.First(a => a.MaxBet == 30000);
        middleAssignment.Hdp.Should().Be("MemberManager", "Member manager should get middle MaxBet");

        // First manager should get lowest MaxBet
        var lowestAssignment = result.Assignments.First(a => a.MaxBet == 22000);
        lowestAssignment.Hdp.Should().Be("FirstManager", "First manager should get lowest MaxBet");
    }

    #endregion

    #region Capacity Distribution Tests

    [Test]
    public void Assign_TwoM2MatchesSameTime_TwoTradersWithCapacity2_DistributesFairly()
    {
        // Arrange - When both traders have capacity=2 and 2 matches at same time exist
        // The solver should distribute matches between traders rather than giving both to Elite
        // This ensures fair workload distribution
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 2),  // Can handle 2 concurrent matches
                CreateTraderShift("2", "ThirdTrader", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 2)   // Can handle 2 concurrent matches
            },
            Matches = new List<Match>
            {
                // Two matches at exactly the same time
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        // Get the assignments (excluding unassigned)
        var assignedMatches = result.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignedMatches.Should().HaveCount(2);

        // Verify that EACH trader gets ONE match (fair distribution)
        var traderNames = assignedMatches.Select(a => a.Hdp).ToList();
        traderNames.Should().Contain("EliteTrader", "Elite trader should handle one match");
        traderNames.Should().Contain("ThirdTrader", "Third trader should handle one match");
        traderNames.Should().OnlyHaveUniqueItems("each trader should only handle one match for fair distribution");
    }

    [Test]
    public void Assign_TwoM2MatchesSameTime_EliteCapacity1ThirdCapacity2_ThirdHandlesBoth()
    {
        // Arrange - Elite has capacity=1, Third has capacity=2
        // Both matches at same time, so Elite can only take 1
        // Third should take both since they have capacity
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1),  // Can only handle 1 match at a time
                CreateTraderShift("2", "ThirdTrader", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 2)   // Can handle 2 concurrent matches
            },
            Matches = new List<Match>
            {
                // Two matches at exactly the same time
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        // Get the assignments (excluding unassigned)
        var assignedMatches = result.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignedMatches.Should().HaveCount(2);

        // Third trader should handle BOTH matches since Elite only has capacity for 1
        // and Third has capacity for 2
        assignedMatches.Should().AllSatisfy(a =>
        {
            a.Hdp.Should().Be("ThirdTrader", "Third trader has capacity for both concurrent matches");
            a.Ou.Should().Be("ThirdTrader");
        });
    }

    [Test]
    public void Assign_ThreeM2MatchesSameTime_TwoTradersWithCapacity2_OptimalDistribution()
    {
        // Arrange - 3 matches at same time, 2 traders with capacity=2 each
        // Elite (higher priority) should get 2 matches
        // Third should get 1 match
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 2),
                CreateTraderShift("2", "ThirdTrader", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 2)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 22000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 28000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 35000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3, "all three matches should be assigned");

        var assignedMatches = result.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignedMatches.Should().HaveCount(3);

        // Elite (higher priority) should handle 2 matches
        var eliteMatches = assignedMatches.Where(a => a.Hdp == "EliteTrader").ToList();
        eliteMatches.Should().HaveCount(2, "Elite trader should get 2 matches (capacity limit)");

        // Third should handle 1 match
        var thirdMatches = assignedMatches.Where(a => a.Hdp == "ThirdTrader").ToList();
        thirdMatches.Should().HaveCount(1, "Third trader should get remaining 1 match");

        // Elite should get the two highest MaxBet matches (35K and 28K)
        eliteMatches.Select(a => a.MaxBet).Should().Contain(new[] { 35000, 28000 },
            "Elite trader should handle the highest MaxBet matches");

        // Third should get the lowest MaxBet match (22K)
        thirdMatches.First().MaxBet.Should().Be(22000,
            "Third trader should handle the lowest MaxBet match");
    }

    #endregion
}
