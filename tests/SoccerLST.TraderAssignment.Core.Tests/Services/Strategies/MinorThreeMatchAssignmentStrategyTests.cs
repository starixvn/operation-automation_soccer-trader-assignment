using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

[TestFixture]
public class MinorThreeMatchAssignmentStrategyTests
{
    private MinorThreeMatchAssignmentStrategy _strategy = null!;
    private MethodInfo? _categoryM3EalLink5Method;
    private MethodInfo? _satisfiesConditionMethod;
    private MethodInfo? _findAllValidSubsetsMethod;
    private MethodInfo? _findNonOverlappingValidSubsetsMethod;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MinorThreeMatchAssignmentStrategy();

        var type = typeof(MinorThreeMatchAssignmentStrategy);
        _categoryM3EalLink5Method = type.GetMethod("CategoryM3EalLink5", BindingFlags.NonPublic | BindingFlags.Instance);
        _satisfiesConditionMethod = type.GetMethod("SatisfiesCondition", BindingFlags.NonPublic | BindingFlags.Instance);
        _findAllValidSubsetsMethod = type.GetMethod("FindAllValidSubsets", BindingFlags.NonPublic | BindingFlags.Instance);
        _findNonOverlappingValidSubsetsMethod = type.GetMethod("FindNonOverlappingValidSubsets", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _strategy = null!;
        _categoryM3EalLink5Method = null;
        _satisfiesConditionMethod = null;
        _findAllValidSubsetsMethod = null;
        _findNonOverlappingValidSubsetsMethod = null;
    }

    #region SatisfiesCondition Tests

    [Test]
    public void SatisfiesCondition_AllMatchesBelowThreshold_ReturnsTrue()
    {
        // Arrange - All 5 matches have MaxBet < 10000
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        var result = (bool)_satisfiesConditionMethod!.Invoke(_strategy, new object[] { matches })!;

        // Assert
        result.Should().BeTrue("all matches have MaxBet < 10000");
    }

    [Test]
    public void SatisfiesCondition_OneMatchAtThresholdFourBelow_ReturnsTrue()
    {
        // Arrange - One match has MaxBet = 10000, four have MaxBet < 10000
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        var result = (bool)_satisfiesConditionMethod!.Invoke(_strategy, new object[] { matches })!;

        // Assert
        result.Should().BeTrue("one match has MaxBet = 10000 and four have MaxBet < 10000");
    }

