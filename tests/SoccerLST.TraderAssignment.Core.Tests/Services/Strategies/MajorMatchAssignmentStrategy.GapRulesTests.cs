using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.MJ;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public class MajorMatchAssignmentStrategyGapRulesTests
{
    private MajorMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    #region PES5MIN Gap Rule Tests

    [Test]
    public void Assign_WithPES5MINJob_Requires15MinuteGap()
    {
        // Arrange - PES5MIN job ends at 10:00, MJ match starts at 10:10 (only 10 min gap)
        var traderWithPES5MIN = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES5MIN.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES5MIN",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES5MIN,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:10:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:10:00Z"),
                    100000, "EPL")
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
        // Arrange - PES5MIN job ends at 10:00, MJ match starts at 10:15 (exactly 15 min gap)
        var traderWithPES5MIN = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES5MIN.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES5MIN",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES5MIN,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:15:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:15:00Z"),
                    100000, "EPL")
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
    }

    [Test]
    public void Assign_WithPES5MINJob_AllowsAssignmentWithMoreThan15Minutes()
    {
        // Arrange - PES5MIN job ends at 10:00, MJ match starts at 10:20 (20 min gap)
        var traderWithPES5MIN = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES5MIN.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES5MIN",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES5MIN,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:20:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:20:00Z"),
                    100000, "EPL")
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
    public void Assign_WithMultiplePES5MINJobs_EnforcesGapForAll()
    {
        // Arrange - Trader has multiple PES5MIN jobs
        var traderWithMultiplePES5MIN = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithMultiplePES5MIN.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES5MIN",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T09:05:00Z")
            },
            new AssignedJob
            {
                Type = "PES5MIN",
                Start = DateTimeOffset.Parse("2025-09-28T11:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T11:05:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithMultiplePES5MIN,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T09:20:00Z"),
                    DateTimeOffset.Parse("2025-09-28T10:50:00Z"),
                    100000, "EPL")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Match doesn't conflict with PES5MIN jobs");
    }

    #endregion

    #region PES24 Gap Rule Tests

    [Test]
    public void Assign_WithPES24Job_Requires15MinuteGap()
    {
        // Arrange - PES24 job ends at 10:00, MJ match starts at 10:10 (only 10 min gap)
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES24.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES24",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES24,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:10:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:10:00Z"),
                    100000)
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
        // Arrange - PES24 job ends at 10:00, MJ match starts at 10:15 (exactly 15 min gap)
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES24.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES24",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES24,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:15:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:15:00Z"),
                    100000)
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
        // Arrange - PES24 job ends at 10:00, MJ match starts at 10:20 (20 min gap)
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES24.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES24",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES24,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:20:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:20:00Z"),
                    100000)
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
        // Arrange - PES24 job ends at 10:00, match starts at 10:14 (14 min gap - insufficient)
        var traderWithPES24 = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPES24.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES24",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPES24,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:14:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:14:00Z"),
                    100000)
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
        // Arrange - PKIF job ends at 10:00, MJ match starts at 10:10 (only 10 min gap)
        var traderWithPKIF = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPKIF.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PKIF",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPKIF,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:10:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:10:00Z"),
                    100000)
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
        // Arrange - PKIF job ends at 10:00, MJ match starts at 10:15 (exactly 15 min gap)
        var traderWithPKIF = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPKIF.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PKIF",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPKIF,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:15:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:15:00Z"),
                    100000)
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
        // Arrange - PKCF job ends at 10:00, MJ match starts at 10:10 (only 10 min gap)
        var traderWithPKCF = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPKCF.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PKCF",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPKCF,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:10:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:10:00Z"),
                    100000)
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
        // Arrange - PKCF job ends at 10:00, MJ match starts at 10:15 (exactly 15 min gap)
        var traderWithPKCF = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPKCF.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PKCF",
                Start = DateTimeOffset.Parse("2025-09-28T09:55:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPKCF,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:15:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:15:00Z"),
                    100000)
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
        // Arrange - PESCF job (not PES5MIN/PES24/PK*) ends at 10:00, MJ match starts at 10:05
        var traderWithPESCF = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithPESCF.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PESCF", // Different job type
                Start = DateTimeOffset.Parse("2025-09-28T09:50:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:00:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithPESCF,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:05:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:05:00Z"),
                    100000, "EPL")
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
    public void Assign_WithBothPES5MINAndPES24Jobs_EnforcesGapForBoth()
    {
        // Arrange - Trader has both PES5MIN and PES24 jobs
        var traderWithMixedJobs = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithMixedJobs.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PES5MIN",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T09:05:00Z")
            },
            new AssignedJob
            {
                Type = "PES24",
                Start = DateTimeOffset.Parse("2025-09-28T11:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T11:30:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithMixedJobs,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T09:20:00Z"),
                    DateTimeOffset.Parse("2025-09-28T10:50:00Z"),
                    100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Match doesn't conflict with gap rules");
    }

    [Test]
    public void Assign_WithMixedPKAndPESJobs_BlocksInsufficientGap()
    {
        // Arrange - Trader has PKIF and PES24 jobs
        var traderWithMixedJobs = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithMixedJobs.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob
            {
                Type = "PKIF",
                Start = DateTimeOffset.Parse("2025-09-28T09:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T09:05:00Z")
            },
            new AssignedJob
            {
                Type = "PES24",
                Start = DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                End = DateTimeOffset.Parse("2025-09-28T10:30:00Z")
            }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithMixedJobs,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T10:40:00Z"), // 10 min after PES24 ends
                    DateTimeOffset.Parse("2025-09-28T12:40:00Z"),
                    100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Insufficient gap after PES24 job");
    }

    [Test]
    public void Assign_WithAllPKAndPESJobs_EnforcesGapForAll()
    {
        // Arrange - Trader has all virtual job types
        var traderWithAllJobs = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false);
        traderWithAllJobs.AssignedJobs = new List<AssignedJob>
        {
            new AssignedJob { Type = "PES5MIN", Start = DateTimeOffset.Parse("2025-09-28T08:00:00Z"), End = DateTimeOffset.Parse("2025-09-28T08:05:00Z") },
            new AssignedJob { Type = "PES24", Start = DateTimeOffset.Parse("2025-09-28T08:30:00Z"), End = DateTimeOffset.Parse("2025-09-28T09:00:00Z") },
            new AssignedJob { Type = "PKIF", Start = DateTimeOffset.Parse("2025-09-28T11:00:00Z"), End = DateTimeOffset.Parse("2025-09-28T11:05:00Z") },
            new AssignedJob { Type = "PKCF", Start = DateTimeOffset.Parse("2025-09-28T13:00:00Z"), End = DateTimeOffset.Parse("2025-09-28T13:05:00Z") }
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                traderWithAllJobs,
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T09:15:00Z"), // 15 min after PES24
                    DateTimeOffset.Parse("2025-09-28T10:45:00Z"),
                    100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Match has sufficient gap after all jobs");
    }

    #endregion
}
