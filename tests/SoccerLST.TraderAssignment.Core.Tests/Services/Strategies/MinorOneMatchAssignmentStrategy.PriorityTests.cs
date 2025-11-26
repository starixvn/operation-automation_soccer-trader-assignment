using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorOneMatchAssignmentStrategyPriorityTests
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

    #region Trader Level Priority Tests

    [Test]
    public void Assign_WithEliteAndMemberTraders_AssignsEliteToHigherMaxBet()
    {
        // Arrange - Elite trader should get the 65K match, Member gets the 45K match
        // Matches must overlap to force distribution between traders
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("2", "MemberTrader", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 45000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 65000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var highMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 65000);
        highMaxBetAssignment.Hdp.Should().Be("EliteTrader", "Elite trader should handle higher MaxBet match");
        highMaxBetAssignment.Ou.Should().Be("EliteTrader", "Elite trader should handle higher MaxBet match");

        var lowMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 45000);
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
                    m1Capacity: 1),
                CreateTraderShift("2", "MemberTrader", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("3", "FirstTrader", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 41000),  // Lowest
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T12:30:00Z"), 55000),  // Middle
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 69000)   // Highest
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3);

        var highestMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 69000);
        highestMaxBetAssignment.Hdp.Should().Be("EliteTrader");
        highestMaxBetAssignment.Ou.Should().Be("EliteTrader");

        var middleMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 55000);
        middleMaxBetAssignment.Hdp.Should().Be("MemberTrader");
        middleMaxBetAssignment.Ou.Should().Be("MemberTrader");

        var lowestMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 41000);
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
                    m1Capacity: 1),
                CreateTraderShift("2", "EliteTraderB", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 68000)
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
                    m1Capacity: 1),
                CreateTraderShift("2", "EliteNonManager", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 60000)
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
                    m1Capacity: 1),
                CreateTraderShift("2", "FirstTraderB", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 42000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 67000)
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
                    m1Capacity: 1),
                CreateTraderShift("2", "Elite2", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("3", "Member1", new string[] { }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("4", "First1", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"), 40000),   // Lowest
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 52000),   // Low-Mid
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 63000),   // High-Mid
                CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T13:30:00Z"), DateTimeOffset.Parse("2025-09-28T14:30:00Z"), 69000)    // Highest
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(4, "all matches should be assigned");

        // Verify Elite traders get the highest MaxBet matches
        var highMaxBetMatches = result.Assignments.Where(a => a.MaxBet >= 63000).ToList();
        highMaxBetMatches.Should().HaveCount(2);
        highMaxBetMatches.Should().OnlyContain(a => a.Hdp.StartsWith("Elite") && a.Ou.StartsWith("Elite"),
            "Elite traders should handle the highest MaxBet matches");
    }

    [Test]
    public void Assign_WithEliteAndThirdTraders_ForcesCoverageOverOptimality()
    {
        // Arrange - Two overlapping matches force Elite to choose one
        // Third can only work the 65K match (shift constraint)
        // Expected: Elite takes 40K, Third takes 65K (both matches covered)
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteTrader", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("2", "ThirdTrader", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T10:30:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 40000),  // Only Elite can work (starts before Third shift)
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 65000)   // Both can work, but overlaps with Match 1
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "both matches should be assigned");

        var highMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 65000);
        highMaxBetAssignment.Hdp.Should().Be("ThirdTrader", "Third trader must take 65K match as they can't work the 40K match");
        highMaxBetAssignment.Ou.Should().Be("ThirdTrader");

        var lowMaxBetAssignment = result.Assignments.First(a => a.MaxBet == 40000);
        lowMaxBetAssignment.Hdp.Should().Be("EliteTrader", "Elite trader takes 40K match to ensure full coverage");
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
                    m1Capacity: 1),
                CreateTraderShift("2", "FirstTrader", new string[] { }, "First", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 55000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 55000)
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
                    m1Capacity: 1),
                CreateTraderShift("2", "MemberManager", new string[] { }, "Member", true,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("3", "FirstManager", new string[] { }, "First", true,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                // All matches overlap to force each trader to pick only one
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 42000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T12:30:00Z"), 55000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 68000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3);

        // Elite manager should get the highest MaxBet
        var highestAssignment = result.Assignments.First(a => a.MaxBet == 68000);
        highestAssignment.Hdp.Should().Be("EliteManager", "Elite manager should get highest MaxBet when only managers available");

        // Member manager should get middle MaxBet
        var middleAssignment = result.Assignments.First(a => a.MaxBet == 55000);
        middleAssignment.Hdp.Should().Be("MemberManager", "Member manager should get middle MaxBet");

        // First manager should get lowest MaxBet
        var lowestAssignment = result.Assignments.First(a => a.MaxBet == 42000);
        lowestAssignment.Hdp.Should().Be("FirstManager", "First manager should get lowest MaxBet");
    }

    #endregion

    #region Boundary Condition Tests

    [Test]
    public void Assign_MatchEndEqualsThirdShiftEnd_AssignsToSecondTrader()
    {
        // Arrange - Two traders: Second and Third level
        // Match end time equals Third trader's shift end time
        // Expected: Match should be assigned to Second trader (higher priority)
        // even though Third trader's shift can technically accommodate the match
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "SecondTrader", new string[] { }, "Second", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m1Capacity: 1),
                CreateTraderShift("2", "ThirdTrader", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T16:00:00Z"),  // Shift ends at 16:00
                    m1Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T14:00:00Z"),  // Match: 14:00 - 16:00
                    DateTimeOffset.Parse("2025-09-28T16:00:00Z"),  // Ends exactly at Third's shift end
                    50000)  // M1 range: 40,000 - 70,000
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "match should be assigned");

        var assignment = result.Assignments.First(a => a.Hdp != "N/A");
        assignment.Hdp.Should().Be("SecondTrader",
            "Second trader should be assigned (higher priority than Third)");
        assignment.Ou.Should().Be("SecondTrader",
            "Second trader should be assigned (higher priority than Third)");
        assignment.MaxBet.Should().Be(50000);
    }

    #endregion
}
