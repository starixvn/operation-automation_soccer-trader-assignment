namespace SoccerLST.TraderAssignment.Core.Models;

public class AssignedJob
{
    public string Type { get; set; } = string.Empty;

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }
}
