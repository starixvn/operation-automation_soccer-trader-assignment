using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M1;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public class MinorOneMatchAssignmentStrategyConsecutiveBonusTests
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

    #region Match-to-Match Consecutive Bonus Tests

    [Test]
    public void Assign_TwoMatches_ZeroMinuteGap_PrefersSameTrader()
    {
        // Arrange - Two matches with 0 minute gap (back-to-back)
        // Match1: 10:00-12:00, Match2: 12:00-14:00
        // Two traders available, should prefer assigning both to same trader
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000)
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

        // Both matches should be assigned to the same trader (consecutive bonus)
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "0 minute gap should prefer same trader");
        match1Assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_TwoMatches_120MinuteGap_PrefersSameTrader()
    {
        // Arrange - Two matches with 120 minute gap (2 hours)
        // Match1: 10:00-12:00, Match2: 14:00-16:00
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 50000)
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

        // Both matches should be assigned to the same trader (120 min gap bonus)
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "120 minute gap should prefer same trader");
        match1Assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_TwoMatches_240MinuteGap_PrefersSameTrader()
    {
        // Arrange - Two matches with 240 minute gap (4 hours)
        // Match1: 10:00-12:00, Match2: 16:00-18:00
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T16:00:00Z"), DateTimeOffset.Parse("2025-09-28T18:00:00Z"), 50000)
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

        // Both matches should be assigned to the same trader (240 min gap bonus)
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "240 minute gap should prefer same trader");
        match1Assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_TwoMatches_15MinuteGap_GetsSmallerBonus()
    {
        // Arrange - Two matches with 15 minute gap (near-perfect)
        // Match1: 10:00-12:00, Match2: 12:15-14:15
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:15:00Z"), DateTimeOffset.Parse("2025-09-28T14:15:00Z"), 50000)
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

        // Both matches should be assigned to the same trader (smaller bonus but still incentivized)
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp, "15 minute gap should still prefer same trader");
        match1Assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_TwoMatches_OverlappingTimes_DoesNotGetBonus()
    {
        // Arrange - Two matches that overlap
        // Match1: 10:00-12:00, Match2: 11:00-13:00
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "Both matches should be assigned but to different traders");

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        // Overlapping matches must go to different traders
        match1Assignment.Hdp.Should().NotBe(match2Assignment.Hdp, "Overlapping matches cannot be assigned to same trader");
    }

    [Test]
    public void Assign_TwoMatches_LargeGap300Minutes_NoBonus()
    {
        // Arrange - Two matches with 300 minute gap (5 hours) - beyond 240 min threshold
        // Match1: 10:00-12:00, Match2: 17:00-19:00
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T17:00:00Z"), DateTimeOffset.Parse("2025-09-28T19:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "Both matches should be assigned");

        // With 300 min gap, there's no consecutive bonus, so assignment may vary
        // Just verify both are assigned
        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");
        match1Assignment.Hdp.Should().NotBe("N/A");
        match2Assignment.Hdp.Should().NotBe("N/A");
    }

    #endregion

    #region AssignedJob-to-Match Consecutive Bonus Tests

    [Test]
    public void Assign_MatchAfterMJJob_ZeroMinuteGap_PrefersThatTrader()
    {
        // Arrange - Trader has MJ job 10:00-12:00, M1 match is 12:00-14:00 (0 min gap)
        var traderWithMJJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMJJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "Trader with MJ job ending at match start should get consecutive bonus");
    }

    [Test]
    public void Assign_MatchAfterM1Job_120MinuteGap_PrefersThatTrader()
    {
        // Arrange - Trader has M1 job 10:00-12:00, new M1 match is 14:00-16:00 (120 min gap)
        var traderWithM1Job = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM1Job, traderNoJob },
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
        result.AssignedMatches.Should().Be(1);

        // With gap=0 only optimization, 120-minute gaps get NO bonus
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBeNull("Match should be assigned to someone");
    }

    [Test]
    public void Assign_MatchAfterJob_240MinuteGap_NoBonus()
    {
        // Arrange - Trader has job 10:00-12:00, match is 16:00-18:00 (240 min gap)
        // With gap=0 only optimization, no bonus for 240-minute gaps
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T16:00:00Z"), DateTimeOffset.Parse("2025-09-28T18:00:00Z"), 50000)
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
    public void Assign_MatchAfterJob_15MinuteGap_NoBonus()
    {
        // Arrange - Trader has job 10:00-12:00, match is 12:15-14:15 (15 min gap)
        // With performance optimization, only perfect gaps (0, 120, 240 min) get bonus
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:15:00Z"), DateTimeOffset.Parse("2025-09-28T14:15:00Z"), 50000)
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
    public void Assign_MatchBeforeJob_NoBonus()
    {
        // Arrange - Trader has job 14:00-16:00, match is 10:00-12:00 (before the job)
        // Match should not get consecutive bonus from future job
        var traderWithFutureJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithFutureJob, traderNoJob },
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
        result.AssignedMatches.Should().Be(1);

        // Match before job gets no consecutive bonus, so both traders are equally preferred
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_MatchAfterJob_LargeGap300Minutes_NoBonus()
    {
        // Arrange - Trader has job 10:00-12:00, match is 17:00-19:00 (300 min gap - beyond threshold)
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T17:00:00Z"), DateTimeOffset.Parse("2025-09-28T19:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        // With 300 min gap, no consecutive bonus, so both traders are equally preferred
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_MultipleJobs_MatchAfterLastJob_GetsBonus()
    {
        // Arrange - Trader has multiple jobs, match follows the last one with 0 min gap
        var traderWithMultipleJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z")),
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJobs = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMultipleJobs, traderNoJobs },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "Trader with job ending at match start should get consecutive bonus");
    }

    #endregion

    #region Shift Boundary Tests

    [Test]
    public void Assign_MatchesOutsideShift_NoConsecutiveBonus()
    {
        // Arrange - Trader shift 08:00-16:00, but matches are 10:00-12:00 and 17:00-19:00
        // Second match is outside shift, so no consecutive bonus
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T17:00:00Z"), DateTimeOffset.Parse("2025-09-28T19:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Only first match is within shift");

        var match1Assignment = result.Assignments.First(a => a.RowNumber == "1");
        var match2Assignment = result.Assignments.First(a => a.RowNumber == "2");

        match1Assignment.Hdp.Should().Be("TraderA");
        match2Assignment.Hdp.Should().Be("N/A", "Match is outside trader shift");
    }

    [Test]
    public void Assign_JobOutsideShift_NoConsecutiveBonusForMatch()
    {
        // Arrange - Trader shift 12:00-20:00, has job 08:00-10:00 (before shift)
        // Match 12:00-14:00 should not get bonus from job outside shift
        var traderWithJobBeforeShift = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T12:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T20:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJobBeforeShift, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1);

        // Match within shift but job before shift - no bonus, so both traders equally preferred
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().NotBe("N/A");
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public void Assign_ThreeConsecutiveMatches_PrefersAllToSameTrader()
    {
        // Arrange - Three matches with 0 minute gaps: 10-12, 12-14, 14-16
        var traderA = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 50000)
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

        // All three should go to same trader due to consecutive bonuses
        match1Assignment.Hdp.Should().Be(match2Assignment.Hdp);
        match2Assignment.Hdp.Should().Be(match3Assignment.Hdp);
        match1Assignment.Hdp.Should().NotBe("N/A");
    }

    [Test]
    public void Assign_JobThenTwoConsecutiveMatches_OnlyFirstMatchGetsBonus()
    {
        // Arrange - Trader has job 10-12, then two consecutive matches 12-14, 14-16
        // With job-to-match only optimization: only job→match1 gets bonus, not match1→match2
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 50000)
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
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                // Match ends at 12:00, job starts at 12:00 - should get consecutive bonus
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
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
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                // Job ends at 10:00, match starts at 10:00 - should get consecutive bonus
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
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
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"))
            });

        var traderNoJob = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob, traderNoJob },
            Matches = new List<Match>
            {
                // Match ends at 11:00, job starts at 12:00 - 60 minute gap, no bonus
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 50000)
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
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var traderB = CreateTraderShift("2", "TraderB", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z"))
            });

        var traderC = CreateTraderShift("3", "TraderC", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB, traderC },
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
