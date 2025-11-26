using FluentAssertions;
using NUnit.Framework;
using SoccerLST.TraderAssignment.Core.Models;
using SoccerLST.TraderAssignment.Core.Services.Strategies.MJ;
using static SoccerLST.TraderAssignment.Core.Tests.Utils.TestHelpers;

namespace SoccerLST.TraderAssignment.Core.Tests.Services.Strategies;

/// <summary>
/// Tests for MJ (Major) solo match functionality
/// Solo matches are defined as matches where there is no other match in the same league
/// that starts within 45 minutes before or after the match start time
/// </summary>
[TestFixture]
public class MajorMatchAssignmentStrategySoloMatchTests
{
    private MajorMatchAssignmentStrategy _strategy = null!;

    [SetUp]
    public void SetUp()
    {
        _strategy = new MajorMatchAssignmentStrategy();
    }

    [TearDown]
    public void TearDown()
    {
        _strategy = null!;
    }

    [Test]
    public void Assign_NonSoloMatch_TwoTradersWithNoRestrictions_ShouldAssignBoth()
    {
        // Arrange - 2 traders with HDP and OU skills, no NonSoloLeagues restrictions
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Non-solo match in La Liga
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ESL1",
                    leagueName: "SPAIN LA LIGA",
                    isSoloMatch: false)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Non-solo match should be assigned to both traders");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_SoloMatch_TwoTradersWithNoRestrictions_ShouldAssignBoth()
    {
        // Arrange - 2 traders with HDP and OU skills, no NonSoloLeagues restrictions
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in EPL
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Solo match should be assigned when traders have no NonSoloLeagues restrictions");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_SoloMatch_TradersRestrictedFromDifferentLeague_ShouldAssignBoth()
    {
        // Arrange - Traders have NonSoloLeagues restriction for EPL, but match is in La Liga
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in La Liga (not restricted)
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ESL1",
                    leagueName: "SPAIN LA LIGA",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Solo match should be assigned when match league is not in traders' NonSoloLeagues list");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_SoloMatch_TradersRestrictedFromMatchLeague_ShouldNotAssign()
    {
        // Arrange - Traders have NonSoloLeagues restriction for EPL, and match is in EPL
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in EPL (restricted league)
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Solo match should NOT be assigned when match league is in traders' NonSoloLeagues list");
        result.Assignments.Should().HaveCount(1, "Match should appear in response");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("N/A");
        assignment.Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_NonSoloMatch_TradersRestrictedFromMatchLeague_ShouldAssign()
    {
        // Arrange - Traders have NonSoloLeagues restriction for EPL, but match is NOT solo
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Non-solo match in EPL (same league but not solo)
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: false)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Non-solo match should be assigned even if league is in NonSoloLeagues list");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_SoloMatch_LeagueNameContainsNonSoloLeague_ShouldNotAssign()
    {
        // Arrange - Traders have NonSoloLeagues restriction for "PREMIER LEAGUE", match is "ENGLAND PREMIER LEAGUE"
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "PREMIER LEAGUE" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "PREMIER LEAGUE" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match - leagueName contains "PREMIER LEAGUE"
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Solo match should NOT be assigned when leagueName contains a NonSoloLeagues value");
        result.Assignments.Should().HaveCount(1, "Match should appear in response");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("N/A");
        assignment.Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_SoloMatch_CaseInsensitiveLeagueMatch_ShouldNotAssign()
    {
        // Arrange - Traders have NonSoloLeagues in lowercase, match leagueName is uppercase
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "premier league" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "premier league" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match - case insensitive match
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Solo match should NOT be assigned with case-insensitive league matching");
        result.Assignments.Should().HaveCount(1, "Match should appear in response");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("N/A");
        assignment.Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_TwoSoloMatches_OneRestrictedOneNot_ShouldAssignOnlyUnrestricted()
    {
        // Arrange - Traders restricted from EPL, 2 solo matches (EPL and La Liga)
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in EPL (restricted)
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true),
                // Solo match in La Liga (not restricted)
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2024-11-08T14:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T16:00:00Z"),
                    70000,
                    leagueCode: "ESL1",
                    leagueName: "SPAIN LA LIGA",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Only the unrestricted solo match should be assigned");
        result.Assignments.Should().HaveCount(2);

        var eplMatch = result.Assignments.First(a => a.RowNumber == "1");
        eplMatch.Hdp.Should().Be("N/A");
        eplMatch.Ou.Should().Be("N/A");

        var laLigaMatch = result.Assignments.First(a => a.RowNumber == "2");
        laLigaMatch.Hdp.Should().Be("TraderA");
        laLigaMatch.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_SoloMatch_OnlyHdpTraderRestricted_ShouldNotAssign()
    {
        // Arrange - Only HDP trader restricted from EPL solo matches
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in EPL
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Match cannot be assigned because HDP trader is restricted (requires both HDP and OU)");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("N/A");
        assignment.Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_MultipleNonSoloLeagues_MatchInOneOfThem_ShouldNotAssign()
    {
        // Arrange - Traders restricted from multiple leagues
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE", "SPAIN LA LIGA", "GERMANY BUNDESLIGA" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE", "SPAIN LA LIGA", "GERMANY BUNDESLIGA" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in Bundesliga (one of restricted leagues)
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "GBL1",
                    leagueName: "GERMANY BUNDESLIGA",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Solo match should NOT be assigned when league is in traders' NonSoloLeagues list");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("N/A");
        assignment.Ou.Should().Be("N/A");
    }

    [Test]
    public void Assign_EmptyNonSoloLeagues_SoloMatch_ShouldAssign()
    {
        // Arrange - Traders with empty NonSoloLeagues list (not null, but empty)
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string>());

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string>());

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in EPL
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1, "Solo match should be assigned when NonSoloLeagues list is empty");
        var assignment = result.Assignments.First();
        assignment.Hdp.Should().Be("TraderA");
        assignment.Ou.Should().Be("TraderB");
    }

    [Test]
    public void Assign_TwoSoloMatches_BothRestricted_ShouldNotAssignEither()
    {
        // Arrange - Traders restricted from EPL and La Liga
        var traderHdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE", "SPAIN LA LIGA" });

        var traderOu = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE", "SPAIN LA LIGA" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderHdp, traderOu },
            Matches = new List<Match>
            {
                // Solo match in EPL (restricted)
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true),
                // Solo match in La Liga (also restricted)
                CreateMatch("2", "101",
                    DateTimeOffset.Parse("2024-11-08T14:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T16:00:00Z"),
                    70000,
                    leagueCode: "ESL1",
                    leagueName: "SPAIN LA LIGA",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(0, "Both solo matches should NOT be assigned because both leagues are restricted");
        result.Assignments.Should().HaveCount(2);
        result.Assignments.Should().AllSatisfy(a =>
        {
            a.Hdp.Should().Be("N/A");
            a.Ou.Should().Be("N/A");
        });
    }

    [Test]
    public void Assign_SoloMatch_TradersWithDifferentRestrictions_ShouldAssignOnlyUnrestrictedTraders()
    {
        // Arrange - 4 traders with different NonSoloLeagues restrictions
        // TraderA (HDP): Restricted from EPL
        var traderA_Hdp = CreateTraderShift("1", "TraderA", new[] { "HDP" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "ENGLAND PREMIER LEAGUE" });

        // TraderB (OU): No restrictions
        var traderB_Ou = CreateTraderShift("2", "TraderB", new[] { "OU" }, "Elite", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        // TraderC (HDP): No restrictions
        var traderC_Hdp = CreateTraderShift("3", "TraderC", new[] { "HDP" }, "Member", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: null);

        // TraderD (OU): Restricted from La Liga
        var traderD_Ou = CreateTraderShift("4", "TraderD", new[] { "OU" }, "Member", false,
            start: DateTimeOffset.Parse("2024-11-08T08:00:00Z"),
            end: DateTimeOffset.Parse("2024-11-08T18:00:00Z"),
            nonSoloLeagues: new List<string> { "SPAIN LA LIGA" });

        var request = new AssignmentRequest
        {
            TraderShifts = new List<TraderShift> { traderA_Hdp, traderB_Ou, traderC_Hdp, traderD_Ou },
            Matches = new List<Match>
            {
                // Solo match in EPL
                CreateMatch("1", "100",
                    DateTimeOffset.Parse("2024-11-08T10:00:00Z"),
                    DateTimeOffset.Parse("2024-11-08T12:00:00Z"),
                    70000,
                    leagueCode: "ENPR",
                    leagueName: "ENGLAND PREMIER LEAGUE",
                    isSoloMatch: true)
            },
            PreconfiguredPairs = new List<PreconfiguredPair>()
        };

        // Act
        var result = _strategy.Assign(request);

        // Assert
        result.Success.Should().BeTrue();
        result.AssignedMatches.Should().Be(1,
            "Match should be assigned using TraderC (HDP, unrestricted) and TraderB (OU, unrestricted)");
        var assignment = result.Assignments.First();

        // Should assign unrestricted traders: TraderC (HDP) and TraderB (OU)
        // Should NOT assign TraderA (HDP restricted from EPL)
        assignment.Hdp.Should().Be("TraderC", "TraderC is HDP trader with no EPL restriction");
        assignment.Ou.Should().Be("TraderB", "TraderB is OU trader with no EPL restriction");
    }
}
