namespace SoccerLST.TraderAssignment.Core.Models;

public class MatchAssignment
{
    public string RowNumber { get; set; } = string.Empty;

    public string Match { get; set; } = string.Empty;

    public string League { get; set; } = string.Empty;

    public int MaxBet { get; set; }

    public string Time { get; set; } = string.Empty;

    public string Hdp { get; set; } = string.Empty;

    public string HdpLevel { get; set; } = string.Empty;

    public string Ou { get; set; } = string.Empty;

    public string OuLevel { get; set; } = string.Empty;

    public string PairType { get; set; } = string.Empty;

    public long Score { get; set; }
}
