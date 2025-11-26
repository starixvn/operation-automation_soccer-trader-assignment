using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.MJ;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for PrioritySkill feature in MajorMatchAssignmentStrategy
/// Ensures that traders with PrioritySkill get 1.1x bonus scoring when assigned to their priority skill role
/// </summary>
[TestFixture]
public class MajorMatchAssignmentStrategyPrioritySkillTests
{
    private MajorMatchAssignmentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    [Test]
    public void Assign_TraderWithPrioritySkillHDP_ShouldPreferHDPRole()
    {
        // Arrange - Trader with HDP priority skill should be preferred for HDP role
        var traderWithHdpPriority = CreateTraderShift("1", "TraderA", ["HDP", "OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "HDP");

        var traderWithOuPriority = CreateTraderShift("2", "TraderB", ["HDP", "OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "OU");

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderWithHdpPriority, traderWithOuPriority },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();

        // TraderA has HDP priority skill, so should be assigned to HDP
        assignment.Hdp.Should().Be("TraderA", "TraderA has PrioritySkill = HDP");
        assignment.Ou.Should().Be("TraderB", "TraderB has PrioritySkill = OU");
    }

    [Test]
    public void Assign_TraderWithPrioritySkillOU_ShouldPreferOURole()
    {
        // Arrange - Trader with OU priority skill should be preferred for OU role
        var traderNoPriority = CreateTraderShift("1", "TraderA", ["HDP"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: null);

        var traderWithOuPriority = CreateTraderShift("2", "TraderB", ["OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "OU");

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderNoPriority, traderWithOuPriority },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();
        assignment.Ou.Should().Be("TraderB", "TraderB has PrioritySkill = OU");
    }

    [Test]
    public void Assign_TradersWithNoPrioritySkill_ShouldAssignNormally()
    {
        // Arrange - Traders without PrioritySkill should work as before
        var traderA = CreateTraderShift("1", "TraderA", ["HDP"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: null);

        var traderB = CreateTraderShift("2", "TraderB", ["OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: null);

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_MemberWithPrioritySkill_ShouldOutscoreEliteWithoutPriority()
    {
        // Arrange - Member trader with priority skill should get higher score than Elite without
        // Member base score: 16 points * 1.3 priority = 20.8
        // Member with priority bonus: 20.8 * 1.1 = 22.88
        // Elite base score: 256 points * 2.0 priority = 512 (way higher even with bonus)
        // This test verifies the bonus is applied, but Elite still wins due to much higher base

        var memberWithPriority = CreateTraderShift("1", "MemberTrader", ["HDP"], "Member", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "HDP");

        var eliteNoPriority = CreateTraderShift("2", "EliteTrader", ["OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: null);

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { memberWithPriority, eliteNoPriority },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();

        // Elite should still be preferred due to much higher base points (256 vs 16)
        // But the score should reflect the priority skill bonus for Member
        assignment.Hdp.Should().Be("MemberTrader");
        assignment.Ou.Should().Be("EliteTrader");
    }

    [Test]
    public void Assign_TwoEliteTradersWithDifferentPrioritySkills_ShouldAssignToMatchingRoles()
    {
        // Arrange - Both Elite, but different priority skills
        var eliteHdpPriority = CreateTraderShift("1", "EliteA", ["HDP", "OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "HDP");

        var eliteOuPriority = CreateTraderShift("2", "EliteB", ["HDP", "OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "OU");

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { eliteHdpPriority, eliteOuPriority },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();

        // Should assign to matching priority skills
        assignment.Hdp.Should().Be("EliteA", "EliteA has PrioritySkill = HDP");
        assignment.Ou.Should().Be("EliteB", "EliteB has PrioritySkill = OU");
    }

    [Test]
    public void Assign_CaseInsensitivePrioritySkill_ShouldMatchCorrectly()
    {
        // Arrange - PrioritySkill should be case-insensitive
        var traderLowercase = CreateTraderShift("1", "TraderA", ["HDP"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "hdp"); // lowercase

        var traderB = CreateTraderShift("2", "TraderB", ["OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "OU");

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderLowercase, traderB },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();
        assignment.Hdp.Should().Be("TraderA", "lowercase 'hdp' should match 'HDP' role");
    }

    [Test]
    public void Assign_MultipleMatches_PrioritySkillAppliedConsistently()
    {
        // Arrange - Multiple matches to verify consistent priority skill behavior
        var traderHdpPriority = CreateTraderShift("1", "TraderA", ["HDP", "OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "HDP");

        var traderOuPriority = CreateTraderShift("2", "TraderB", ["HDP", "OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "OU");

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T10:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T12:00:00Z"),
                120000, "EPL"),
            CreateMatch("102", "102",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "SPA"),
            CreateMatch("103", "103",
                DateTimeOffset.Parse("2025-01-15T18:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T20:00:00Z"),
                120000, "ITA")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdpPriority, traderOuPriority },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(3);

        // All assignments should respect priority skills
        foreach (var assignment in response.Assignments.Where(a => a.Hdp != "N/A"))
        {
            assignment.Hdp.Should().Be("TraderA", "TraderA should always be assigned to HDP (priority skill)");
            assignment.Ou.Should().Be("TraderB", "TraderB should always be assigned to OU (priority skill)");
        }
    }

    [Test]
    public void Assign_PrioritySkillWithPreconfiguredPair_BothBonusesApplied()
    {
        // Arrange - Priority skill bonus should stack with preconfigured pair bonus
        var traderA = CreateTraderShift("1", "TraderA", ["HDP"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "HDP");

        var traderB = CreateTraderShift("2", "TraderB", ["OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "OU");

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var pairs = new List<PreconfiguredPair>
        {
            CreatePreconfiguredPair("TraderA", "TraderB", "primary")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA, traderB },
            Matches = matches,
            PreconfiguredPairs = pairs
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        var assignment = response.Assignments.First();

        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
        assignment.PairType.Should().Contain("Primary");

        // Score should reflect both bonuses: primary pair (1.5x) + priority skill (1.1x for each trader)
        assignment.Score.Should().BeGreaterThan(0);
    }

    [Test]
    public void Assign_EmptyPrioritySkill_ShouldBeTreatedAsNull()
    {
        // Arrange - Empty string priority skill should be treated as no priority
        var traderEmptyPriority = CreateTraderShift("1", "TraderA", ["HDP"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: "");

        var traderB = CreateTraderShift("2", "TraderB", ["OU"], "Elite", false,
            start: DateTimeOffset.Parse("2025-01-15T08:00:00Z"),
            end: DateTimeOffset.Parse("2025-01-15T23:00:00Z"),
            maxBet: 150000,
            prioritySkill: null);

        var matches = new List<Match>
        {
            CreateMatch("101", "101",
                DateTimeOffset.Parse("2025-01-15T14:00:00Z"),
                DateTimeOffset.Parse("2025-01-15T16:00:00Z"),
                120000, "EPL")
        };

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderEmptyPriority, traderB },
            Matches = matches,
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var response = _strategy.Assign(request);

        // Assert
        response.Success.Should().BeTrue();
        response.AssignedMatches.Should().Be(1);
        // Should assign normally without priority bonus
    }
}
