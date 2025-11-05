namespace SoccerLST.TraderAssignment.Core.Models;

public class AssignmentRequest
{
    public List<TraderShift> TraderShifts { get; set; } = new();

    public List<Match> Matches { get; set; } = new();

    public List<PreconfiguredPair> PreconfiguredPairs { get; set; } = new();
}
