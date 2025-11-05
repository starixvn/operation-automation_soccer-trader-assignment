using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorTwoMatchAssignmentStrategyConcurrentTests
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

    #region M3/M4 Concurrent Assignment Tests

    [Test]
    public void Assign_WithM3JobSameStartTime_AndM2Capacity2_AssignsMatch()
    {
        // Arrange - M2 match can run alongside M3 job if they start at same time and M2Capacity >= 2
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M2 match can run alongside M3 job with same start time when M2Capacity >= 2");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithM4JobSameStartTime_AndM2Capacity2_AssignsMatch()
    {
        // Arrange - M2 match can run alongside M4 job if they start at same time and M2Capacity >= 2
        var traderWithM4Job = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M4", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM4Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T14:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M2 match can run alongside M4 job with same start time when M2Capacity >= 2");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithM3JobSameStartTime_ButM2Capacity1_DoesNotAssign()
    {
        // Arrange - M2 match cannot run alongside M3 if M2Capacity is only 1
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M2Capacity is 1, cannot handle M2 match alongside M3 job");
    }

    [Test]
    public void Assign_WithM3JobDifferentStartTime_DoesNotAssign()
    {
        // Arrange - M2 match cannot run alongside M3 if start times don't match
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:30:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Start times don't match - M2 match overlaps with M3 job");
    }

    [Test]
    public void Assign_WithMJJobSameStartTime_DoesNotAssign()
    {
        // Arrange - M2 match cannot run alongside MJ job even with same start time
        var traderWithMJJob = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("MJ", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMJJob },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "MJ jobs always block M2 matches, even with same start time");
    }

    [Test]
    public void Assign_WithM1JobSameStartTime_DoesNotAssign()
    {
        // Arrange - M2 match cannot run alongside M1 job even with same start time (M1 has higher priority)
        var traderWithM1Job = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M1", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM1Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "M1 jobs always block M2 matches, even with same start time");
    }

    [Test]
    public void Assign_WithTwoM3Jobs_AndM2Capacity2_DoesNotAssign()
    {
        // Arrange - If trader already has 2 M3 jobs at same time, cannot add M2 match even with M2Capacity=2
        var traderWithTwoM3Jobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
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
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
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
    public void Assign_WithM3JobSameStartTime_AndM2Capacity3_AssignsMatch()
    {
        // Arrange - With M2Capacity=3, can handle M3 job + M2 match
        var traderWithM3Job = CreateTraderShift("1", "TraderA", new string[] { }, "First", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 3,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithM3Job },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M2Capacity=3 allows M2 match alongside M3 job");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithM3M4SequentialJobs_AndMatchOverlappingBoth_DoesNotAssign()
    {
        // Arrange - M2 match overlaps with M3 (same start) and M4 (different start)
        // M3: 08:00-10:00, M4: 10:00-12:00, Match: 08:00-11:00
        // Even though M2Capacity=2, the M4 overlap blocks the assignment
        var traderWithSequentialJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
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
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:00:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Match overlaps with M4 job that has different start time");
    }

    #endregion

    #region M2 Concurrent Assignment Tests (Multiple M2 Matches)

    [Test]
    public void Assign_TwoM2Matches_SameTime_WithM2Capacity2_AssignsBoth()
    {
        // Arrange - Trader with M2Capacity=2 can handle 2 concurrent M2 matches at same start time
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "M2Capacity=2 allows 2 concurrent M2 matches");
        result.Assignments.Should().HaveCount(2);
        result.Assignments.Should().AllSatisfy(a =>
        {
            a.Hdp.Should().Be("TraderA");
            a.Ou.Should().Be("TraderA");
        });
    }

    [Test]
    public void Assign_TwoM2Matches_SameTime_WithM2Capacity1_AssignsOne()
    {
        // Arrange - Trader with M2Capacity=1 can only handle 1 M2 match
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M2Capacity=1 allows only 1 concurrent M2 match");
        result.Assignments.Should().HaveCount(2, "All matches appear in response");
        result.Assignments.Count(a => a.Hdp != "N/A").Should().Be(1, "Only 1 match should be assigned");
    }

    [Test]
    public void Assign_ThreeM2Matches_SameTime_WithM2Capacity3_AssignsAll()
    {
        // Arrange - Trader with M2Capacity=3 can handle 3 concurrent M2 matches
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 3);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 22000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(3, "M2Capacity=3 allows 3 concurrent M2 matches");
        result.Assignments.Should().HaveCount(3);
    }

    [Test]
    public void Assign_ThreeM2Matches_SameTime_WithM2Capacity2_AssignsTwo()
    {
        // Arrange - Trader with M2Capacity=2 can only handle 2 out of 3 M2 matches
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 22000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "M2Capacity=2 limits to 2 concurrent M2 matches");
        result.Assignments.Should().HaveCount(3, "All matches appear in response");
        result.Assignments.Count(a => a.Hdp != "N/A").Should().Be(2, "Only 2 matches should be assigned");
    }

    [Test]
    public void Assign_TwoM2Matches_OneM3Job_SameTime_WithM2Capacity3_AssignsTwo()
    {
        // Arrange - Trader has 1 M3 job, M2Capacity=3, can handle 2 M2 matches alongside M3
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 3,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "M2Capacity=3 with 1 M3 job leaves room for 2 M2 matches");
        result.Assignments.Should().HaveCount(2);
    }

    [Test]
    public void Assign_TwoM2Matches_OneM3Job_SameTime_WithM2Capacity2_AssignsOne()
    {
        // Arrange - Trader has 1 M3 job, M2Capacity=2, can handle only 1 M2 match alongside M3
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M3", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M2Capacity=2 with 1 M3 job leaves room for only 1 M2 match");
        result.Assignments.Should().HaveCount(2, "All matches appear in response");
        result.Assignments.Count(a => a.Hdp != "N/A").Should().Be(1, "Only 1 match should be assigned");
    }

    [Test]
    public void Assign_TwoM2Matches_OneM2AlreadyHandled_SameTime_WithM2Capacity2_AssignsOne()
    {
        // Arrange - Trader has 1 M2 unchanged match, M2Capacity=2, can handle only 1 more M2 match
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 2,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("M2", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 25000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:30:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "M2Capacity=2 with 1 M2 unchanged match leaves room for only 1 more M2 match");
        result.Assignments.Should().HaveCount(2, "All matches appear in response");
        result.Assignments.Count(a => a.Hdp != "N/A").Should().Be(1, "Only 1 match should be assigned");
    }

    #endregion
}
