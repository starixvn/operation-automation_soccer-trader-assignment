using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using SoccerLST.TraderAssignment.Core.Tests.Utils;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public class MajorMatchAssignmentStrategyTests
{
    private MajorMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    [Test]
    public void MatchType_ShouldBeMJ()
    {
        // Assert
        _strategy.MatchType.Should().Be("MJ");
    }

    [Test]
    public void ValidateMatches_WithValidMJMatches_ReturnsValid()
    {
        // Arrange
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000)
        };

        // Act
        var (isValid, errorMessage) = _strategy.ValidateMatches(matches);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Test]
    public void ValidateMatches_WithInvalidMaxBet_ReturnsInvalid()
    {
        // Arrange
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
        };

        // Act
        var (isValid, errorMessage) = _strategy.ValidateMatches(matches);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("MaxBet < 70,000");
    }

    [Test]
    public void Assign_WithValidRequest_ReturnsOptimalSolution()
    {
        // Arrange
        var request = CreateBasicAssignmentRequest();

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be("Optimal");
        result.TotalMatches.Should().Be(1);
        result.AssignedMatches.Should().Be(1);
        result.Assignments.Should().HaveCount(1);
    }

    [Test]
    public void Assign_WithMultipleMatches_AssignsAllMatches()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "TraderC", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "TraderD", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalMatches.Should().Be(2);
        result.AssignedMatches.Should().Be(2);
        result.Assignments.Should().HaveCount(2);
    }

    [Test]
    public void Assign_WithAlreadyHandledMatches_FiltersThemOut()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000, isAlreadyHandled: true),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 100000, isAlreadyHandled: false)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalMatches.Should().Be(1, "only matches with IsAlreadyHandled=false should be processed");
        result.AssignedMatches.Should().Be(1);
    }

    [Test]
    public void Assign_WithOverlappingMatches_AssignsNonOverlappingOnly()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T11:00:00Z"), DateTimeOffset.Parse("2025-09-28T13:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().BeLessThan(2, "traders cannot work overlapping matches");
    }

    [Test]
    public void Assign_WithTraderJobConflicts_RespectsAssignedJobs()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                new TraderShift
                {
                    Id = "1",
                    Name = "TraderA",
                    Start = DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    End = DateTimeOffset.Parse("2025-09-28T16:00:00Z"),
                    Skills = new List<string> { "HDP" },
                    Level = "Elite",
                    IsManager = false,
                    AssignedJobs = new List<AssignedJob>
                    {
                        new AssignedJob
                        {
                            Type = "PESCF",
                            Start = DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                            End = DateTimeOffset.Parse("2025-09-28T12:00:00Z")
                        }
                    }
                },
                CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "TraderC", new[] { "HDP" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderC", "TraderA has a job conflict, so TraderC should be assigned");
    }

    [Test]
    public void Assign_WithPreconfiguredPairs_PrioritizesThem()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "MemberHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "MemberOU", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000, "EPL")
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "EliteHDP",
                    Ou = "EliteOU",
                    Tier = "primary",
                    LeaguePriority = new Dictionary<int, List<string>>
                    {
                        { 1, new List<string> { "EPL" } }
                    }
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("EliteHDP");
        assignment.Ou.Should().Be("EliteOU");
        assignment.PairType.Should().Contain("Primary");
        assignment.Score.Should().BeGreaterThan(0);
    }

    [Test]
    public void Assign_WithNoAvailableTraders_ReturnsOptimalButUnassigned()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T14:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T16:00:00Z"))
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalMatches.Should().Be(1);
        result.AssignedMatches.Should().Be(0, "no traders available during match time");
    }

    [Test]
    public void Assign_WithMissingSkills_DoesNotAssignTrader()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "TraderB", new[] { "HDP" }, "Elite", false) // Both HDP, no OU
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "match needs both HDP and OU traders");
    }

    [Test]
    public void Assign_WithHigherMaxBet_PrioritizesMatch()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "EliteHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "EliteOU", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 70000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 200000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2);
        // Higher maxBet matches should get better traders (verified by score)
        var highBetMatch = result.Assignments.First(a => a.Match == "101");
        highBetMatch.HdpLevel.Should().Be("Elite");
        highBetMatch.OuLevel.Should().Be("Elite");
    }

    [Test]
    public void Assign_Summary_CalculatesCorrectly()
    {
        // Arrange
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "PrimaryHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "PrimaryOU", new[] { "OU" }, "Elite", false),
                CreateTraderShift("3", "BackupHDP", new[] { "HDP" }, "Member", false),
                CreateTraderShift("4", "BackupOU", new[] { "OU" }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000),
                CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T13:00:00Z"), DateTimeOffset.Parse("2025-09-28T15:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>
            {
                new PreconfiguredPair
                {
                    Hdp = "PrimaryHDP",
                    Ou = "PrimaryOU",
                    Tier = "primary",
                    LeaguePriority = null
                },
                new PreconfiguredPair
                {
                    Hdp = "BackupHDP",
                    Ou = "BackupOU",
                    Tier = "backup",
                    LeaguePriority = null
                }
            }
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Summary.Should().NotBeNull();
        result.Summary.PreconfiguredPairsUsed.Should().BeGreaterThan(0);
        result.Summary.PreconfiguredPairsUsed.Should().Be(
            result.Summary.PrimaryPairs +
            result.Summary.SecondaryPairs +
            result.Summary.BackupPairs);
    }

    #region EXPT Match Restriction Tests

    [Test]
    public void Assign_EXPTMatch_TraderCannotHandleEXPT_DoesNotAssign()
    {
        // Arrange - EXPT match with trader who cannot handle EXPT matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderHDP", new[] { "HDP" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false),  // Cannot handle EXPT
                CreateTraderShift("2", "TraderOU", new[] { "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false)   // Cannot handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000, isEXPT: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "both traders cannot handle EXPT matches");
    }

    [Test]
    public void Assign_EXPTMatch_TraderCanHandleEXPT_AssignsTrader()
    {
        // Arrange - EXPT match with trader who can handle EXPT matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderHDP", new[] { "HDP" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: true),   // Can handle EXPT
                CreateTraderShift("2", "TraderOU", new[] { "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: true)    // Can handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000, isEXPT: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "both traders can handle EXPT matches");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderHDP");
        assignment.Ou.Should().Be("TraderOU");
    }

    [Test]
    public void Assign_NonEXPTMatch_TraderCannotHandleEXPT_AssignsTrader()
    {
        // Arrange - Non-EXPT match with trader who cannot handle EXPT matches
        // Restriction should not apply to non-EXPT matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderHDP", new[] { "HDP" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false),  // Cannot handle EXPT
                CreateTraderShift("2", "TraderOU", new[] { "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false)   // Cannot handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000, isEXPT: false)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "EXPT restriction only applies to EXPT matches");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderHDP");
        assignment.Ou.Should().Be("TraderOU");
    }

    [Test]
    public void Assign_EXPTMatch_MixedTraders_AssignsOnlyEligibleTrader()
    {
        // Arrange - EXPT match with one trader who can handle EXPT and one who cannot
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderHDP", new[] { "HDP" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: true),   // Can handle EXPT
                CreateTraderShift("2", "TraderOU", new[] { "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"),
                    isEXPTMatchAssignable: false)   // Cannot handle EXPT
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000, isEXPT: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "HDP trader can handle EXPT but OU trader cannot - need both roles");
    }

    #endregion

    #region Overlap Constraint Tests

    [Test]
    public void Assign_TraderWithBothSkills_OverlappingMatches_CannotAssignSameTraderToBothMatches()
    {
        // Arrange - One trader with both HDP and OU skills, two overlapping matches
        // match1: 08:00 -> 10:00
        // match2: 09:30 -> 11:30 (overlaps with match1)
        // Expected: Trader cannot be assigned to both matches (even in different roles)
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderBothSkills", new[] { "HDP", "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"))
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    100000),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T09:30:00Z"),
                    DateTimeOffset.Parse("2025-09-28T11:30:00Z"),
                    100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0,
            "Cannot assign either match because MJ requires 2 different traders per match, " +
            "and the single trader cannot work overlapping matches");
    }

    [Test]
    public void Assign_MultipleTradersWithBothSkills_OverlappingMatches_AssignsBothWithDifferentTraders()
    {
        // Arrange - Four traders with both skills, two overlapping matches
        // Expected: Each match gets 2 different traders, no trader works both matches
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "Trader1", new[] { "HDP", "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z")),
                CreateTraderShift("2", "Trader2", new[] { "HDP", "OU" }, "Elite", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z")),
                CreateTraderShift("3", "Trader3", new[] { "HDP", "OU" }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z")),
                CreateTraderShift("4", "Trader4", new[] { "HDP", "OU" }, "Member", false,
                    start: DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    end: DateTimeOffset.Parse("2025-09-28T18:00:00Z"))
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2025-09-28T08:00:00Z"),
                    DateTimeOffset.Parse("2025-09-28T10:00:00Z"),
                    100000),
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2025-09-28T09:30:00Z"),
                    DateTimeOffset.Parse("2025-09-28T11:30:00Z"),
                    100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(2, "With 4 traders, both overlapping matches can be assigned");

        var match1 = result.Assignments.First(a => a.Match == "100");
        var match2 = result.Assignments.First(a => a.Match == "101");

        match1.Hdp.Should().NotBe("N/A");
        match1.Ou.Should().NotBe("N/A");
        match2.Hdp.Should().NotBe("N/A");
        match2.Ou.Should().NotBe("N/A");

        // Verify no trader works both matches
        var match1Traders = new[] { match1.Hdp, match1.Ou };
        var match2Traders = new[] { match2.Hdp, match2.Ou };

        match1Traders.Should().NotContain(t => match2Traders.Contains(t),
            "No trader should work in both overlapping matches");
    }

    #endregion

    // Helper methods
    private static AssignmentRequest CreateBasicAssignmentRequest()
    {
        return new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderHDP", new[] { "HDP" }, "Elite", false),
                CreateTraderShift("2", "TraderOU", new[] { "OU" }, "Elite", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 100000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };
    }

}
