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

    #region M2 Overlap Bug Prevention Tests
    // Tests for preventing traders from being assigned to overlapping matches with DIFFERENT start times
    // M2Capacity should only allow concurrent matches with the SAME start time

    /// <summary>
    /// This test reproduces the exact bug reported by the user:
    /// SLGRuby (M2Capacity=2) was assigned to:
    /// - Match 341: 23:15 - 01:15
    /// - Match 346: 23:30 - 01:30
    /// These matches overlap but have different start times.
    /// </summary>
    [Test]
    public void Assign_TwoOverlappingMatches_DifferentStartTimes_WithM2Capacity2_ShouldNotAssignBoth()
    {
        // Arrange
        var trader = CreateTraderShift("1", "SLGRuby", new string[] { }, "Second", false,
            start: DateTimeOffset.Parse("2024-11-08T15:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-09T03:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                // Match 341: 23:15 - 01:15
                CreateMatch("341", "341",
                    DateTimeOffset.Parse("2024-11-08T23:15:00Z"),
                    DateTimeOffset.Parse("2024-11-09T01:15:00Z"),
                    30000),
                // Match 346: 23:30 - 01:30 (overlaps with 341 from 23:30-01:15)
                CreateMatch("346", "346",
                    DateTimeOffset.Parse("2024-11-08T23:30:00Z"),
                    DateTimeOffset.Parse("2024-11-09T01:30:00Z"),
                    30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().BeLessThan(2,
            "Trader should NOT be assigned to both overlapping matches with different start times, " +
            "even with M2Capacity=2. M2Capacity only applies to matches with the SAME start time.");

        // If both were assigned, this is the bug
        var bothAssigned = result.Assignments.Count(a => a.Hdp == "SLGRuby") == 2;
        bothAssigned.Should().BeFalse(
            "BUG: Trader SLGRuby was assigned to both overlapping matches (341: 23:15-01:15 and 346: 23:30-01:30). " +
            "These matches overlap from 23:30-01:15 but have different start times. " +
            "M2Capacity=2 should only allow concurrent matches with the SAME start time.");
    }

    [Test]
    public void Assign_ThreeOverlappingMatches_AllDifferentStartTimes_WithM2Capacity3_ShouldNotAssignAll()
    {
        // Arrange - 3 matches that all overlap but have different start times
        var trader = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T15:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-09T05:00:00Z"),
            m2Capacity: 3);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                // Match 1: 22:00 - 00:00
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T22:00:00Z"),
                    DateTimeOffset.Parse("2024-11-09T00:00:00Z"),
                    30000),
                // Match 2: 22:30 - 00:30 (overlaps with 1)
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2024-11-08T22:30:00Z"),
                    DateTimeOffset.Parse("2024-11-09T00:30:00Z"),
                    30000),
                // Match 3: 23:00 - 01:00 (overlaps with both 1 and 2)
                CreateMatch("3", "102",
                    DateTimeOffset.Parse("2024-11-08T23:00:00Z"),
                    DateTimeOffset.Parse("2024-11-09T01:00:00Z"),
                    30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().BeLessThan(3,
            "Trader should NOT be assigned to all 3 overlapping matches with different start times, " +
            "even with M2Capacity=3. M2Capacity only applies to matches with the SAME start time.");
    }

    [Test]
    public void Assign_TwoPartiallyOverlappingMatches_DifferentStartTimes_WithM2Capacity2_ShouldNotAssignBoth()
    {
        // Arrange - Matches with 30 minute overlap but different start times
        var trader = CreateTraderShift("1", "TraderB", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                // Match 1: 10:00 - 11:30
                CreateMatch("1", "200",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T11:30:00Z"),
                    25000),
                // Match 2: 11:00 - 12:30 (overlaps 10:00-11:30 from 11:00-11:30)
                CreateMatch("2", "201",
                    DateTimeOffset.Parse("2024-11-08T11:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:30:00Z"),
                    25000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().BeLessThan(2,
            "Trader should NOT be assigned to both matches that overlap (11:00-11:30) " +
            "even though they have different start times and M2Capacity=2.");
    }

    [Test]
    public void Assign_TwoSequentialMatches_NoOverlap_WithM2Capacity2_ShouldAssignBoth()
    {
        // Arrange - Matches that don't overlap (second starts after first ends)
        var trader = CreateTraderShift("1", "TraderC", new string[] { }, "First", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                // Match 1: 10:00 - 11:00
                CreateMatch("1", "300",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T11:00:00Z"),
                    20000),
                // Match 2: 11:00 - 12:00 (starts exactly when first ends - no overlap)
                CreateMatch("2", "301",
                    DateTimeOffset.Parse("2024-11-08T11:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    20000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2,
            "Trader SHOULD be assigned to both sequential matches since they don't overlap.");
        result.Assignments.Should().AllSatisfy(a => a.Hdp.Should().Be("TraderC"));
    }

    [Test]
    public void Assign_TwoMatches_SameStartTime_DifferentEndTime_WithM2Capacity2_ShouldAssignBoth()
    {
        // Arrange - Matches with SAME start time but different duration (this is allowed)
        var trader = CreateTraderShift("1", "TraderD", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            m2Capacity: 2);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { trader },
            Matches = new List<Match>
            {
                // Match 1: 10:00 - 11:30
                CreateMatch("1", "400",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T11:30:00Z"),
                    30000),
                // Match 2: 10:00 - 12:00 (same start, different end)
                CreateMatch("2", "401",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2,
            "Trader SHOULD be assigned to both matches with the SAME start time, " +
            "regardless of different end times. This is what M2Capacity=2 allows.");
        result.Assignments.Should().AllSatisfy(a => a.Hdp.Should().Be("TraderD"));
    }

    #endregion
}
