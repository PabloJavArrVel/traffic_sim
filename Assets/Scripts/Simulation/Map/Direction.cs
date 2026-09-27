/// <summary>The four directions traffic can flow in the city grid.</summary>
public enum Direction
{
    North,
    East,
    South,
    West
}

/// <summary>Helpers to move and turn around the grid.</summary>
public static class DirectionExtensions
{
    // Like in the Excel sheet: rows grow towards the south and columns grow towards the east.

    public static int RowStep(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return -1;
            case Direction.South: return 1;
            default: return 0;
        }
    }

    public static int ColumnStep(this Direction direction)
    {
        switch (direction)
        {
            case Direction.East: return 1;
            case Direction.West: return -1;
            default: return 0;
        }
    }

    public static Direction TurnLeft(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return Direction.West;
            case Direction.West: return Direction.South;
            case Direction.South: return Direction.East;
            default: return Direction.North;
        }
    }

    public static Direction TurnRight(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return Direction.East;
            case Direction.East: return Direction.South;
            case Direction.South: return Direction.West;
            default: return Direction.North;
        }
    }

    public static Direction Opposite(this Direction direction)
    {
        return direction.TurnLeft().TurnLeft();
    }

    public static bool IsEastWest(this Direction direction)
    {
        return direction == Direction.East || direction == Direction.West;
    }

    /// <summary>A one-meter step in this direction, in map coordinates (X grows east, Y grows south).</summary>
    public static MapPoint ToMapVector(this Direction direction)
    {
        return new MapPoint(direction.ColumnStep(), direction.RowStep());
    }

    /// <summary>The arrow used for this direction in the Excel map.</summary>
    public static string ToArrow(this Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return "^";
            case Direction.East: return ">";
            case Direction.South: return "v";
            default: return "<";
        }
    }
}
