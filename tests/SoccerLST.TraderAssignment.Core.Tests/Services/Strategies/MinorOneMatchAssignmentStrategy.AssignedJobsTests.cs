using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorOneMatchAssignmentStrategyAssignedJobsTests
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

    #region Overlap with Assigned Jobs Tests

    [Test]
    public void Assign_WithOverlappingMJJob_DoesNotAssign()
    {
        // Arrange - Trader has MJ job from 10:00-12:00, M1 match is 11:00-13:00 (overlaps)
        var traderWithMJJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMJJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M1 match overlaps with existing MJ job");
    }

    [Test]
    public void Assign_WithNonOverlappingMJJob_AssignsTrader()
    {
        // Arrange - Trader has MJ job from 10:00-12:00, M1 match is 12:00-14:00 (no overlap)
        var traderWithMJJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMJJob },
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
        result.AssignedMatches.Should().Be(1, "M1 match does not overlap with MJ job");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithOverlappingM1Job_DoesNotAssign()
    {
        // Arrange - Trader has M1 job from 10:00-12:00, new M1 match is 11:00-13:00 (overlaps)
        var traderWithM1Job = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM1Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M1 match overlaps with existing M1 job");
    }

    [Test]
    public void Assign_WithMultipleAssignedJobs_BlocksOnAnyOverlap()
    {
        // Arrange - Trader has multiple jobs, match overlaps with one of them
        var traderWithMultipleJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z")),
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T12:00:00Z"), DateTimeOffset.Parse("2025-09-28T14:00:00Z")),
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T15:00:00Z"), DateTimeOffset.Parse("2025-09-28T17:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMultipleJobs },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Match overlaps with one of the assigned jobs");
    }

    [Test]
    public void Assign_WithMultipleAssignedJobs_AssignsInGap()
    {
        // Arrange - Trader has multiple jobs, match fits in gap between them
        var traderWithMultipleJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z")),
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T16:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMultipleJobs },
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
        result.AssignedMatches.Should().Be(1, "Match fits in gap between assigned jobs");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithAssignedJobTouchingAtBoundary_AssignsTrader()
    {
        // Arrange - Assigned job ends exactly when M1 match starts (no overlap)
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob },
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
        result.AssignedMatches.Should().Be(1, "Job ends exactly when match starts - no overlap");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithPartialOverlap_DoesNotAssign()
    {
        // Arrange - Match partially overlaps with assigned job
        var traderWithJob = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithJob },
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
        result.AssignedMatches.Should().Be(0, "Match partially overlaps with assigned job");
    }

    [Test]
    public void Assign_WithNoAssignedJobs_AssignsTrader()
    {
        // Arrange - Trader has no assigned jobs
        var traderNoJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false, start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"), end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"), m1Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderNoJobs },
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
        result.AssignedMatches.Should().Be(1, "Trader has no assigned jobs");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_TwoM1MatchesSameTime_WithM1Capacity2_AssignsOnlyOneMatch()
    {
        // Arrange - 2 M1 matches at same time, 1 trader with M1Capacity=2
        // IMPORTANT: M1Capacity only applies to M3/M4 concurrent jobs, NOT to multiple M1 matches
        // Therefore, trader can only handle ONE M1 match at a time, regardless of capacity
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 55000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M1Capacity only applies to M3/M4 jobs, not M1 matches. Trader can only handle one M1 match at a time.");

        // Verify only one match is assigned (should be the higher value one)
        var assignedMatches = result.Assignments.Where(a => a.Hdp != "N/A").ToList();
        assignedMatches.Should().HaveCount(1);
        assignedMatches[0].MaxBet.Should().Be(55000, "Should assign the higher value match");
        assignedMatches[0].Hdp.Should().Be("TraderA");
        assignedMatches[0].Ou.Should().Be("TraderA");
    }

    #endregion
}
