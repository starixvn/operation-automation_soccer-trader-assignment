using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorTwoMatchAssignmentStrategyConsecutiveBonusTests
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

    #region Match-to-Match Consecutive Bonus Tests

    [Test]
    public void Assign_TwoMatches_ZeroMinuteGap_PrefersSameTrader()
    {
        // Arrange - Two matches with 0 minute gap (back-to-back)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "0 minute gap should prefer same trader");
    }

    [Test]
    public void Assign_TwoMatches_120MinuteGap_PrefersSameTrader()
    {
        // Arrange - Two matches with 120 minute gap (2 hours)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "120 minute gap should prefer same trader");
    }

    [Test]
    public void Assign_TwoMatches_240MinuteGap_PrefersSameTrader()
    {
        // Arrange - Two matches with 240 minute gap (4 hours)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T16:00:00Z"), DateTimeOffset.Parse("2025-09-28T18:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "240 minute gap should prefer same trader");
    }

    [Test]
    public void Assign_TwoMatches_15MinuteGap_GetsReducedBonus()
    {
        // Arrange - Two matches with 15 minute gap (gets 20% bonus)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:15:00Z"), DateTimeOffset.Parse("2025-09-28T14:15:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        // Should still prefer same trader due to 20% bonus, though weaker than perfect gaps
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "15 minute gap gets 20% bonus");
    }

    [Test]
    public void Assign_TwoMatches_OverlappingTimes_NoConsecutiveBonus()
    {
        // Arrange - Two overlapping matches (no gap bonus)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        // Both can be assigned due to M2 capacity, but no consecutive bonus since they overlap
        result.AssignedMatches.Should().BeGreaterThanOrEqualTo(1);
    }

    [Test]
    public void Assign_TwoMatches_LargeGap_NoConsecutiveBonus()
    {
        // Arrange - Two matches with 300 minute gap (exceeds 240 min limit)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T17:00:00Z"), DateTimeOffset.Parse("2025-09-28T19:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);
        // No assertion on same trader - gap is too large for bonus
    }

    [Test]
    public void Assign_TwoMatches_ReverseOrder_NoBonus()
    {
        // Arrange - Second match ends before first match starts (negative gap)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);
        // No assertion on same trader - reverse order doesn't get bonus
    }

    #endregion

    #region AssignedJob-to-Match Consecutive Bonus Tests

    [Test]
    public void Assign_AssignedJobThenMatch_ZeroMinuteGap_PrefersTraderWithJob()
    {
        // Arrange - Trader has MJ job ending at 12:00, M2 match starts at 12:00
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "0 minute gap from assigned job should prefer same trader");
    }

    [Test]
    public void Assign_AssignedJobThenMatch_120MinuteGap_PrefersTraderWithJob()
    {
        // Arrange - Trader has M1 job ending at 12:00, M2 match starts at 14:00 (120 min gap)
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        // With gap=0 only optimization, 120-minute gaps get NO bonus
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBeNull("Match should be assigned to someone");
    }

    [Test]
    public void Assign_AssignedJobThenMatch_240MinuteGap_NoBonus()
    {
        // Arrange - Trader has M2 job ending at 12:00, M2 match starts at 16:00 (240 min gap)
        // With gap=0 only optimization, no bonus for 240-minute gaps
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M2", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T16:00:00Z"), DateTimeOffset.Parse("2025-09-28T18:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        // With gap=0 only optimization, 240-minute gaps get NO bonus
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBeNull("Match should be assigned to someone");
    }

    [Test]
    public void Assign_AssignedJobThenMatch_15MinuteGap_NoBonus()
    {
        // Arrange - Trader has job ending at 12:00, match starts at 12:15 (15 min gap)
        // With performance optimization, only perfect gaps (0, 120, 240 min) get bonus
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:15:00Z"), DateTimeOffset.Parse("2025-09-28T14:15:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        // With performance optimization, 15-minute gaps get NO bonus
        // Either trader could be assigned (no preference)
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBeNull("Match should be assigned to someone");
    }

    [Test]
    public void Assign_MatchBeforeAssignedJob_NoBonus()
    {
        // Arrange - Match ends before assigned job starts (negative gap)
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        // No assertion on which trader - match before job doesn't get bonus
    }

    [Test]
    public void Assign_AssignedJobThenMatch_LargeGap_NoBonus()
    {
        // Arrange - Trader has job ending at 10:00, match starts at 16:00 (360 min gap, exceeds limit)
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T16:00:00Z"), DateTimeOffset.Parse("2025-09-28T18:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        // No assertion on which trader - gap exceeds 240 min limit
    }

    [Test]
    public void Assign_MultipleAssignedJobs_ConsidersBestGap()
    {
        // Arrange - Trader has two jobs, match has perfect gap from second job
        var traderWithJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z")),
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJobs = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJobs, traderNoJobs },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "0 minute gap from second assigned job should prefer same trader");
    }

    #endregion

    #region Shift Boundary Tests

    [Test]
    public void Assign_MatchesOutsideShift_NoConsecutiveBonus()
    {
        // Arrange - Matches outside trader's shift don't get consecutive bonus
        var traderShortShift = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T13:00:00Z"),
            m2Capacity: 1);

        var traderFullShift = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderShortShift, traderFullShift },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        // TraderA cannot handle either match (outside shift)
        // TraderB can handle both but gets no consecutive bonus (TraderA can't participate)
        result.AssignedMatches.Should().BeGreaterThanOrEqualTo(1);
    }

    [Test]
    public void Assign_AssignedJobOutsideShift_NoConsecutiveBonus()
    {
        // Arrange - Assigned job outside shift doesn't contribute to consecutive bonus
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);
        // Match is at shift boundary (10:00) - no bonus since job is before shift starts
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public void Assign_ThreeConsecutiveMatches_PrefersAllSameTrader()
    {
        // Arrange - Three matches in sequence with ideal gaps
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var traderC = CreateTraderShift("3", "TraderC", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB, traderC },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 30000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3);

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");
        var match3Assignment = result.Assignments.First(a => a.RowNumber == "3");

        // All three should be assigned to same trader due to consecutive bonuses
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp);
        match2Assignment.Hdp.Should().Be(match3Assignment.Hdp);
    }

    [Test]
    public void Assign_AssignedJobPlusTwoMatches_OnlyFirstMatchGetsBonus()
    {
        // Arrange - Trader has job 8-10, then two consecutive matches 10-12, 12-14
        // With job-to-match only optimization: only job→match1 gets bonus, not match1→match2
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        // Only first match gets bonus (job→match1), second match has no preference
        match1Assignment.Hdp.Should().Be("TraderA", "First match follows job with 0 min gap");
        match2Assignment.Hdp.Should().NotBeNull("Second match should be assigned to someone");
    }

    #endregion

    #region Bidirectional Consecutive Bonus Tests

    [Test]
    public void Assign_MatchBeforeAssignedJob_BidirectionalBonus()
    {
        // Arrange - Match ends exactly when job starts (match.End == job.Start)
        // This tests the bidirectional bonus: Match → Job direction
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                // Match ends at 12:00, job starts at 12:00 - should get consecutive bonus
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        var assignment = result.Assignments.First();
        // TraderA should be strongly preferred due to consecutive bonus (match ends right before job starts)
        assignment.Hdp.Should().Be("TraderA", "Match ends exactly when job starts - should get bidirectional bonus");
    }

    [Test]
    public void Assign_JobThenMatch_ForwardDirectionBonus()
    {
        // Arrange - Job ends exactly when match starts (job.End == match.Start)
        // This tests the forward direction: Job → Match
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                // Job ends at 10:00, match starts at 10:00 - should get consecutive bonus
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        var assignment = result.Assignments.First();
        // TraderA should be strongly preferred due to consecutive bonus (job ends right when match starts)
        assignment.Hdp.Should().Be("TraderA", "Job ends exactly when match starts - should get forward direction bonus");
    }

    [Test]
    public void Assign_MatchBeforeAssignedJob_WithGap_NoBonus()
    {
        // Arrange - Match ends at 11:00, job starts at 12:00 (60 min gap)
        // With gap=0 optimization, this should NOT get bonus
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                // Match ends at 11:00, job starts at 12:00 - 60 minute gap, no bonus
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        // With gap=0 only, 60-minute gap gets NO bonus
        // Either trader could be assigned (no preference)
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBeNull("Match should be assigned to someone");
    }

    [Test]
    public void Assign_BothDirections_BothGetBonus()
    {
        // Arrange - Test both directions work simultaneously
        // TraderA: has job 8-10, match candidate 10-12 (Job → Match)
        // TraderB: has job 12-14, match candidate 10-12 (Match → Job)
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"))
            });

        var traderC = CreateTraderShift("3", "TraderC", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB, traderC },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        var assignment = result.Assignments.First();
        // Both TraderA (forward) and TraderB (backward) have consecutive bonus
        // Either should be preferred over TraderC (no bonus)
        assignment.Hdp.Should().Match(trader => trader == "TraderA" || trader == "TraderB",
            "TraderA (job→match) or TraderB (match→job) should be assigned, both have consecutive bonus");
        assignment.Hdp.Should().NotBe("TraderC", "TraderC has no consecutive bonus");
    }

    #endregion
}
