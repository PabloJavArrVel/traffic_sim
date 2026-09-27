/// <summary>
/// A way to drive from the center of one cell to the center of a neighbouring cell.
///
/// Drive links go to the cell ahead, left or right. Lane changes go one cell forward and one cell sideways
/// into a parallel lane; while changing lanes a car sweeps through a 2x2 block of cells, so a lane change
/// also remembers the other two cells of that block (the one beside the car and the one ahead of it).
/// </summary>
public class CellLink
{
    public CellLink(RoadCell from, RoadCell to, float length, Direction heading)
    {
        From = from;
        To = to;
        Length = length;
        Heading = heading;
    }

    public CellLink(RoadCell from, RoadCell to, float length, Direction heading, RoadCell cellBeside, RoadCell cellAhead)
        : this(from, to, length, heading)
    {
        CellBeside = cellBeside;
        CellAhead = cellAhead;
    }

    public RoadCell From { get; }
    public RoadCell To { get; }

    /// <summary>Meters between the two cell centers.</summary>
    public float Length { get; }

    /// <summary>The direction the car faces while driving this link.</summary>
    public Direction Heading { get; }

    /// <summary>For lane changes: the cell next to the car when it starts. Null for normal links.</summary>
    public RoadCell CellBeside { get; }

    /// <summary>For lane changes: the cell straight ahead of the car when it starts. Null for normal links.</summary>
    public RoadCell CellAhead { get; }

    public bool IsLaneChange => CellBeside != null;

    public override string ToString() => $"{From.Position} -> {To.Position}{(IsLaneChange ? " (lane change)" : "")}";
}
