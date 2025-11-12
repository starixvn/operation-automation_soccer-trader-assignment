namespace SoccerLST.TraderAssignment.Core.Models;

public class Match
{
    public string Id { get; set; } = string.Empty;

    public string MatchNo { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }

    public int MaxBet { get; set; }

    public string? LeagueCode { get; set; }

    public string? LeagueName { get; set; }

    public string? BetType { get; set; }

    public bool IsSoloMatch { get; set; } = false;

    public bool IsUseAlternativeMJRule { get; set; } = false;

    public bool IsAlreadyHandled { get; set; } = false;

    public bool IsEXPT { get; set; } = false;

    public bool IsEAL { get; set; } = false;
}