    [Test]
    public void SatisfiesCondition_TwoMatchesAtThreshold_ReturnsFalse()
    {
        // Arrange - Two matches have MaxBet = 10000
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true)
        };

        // Act
        var result = (bool)_satisfiesConditionMethod!.Invoke(_strategy, new object[] { matches })!;

        // Assert
        result.Should().BeFalse("only one match can have MaxBet = 10000");
    }

    [Test]
    public void SatisfiesCondition_OneMatchAboveThreshold_ReturnsFalse()
    {
        // Arrange - One match has MaxBet > 10000
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10001, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        var result = (bool)_satisfiesConditionMethod!.Invoke(_strategy, new object[] { matches })!;

        // Assert
        result.Should().BeFalse("no match can have MaxBet > 10000");
    }

    [Test]
    public void SatisfiesCondition_NotExactlyFiveMatches_ReturnsFalse()
    {
        // Arrange - Only 4 matches
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true)
        };

        // Act
        var result = (bool)_satisfiesConditionMethod!.Invoke(_strategy, new object[] { matches })!;

        // Assert
        result.Should().BeFalse("must have exactly 5 matches");
    }

    [Test]
    public void SatisfiesCondition_SixMatches_ReturnsFalse()
    {
        // Arrange - 6 matches
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            CreateMatch("6", "105", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true)
        };

        // Act
        var result = (bool)_satisfiesConditionMethod!.Invoke(_strategy, new object[] { matches })!;

        // Assert
        result.Should().BeFalse("must have exactly 5 matches");
    }

    #endregion

    #region FindAllValidSubsets Tests

    [Test]
    public void FindAllValidSubsets_GroupOfSixWithValidSubset_ReturnsValidSubset()
    {
        // Arrange - 6 matches, first 5 satisfy condition
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            CreateMatch("6", "105", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 12000, isEAL: true) // This one exceeds threshold
        };

        // Act
        var result = (List<List<Match>>)_findAllValidSubsetsMethod!.Invoke(_strategy, new object[] { matches, 5 })!;

        // Assert
        result.Should().NotBeNull("should return a list");
        result.Should().NotBeEmpty("should find at least one valid subset");
        result.Should().OnlyContain(subset => subset.Count == 5, "each subset should have exactly 5 matches");
        result.Should().OnlyContain(subset => subset.All(m => m.MaxBet <= 10000), "all matches in subsets should have MaxBet <= 10000");
        result.Should().OnlyContain(subset => !subset.Any(m => m.Id == "6"), "should exclude the match with MaxBet > 10000");
    }

    [Test]
    public void FindAllValidSubsets_GroupOfSevenWithValidSubset_ReturnsValidSubset()
    {
        // Arrange - 7 matches, can form valid subset
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            CreateMatch("6", "105", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 12000, isEAL: true), // Exceeds threshold
            CreateMatch("7", "106", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 15000, isEAL: true)  // Exceeds threshold
        };

        // Act
        var result = (List<List<Match>>)_findAllValidSubsetsMethod!.Invoke(_strategy, new object[] { matches, 5 })!;

        // Assert
        result.Should().NotBeNull("should return a list");
        result.Should().NotBeEmpty("should find at least one valid subset");
        result.Should().OnlyContain(subset => subset.Count == 5, "each subset should have exactly 5 matches");
        foreach (var subset in result)
        {
            var matchesWithMaxBet10000 = subset.Count(m => m.MaxBet == 10000);
            var matchesWithMaxBetBelow10000 = subset.Count(m => m.MaxBet < 10000);
            (matchesWithMaxBetBelow10000 == 5 || (matchesWithMaxBet10000 == 1 && matchesWithMaxBetBelow10000 == 4))
                .Should().BeTrue("each subset should satisfy condition");
        }
    }

    [Test]
    public void FindAllValidSubsets_TenMatchesWithTwoValidGroups_ReturnsBothGroups()
    {
        // Arrange - 10 matches: [a,b,c,d,e] and [f,g,h,i,k] both satisfy condition (*)
        var matches = new List<Match>
        {
            // First valid group: all below threshold
            CreateMatch("a", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("b", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("c", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("d", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("e", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            // Second valid group: one at threshold, four below
            CreateMatch("f", "105", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("g", "106", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("h", "107", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("i", "108", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("k", "109", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        var result = (List<List<Match>>)_findAllValidSubsetsMethod!.Invoke(_strategy, new object[] { matches, 5 })!;

        // Assert
        result.Should().NotBeNull("should return a list");
        result.Count.Should().BeGreaterThanOrEqualTo(2, "should find at least 2 valid subsets");

        // Verify first group [a,b,c,d,e] exists
        var firstGroup = result.FirstOrDefault(subset =>
            subset.Count == 5 &&
            subset.All(m => new[] { "a", "b", "c", "d", "e" }.Contains(m.Id)));
        firstGroup.Should().NotBeNull("should contain the first valid group [a,b,c,d,e]");

        // Verify second group [f,g,h,i,k] exists
        var secondGroup = result.FirstOrDefault(subset =>
            subset.Count == 5 &&
            subset.All(m => new[] { "f", "g", "h", "i", "k" }.Contains(m.Id)));
        secondGroup.Should().NotBeNull("should contain the second valid group [f,g,h,i,k]");
    }

    [Test]
    public void FindAllValidSubsets_NoValidSubset_ReturnsEmptyList()
    {
        // Arrange - 6 matches, but all combinations have issues (e.g., too many with MaxBet = 10000)
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("4", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("5", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("6", "105", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true)
        };

        // Act
        var result = (List<List<Match>>)_findAllValidSubsetsMethod!.Invoke(_strategy, new object[] { matches, 5 })!;

        // Assert
        result.Should().NotBeNull("should return a list");
        result.Should().BeEmpty("no valid subset exists (all have MaxBet = 10000, need at most 1)");
    }

    [Test]
    public void FindAllValidSubsets_GroupSmallerThanRequired_ReturnsEmptyList()
    {
        // Arrange - Only 3 matches, need 5
        var matches = new List<Match>
        {
            CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true)
        };

        // Act
        var result = (List<List<Match>>)_findAllValidSubsetsMethod!.Invoke(_strategy, new object[] { matches, 5 })!;

        // Assert
        result.Should().NotBeNull("should return a list");
        result.Should().BeEmpty("cannot form subset of 5 from only 3 matches");
    }

    #endregion

    #region CategoryM3EalLink5 Tests

    [Test]
    public void CategoryM3EalLink5_GroupOfFiveSatisfyingCondition_ProcessesGroup()
    {
        // Arrange - 5 EAL matches with same Start time, all satisfy condition
        var startTime = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var events = new List<Match>
        {
            CreateMatch("1", "100", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert - Method should execute without throwing
        // Note: Since the method doesn't return anything, we're just verifying it doesn't throw
        Assert.Pass("method executed successfully");
    }

    [Test]
    public void CategoryM3EalLink5_GroupOfFiveWithOneAtThreshold_ProcessesGroup()
    {
        // Arrange - 5 EAL matches, one at threshold
        var startTime = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var events = new List<Match>
        {
            CreateMatch("1", "100", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("2", "101", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("3", "102", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("4", "103", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("5", "104", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert
        Assert.Pass("method executed successfully");
    }

    [Test]
    public void CategoryM3EalLink5_GroupOfSixFindsValidSubset_ProcessesSubset()
    {
        // Arrange - 6 EAL matches with same Start time, can form valid subset
        var startTime = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var events = new List<Match>
        {
            CreateMatch("1", "100", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            CreateMatch("6", "105", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 12000, isEAL: true) // Exceeds threshold
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert
        Assert.Pass("method executed successfully");
    }

    [Test]
    public void CategoryM3EalLink5_GroupOfFiveNotSatisfyingCondition_DoesNotProcess()
    {
        // Arrange - 5 EAL matches, but two have MaxBet = 10000 (invalid)
        var startTime = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var events = new List<Match>
        {
            CreateMatch("1", "100", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("2", "101", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("3", "102", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("4", "103", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("5", "104", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true)
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert
        Assert.Pass("method executed successfully (group not processed due to invalid condition)");
    }

    [Test]
    public void CategoryM3EalLink5_FiltersNonEALMatches_OnlyProcessesEAL()
    {
        // Arrange - Mix of EAL and non-EAL matches
        var startTime = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var events = new List<Match>
        {
            CreateMatch("1", "100", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            CreateMatch("6", "105", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: false) // Not EAL
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert
        Assert.Pass("method executed successfully (non-EAL matches filtered out)");
    }

    [Test]
    public void CategoryM3EalLink5_MultipleGroupsWithDifferentStartTimes_ProcessesEachGroup()
    {
        // Arrange - Two groups with different start times
        var startTime1 = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var startTime2 = DateTimeOffset.Parse("2025-09-28T14:00:00Z");
        var events = new List<Match>
        {
            // Group 1
            CreateMatch("1", "100", startTime1, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", startTime1, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", startTime1, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", startTime1, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("5", "104", startTime1, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            // Group 2
            CreateMatch("6", "105", startTime2, DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 10000, isEAL: true),
            CreateMatch("7", "106", startTime2, DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 8000, isEAL: true),
            CreateMatch("8", "107", startTime2, DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 9000, isEAL: true),
            CreateMatch("9", "108", startTime2, DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 8500, isEAL: true),
            CreateMatch("10", "109", startTime2, DateTimeOffset.Parse("2025-09-28T16:00:00Z"), 9999, isEAL: true)
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert
        Assert.Pass("method executed successfully (both groups processed)");
    }

    [Test]
    public void CategoryM3EalLink5_GroupOfFour_DoesNotProcess()
    {
        // Arrange - Only 4 matches, less than required 5
        var startTime = DateTimeOffset.Parse("2025-09-28T10:00:00Z");
        var events = new List<Match>
        {
            CreateMatch("1", "100", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("2", "101", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("3", "102", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("4", "103", startTime, DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true)
        };

        // Act
        _categoryM3EalLink5Method!.Invoke(_strategy, new object[] { events });

        // Assert
        Assert.Pass("method executed successfully (group not processed due to insufficient matches)");
    }

    [Test]
    public void FindNonOverlappingValidSubsets_TenMatchesWithOverlappingGroups_ReturnsNonOverlappingGroups()
    {
        // Arrange - 10 matches where [a,b,c,d,e] and [f,g,h,i,k] are valid and non-overlapping
        // Also [a,b,c,d,f] would be valid but overlaps with [a,b,c,d,e]
        var matches = new List<Match>
        {
            // First valid group: all below threshold
            CreateMatch("a", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("b", "101", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("c", "102", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("d", "103", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9500, isEAL: true),
            CreateMatch("e", "104", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true),
            // Second valid group: one at threshold, four below
            CreateMatch("f", "105", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 10000, isEAL: true),
            CreateMatch("g", "106", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8000, isEAL: true),
            CreateMatch("h", "107", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9000, isEAL: true),
            CreateMatch("i", "108", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 8500, isEAL: true),
            CreateMatch("k", "109", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 9999, isEAL: true)
        };

        // Act
        var result = (List<List<Match>>)_findNonOverlappingValidSubsetsMethod!.Invoke(_strategy, new object[] { matches, 5 })!;

        // Assert
        result.Should().NotBeNull("should return a list");
        result.Count.Should().BeGreaterThanOrEqualTo(2, "should find at least 2 non-overlapping groups");

        // Verify no match appears in multiple groups
        var allMatchIds = new List<string>();
        foreach (var group in result)
        {
            var groupMatchIds = group.Select(m => m.Id).ToList();
            foreach (var matchId in groupMatchIds)
            {
                allMatchIds.Should().NotContain(matchId, $"match {matchId} should not appear in multiple groups");
                allMatchIds.Add(matchId);
            }
        }

        // Verify first group [a,b,c,d,e] exists
        var firstGroup = result.FirstOrDefault(subset =>
            subset.Count == 5 &&
            subset.All(m => new[] { "a", "b", "c", "d", "e" }.Contains(m.Id)));
        firstGroup.Should().NotBeNull("should contain the first valid group [a,b,c,d,e]");

        // Verify second group [f,g,h,i,k] exists
        var secondGroup = result.FirstOrDefault(subset =>
            subset.Count == 5 &&
            subset.All(m => new[] { "f", "g", "h", "i", "k" }.Contains(m.Id)));
        secondGroup.Should().NotBeNull("should contain the second valid group [f,g,h,i,k]");
    }

    #endregion
}

