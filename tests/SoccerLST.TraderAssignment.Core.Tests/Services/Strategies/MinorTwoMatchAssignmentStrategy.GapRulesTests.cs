using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M2;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public partial class MinorTwoMatchAssignmentStrategyGapRulesTests
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

    #region PES5MIN Gap Rule Tests

    [Test]
    public void Assign_WithPES5MINJob_Requires15MinuteGap()
    {
        // Arrange - M2: Like MJ, requires 15-minute gap after PES5MIN
        var traderWithPES5MIN = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES5MIN", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES5MIN },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:10:00Z"), DateTimeOffset.Parse("2025-09-28T12:10:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "TraderA cannot be assigned due to insufficient gap after PES5MIN job");
    }

    [Test]
    public void Assign_WithPES5MINJob_AllowsAssignmentWith15MinuteGap()
    {
        // Arrange - M2: Exactly 15-minute gap is acceptable
        var traderWithPES5MIN = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES5MIN", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES5MIN },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:15:00Z"), DateTimeOffset.Parse("2025-09-28T12:15:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "TraderA can be assigned with exactly 15 minute gap");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderA");
    }

    [Test]
    public void Assign_WithPES5MINJob_AllowsAssignmentWithMoreThan15Minutes()
    {
        // Arrange - M2: More than 15 minutes is acceptable
        var traderWithPES5MIN = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES5MIN", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES5MIN },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:20:00Z"), DateTimeOffset.Parse("2025-09-28T12:20:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "TraderA can be assigned with 20 minute gap");
    }

    #endregion

    #region PES24 Gap Rule Tests

    [Test]
    public void Assign_WithPES24Job_Requires15MinuteGap()
    {
        // Arrange - M2: Like MJ, requires 15-minute gap after PES24
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES24", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES24 },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:10:00Z"), DateTimeOffset.Parse("2025-09-28T12:10:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "TraderA cannot be assigned due to insufficient gap after PES24 job");
    }

    [Test]
    public void Assign_WithPES24Job_AllowsAssignmentWith15MinuteGap()
    {
        // Arrange - M2: Exactly 15-minute gap is acceptable
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES24", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES24 },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:15:00Z"), DateTimeOffset.Parse("2025-09-28T12:15:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "TraderA can be assigned with exactly 15 minute gap");
    }

    [Test]
    public void Assign_WithPES24Job_AllowsAssignmentWithMoreThan15Minutes()
    {
        // Arrange - M2: More than 15 minutes is acceptable
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES24", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES24 },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:20:00Z"), DateTimeOffset.Parse("2025-09-28T12:20:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "TraderA can be assigned with 20 minute gap");
    }

    [Test]
    public void Assign_WithPES24TooClose_BlocksAssignment()
    {
        // Arrange - M2: 14 minutes is insufficient
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new string[] { }, "Member", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES24", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPES24 },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:14:00Z"), DateTimeOffset.Parse("2025-09-28T12:14:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "14 minute gap is insufficient");
    }

    #endregion

    #region PK (PKIF/PKCF) Gap Rule Tests

    [Test]
    public void Assign_WithPKIFJob_Requires15MinuteGap()
    {
        // Arrange - M2: Like MJ, requires 15-minute gap after PKIF
        var traderWithPKIF = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PKIF", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPKIF },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:10:00Z"), DateTimeOffset.Parse("2025-09-28T12:10:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "TraderA cannot be assigned due to insufficient gap after PKIF job");
    }

    [Test]
    public void Assign_WithPKIFJob_AllowsAssignmentWith15MinuteGap()
    {
        // Arrange - M2: Exactly 15-minute gap is acceptable
        var traderWithPKIF = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PKIF", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPKIF },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:15:00Z"), DateTimeOffset.Parse("2025-09-28T12:15:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "TraderA can be assigned with exactly 15 minute gap");
    }

    [Test]
    public void Assign_WithPKCFJob_Requires15MinuteGap()
    {
        // Arrange - M2: Like MJ, requires 15-minute gap after PKCF
        var traderWithPKCF = CreateTraderShift("1", "TraderA", new string[] { }, "First", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PKCF", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPKCF },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:10:00Z"), DateTimeOffset.Parse("2025-09-28T12:10:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "TraderA cannot be assigned due to insufficient gap after PKCF job");
    }

    [Test]
    public void Assign_WithPKCFJob_AllowsAssignmentWith15MinuteGap()
    {
        // Arrange - M2: Exactly 15-minute gap is acceptable
        var traderWithPKCF = CreateTraderShift("1", "TraderA", new string[] { }, "First", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PKCF", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:00:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPKCF },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:15:00Z"), DateTimeOffset.Parse("2025-09-28T12:15:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "TraderA can be assigned with exactly 15 minute gap");
    }

    #endregion

    #region Mixed Job Type Tests

    [Test]
    public void Assign_WithNonPESNonPKJob_NoGapRequired()
    {
        // Arrange - M2: Non-virtual job types don't require gap
        var traderWithPESCF = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PESCF", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:05:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithPESCF },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:05:00Z"), DateTimeOffset.Parse("2025-09-28T12:05:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Non-PES5MIN/PES24/PK jobs don't require 15 minute gap");
    }

    [Test]
    public void Assign_WithMultiplePES5MINJobs_EnforcesGapForAll()
    {
        // Arrange - M2: Gap rule applies to all PES5MIN jobs
        var traderWithMultiplePES5MIN = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PES5MIN", DateTimeOffset.Parse("2025-09-28T09:00:00Z"), DateTimeOffset.Parse("2025-09-28T09:05:00Z")),
                CreateAssignedJob("PES5MIN", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T11:05:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMultiplePES5MIN },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T09:20:00Z"), DateTimeOffset.Parse("2025-09-28T10:50:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Match doesn't conflict with PES5MIN gap rules");
    }

    [Test]
    public void Assign_WithMixedPKAndPESJobs_BlocksInsufficientGap()
    {
        // Arrange - M2: Gap rule applies to all virtual job types
        var traderWithMixedJobs = CreateTraderShift("1", "TraderA", new string[] { }, "Elite", false,
            start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
            m2Capacity: 1,
            assignedJobs: new List<AssignedJob>
            {
                CreateAssignedJob("PKIF", DateTimeOffset.Parse("2025-09-28T08:00:00Z"), DateTimeOffset.Parse("2025-09-28T09:00:00Z")),
                CreateAssignedJob("PES24", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T10:30:00Z"))
            });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithMixedJobs },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:40:00Z"), DateTimeOffset.Parse("2025-09-28T12:40:00Z"), 30000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Insufficient gap after PES24 job");
    }

    #endregion
}
