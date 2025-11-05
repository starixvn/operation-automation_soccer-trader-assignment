using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorTwoMatchAssignmentStrategyTests
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

    #region Match Within Shift Time Tests

    [Test]
    public void Assign_MatchCompletelyWithinShift_AssignsTrader()
    {
        // Arrange - M2: One trader handles both HDP and OU
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), // Shift: 08:00 - 18:00
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"), // Match: 10:00 - 12:00 (within shift)
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
                    30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>() // M2: No pairs needed
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "M2: Same trader handles both HDP and OU");
        assignment.Ou.Should().Be("TraderA", "M2: Same trader handles both HDP and OU");
    }

    [Test]
    public void Assign_MatchStartsBeforeShift_DoesNotAssign()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T10:00:00Z"), // Shift: 10:00 - 18:00
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"))
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T09:00:00Z"), // Match: 09:00 - 11:00 (starts before shift)
                    DateTimeOffset.Parse("2025-09-28T11:00:00Z"),
                    50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "match starts before trader shift begins");
    }

    [Test]
    public void Assign_MatchEndsAfterShift_DoesNotAssign()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T16:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T15:00:00Z"), DateTimeOffset.Parse("2025-09-28T17:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "match ends after trader shift ends");
    }

    [Test]
    public void Assign_MatchCompletelyOutsideShift_DoesNotAssign()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T16:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T18:00:00Z"), DateTimeOffset.Parse("2025-09-28T20:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "match is completely outside trader shift");
    }

    [Test]
    public void Assign_MatchExactlyAtShiftBoundaries_AssignsTrader()
    {
        // Arrange - Edge case: match exactly matches shift times
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T10:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T12:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "match exactly matches shift boundaries - should be valid");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "M2: Same trader handles both roles");
        assignment.Ou.Should().Be("TraderA", "M2: Same trader handles both roles");
    }

    [Test]
    public void Assign_MatchStartsAtShiftStart_AssignsTrader()
    {
        // Arrange - Edge case: match starts exactly when shift starts
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T10:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "match starting exactly at shift start should be valid");
    }

    [Test]
    public void Assign_MatchEndsAtShiftEnd_AssignsTrader()
    {
        // Arrange - Edge case: match ends exactly when shift ends
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T16:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "match ending exactly at shift end should be valid");
    }

    [Test]
    public void Assign_WithMultipleShifts_AssignsToCorrectShift()
    {
        // Arrange - Multiple traders with different shifts, match should go to appropriate trader
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                // Morning shift trader (cannot cover afternoon match)
                CreateTraderShift("1", "MorningTrader", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T14:00:00Z"), m2Capacity: 1),
                // Afternoon shift trader (can cover afternoon match)
                CreateTraderShift("2", "AfternoonTrader", new string[] { }, "Member", false, start: DateTimeOffset.Parse("2025-09-28T12:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T15:00:00Z"), DateTimeOffset.Parse("2025-09-28T17:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("AfternoonTrader", "M2: only afternoon shift trader can cover this match");
        assignment.Ou.Should().Be("AfternoonTrader", "M2: same trader handles both roles");
    }

    [Test]
    public void Assign_MatchPartiallyOverlapsShift_DoesNotAssign()
    {
        // Arrange - Match overlaps but is not fully contained
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T12:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "match extends beyond shift end time");
    }

    #endregion

    #region IsAlreadyHandled Tests

    [Test]
    public void Assign_WithIsAlreadyHandledTrue_IgnoresMatch()
    {
        // Arrange - Match with IsAlreadyHandled = true should be ignored
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000, null, true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalMatches.Should().Be(0, "IsAlreadyHandled matches are filtered out before processing");
        result.AssignedMatches.Should().Be(0, "no matches to assign");
        result.Assignments.Should().BeEmpty("no assignments should be made");
    }

    [Test]
    public void Assign_WithMixedIsAlreadyHandled_OnlyAssignsUnhandledMatches()
    {
        // Arrange - Mix of handled and unhandled matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"), 50000, null, true),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 50000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000, null, true),
                CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalMatches.Should().Be(2, "only 2 unhandled matches are processed");
        result.AssignedMatches.Should().Be(2, "both unhandled matches should be assigned");
        result.Assignments.Should().HaveCount(2);

        // Verify only unhandled matches are in assignments
        var assignedMatchNos = result.Assignments.Select(a => a.Match).ToList();
        assignedMatchNos.Should().Contain("101", "match 101 is not handled");
        assignedMatchNos.Should().Contain("103", "match 103 is not handled");
        assignedMatchNos.Should().NotContain("100", "match 100 is already handled");
        assignedMatchNos.Should().NotContain("102", "match 102 is already handled");
    }

    [Test]
    public void Assign_WithAllIsAlreadyHandled_ReturnsEmptyAssignments()
    {
        // Arrange - All matches already handled
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"), m2Capacity: 1)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 50000, null, true),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 50000, null, true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalMatches.Should().Be(0, "all matches are already handled");
        result.AssignedMatches.Should().Be(0);
        result.Assignments.Should().BeEmpty();
    }

    #endregion

    #region EXPT Match Restriction Tests

    [Test]
    public void Assign_EXPTMatch_TraderCannotHandleEXPT_DoesNotAssign()
    {
        // Arrange - M2 EXPT match with trader who cannot handle EXPT matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false)   // Cannot handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000, isEXPT: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "trader cannot handle EXPT matches");
    }

    [Test]
    public void Assign_EXPTMatch_TraderCanHandleEXPT_AssignsTrader()
    {
        // Arrange - M2 EXPT match with trader who can handle EXPT matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: true)    // Can handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000, isEXPT: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "trader can handle EXPT matches");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_NonEXPTMatch_TraderCannotHandleEXPT_AssignsTrader()
    {
        // Arrange - M2 non-EXPT match with trader who cannot handle EXPT matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Third", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false)   // Cannot handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000, isEXPT: false)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "EXPT restriction only applies to EXPT matches");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    #endregion
}
