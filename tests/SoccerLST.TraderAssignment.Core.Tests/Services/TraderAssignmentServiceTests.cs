using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services;
using SoccerLST.TraderAssignment.Core.Services.Strategies;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M1;
using SoccerLST.TraderAssignment.Core.Services.Strategies.M2;
using SoccerLST.TraderAssignment.Core.Services.Strategies.MJ;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services;

[TestFixture]
public class TraderAssignmentServiceTests
{
    private TraderAssignmentService _service;

    [SetUp]
    public void SetUp()
    {
        var strategies = new List<IMatchAssignmentStrategy>
        {
            new MajorMatchAssignmentStrategy(),
            new MinorOneMatchAssignmentStrategy(),
            new MinorTwoMatchAssignmentStrategy()
        };

        _service = new TraderAssignmentService(strategies);
    }

    [Test]
    public void Constructor_WithoutMJStrategy_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategies = new List<IMatchAssignmentStrategy>
        {
            new MinorOneMatchAssignmentStrategy() // Only M1, no MJ
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new TraderAssignmentService(strategies));
        exception.Message.Should().Contain("Major match strategy (MJ) not registered");
    }

    [Test]
    public void Constructor_WithoutM1Strategy_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategies = new List<IMatchAssignmentStrategy>
        {
            new MajorMatchAssignmentStrategy() // Only MJ, no M1
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new TraderAssignmentService(strategies));
        exception.Message.Should().Contain("Minor 1 match strategy (M1) not registered");
    }

    [Test]
    public void MajorMatchAssignments_DelegatesToMJStrategy()
    {
        // Arrange
        var request = new AssignmentRequest
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

        // Act
        var result = _service.MajorMatchAssignments(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be("Optimal");
        result.TotalMatches.Should().Be(1);
        result.AssignedMatches.Should().Be(1);
    }

    [Test]
    public void MinorOneMatchAssignments_DelegatesToM1Strategy()
    {
        // Arrange - M1: Single trader handles both HDP and OU
        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift>
            {
                CreateTraderShift("1", "TraderA", new string[] { }, "Member", false)
            },
            Matches = new List<Match>
            {
                CreateMatch("1", "100", DateTimeOffset.Parse("2025-09-28T10:00:00Z"), DateTimeOffset.Parse("2025-09-28T12:00:00Z"), 50000)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _service.MinorOneMatchAssignments(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be("Optimal");
        result.TotalMatches.Should().Be(1);
        result.AssignedMatches.Should().Be(1);
    }
}
