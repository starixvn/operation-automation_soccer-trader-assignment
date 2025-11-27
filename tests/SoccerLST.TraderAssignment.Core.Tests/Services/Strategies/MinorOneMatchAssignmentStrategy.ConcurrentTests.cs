using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorOneMatchAssignmentStrategyConcurrentTests
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

    #region M3/M4 Concurrent Assignment Tests

    [Test]
    public void Assign_WithM3JobSameStartTime_AndM1Capacity2_AssignsMatch()
    {
        // Arrange - M1 match can run alongside M3 job if they start at same time and M1Capacity >= 2
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M1 match can run alongside M3 job with same start time when M1Capacity >= 2");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithM4JobSameStartTime_AndM1Capacity2_AssignsMatch()
    {
        // Arrange - M1 match can run alongside M4 job if they start at same time and M1Capacity >= 2
        var traderWithM4Job = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M4", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM4Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M1 match can run alongside M4 job with same start time when M1Capacity >= 2");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithM3JobSameStartTime_ButM1Capacity1_DoesNotAssign()
    {
        // Arrange - M1 match cannot run alongside M3 if M1Capacity is only 1
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M1Capacity is 1, cannot handle M1 match alongside M3 job");
    }

    [Test]
    public void Assign_WithM3JobDifferentStartTime_DoesNotAssign()
    {
        // Arrange - M1 match cannot run alongside M3 if start times don't match
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Start times don't match - M1 match overlaps with M3 job");
    }

    [Test]
    public void Assign_WithMJJobSameStartTime_DoesNotAssign()
    {
        // Arrange - M1 match cannot run alongside MJ job even with same start time
        var traderWithMJJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMJJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "MJ jobs always block M1 matches, even with same start time");
    }

    [Test]
    public void Assign_WithTwoM3Jobs_AndM1Capacity2_DoesNotAssign()
    {
        // Arrange - If trader already has 2 M3 jobs at same time, cannot add M1 match even with M1Capacity=2
        var traderWithTwoM3Jobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z")),
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithTwoM3Jobs },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Trader already handling 2 M3 jobs, no capacity left");
    }

    [Test]
    public void Assign_WithM3JobSameStartTime_AndM1Capacity3_AssignsMatch()
    {
        // Arrange - With M1Capacity=3, can handle M3 job + M1 match
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "First", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 3,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M1Capacity=3 allows M1 match alongside M3 job");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithM3M4SequentialJobs_AndMatchOverlappingBoth_DoesNotAssign()
    {
        // Arrange - M1 match overlaps with M3 (same start) and M4 (different start)
        // M3: 08:00-10:00, M4: 10:00-12:00, Match: 08:00-11:00
        // Even though M1Capacity=2, the M4 overlap blocks the assignment
        var traderWithSequentialJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z")),
                CreateAssignedJob("M4", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithSequentialJobs },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Match overlaps with M4 job that has different start time");
    }

    [Test]
    public void Assign_WithM1JobSameStartTime_AndM1Capacity2_DoesNotAssign()
    {
        // Arrange - M1 match cannot run alongside M1 job even with M1Capacity=2
        // M1 capacity only applies to M3/M4 jobs, not M1/M2 jobs
        var traderWithM1Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM1Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M1 jobs always block M1 matches, even with same start time and M1Capacity=2");
    }

    [Test]
    public void Assign_WithM2JobSameStartTime_AndM1Capacity2_DoesNotAssign()
    {
        // Arrange - M1 match cannot run alongside M2 job even with M1Capacity=2
        // M1 capacity only applies to M3/M4 jobs, not M1/M2 jobs
        var traderWithM2Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m1Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M2", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM2Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M2 jobs always block M1 matches, even with same start time and M1Capacity=2");
    }

    #endregion
}
