using System;

/// <summary>
/// A point (or a direction) on the city floor, in meters.
/// X grows to the east and Y grows to the south, like the columns and rows of the Excel map.
/// The center of cell A1 is (0, 0).
/// </summary>
public readonly struct MapPoint
{
    public readonly float X;
    public readonly float Y;

    public MapPoint(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float Length => MathF.Sqrt(X * X + Y * Y);

    public MapPoint Normalized
    {
        get
        {
            float length = Length;
            return length > 0f ? new MapPoint(X / length, Y / length) : new MapPoint(0f, 0f);
        }
    }

    public static float Dot(MapPoint a, MapPoint b) => a.X * b.X + a.Y * b.Y;
    public static float Distance(MapPoint a, MapPoint b) => (a - b).Length;

    public static MapPoint operator +(MapPoint a, MapPoint b) => new MapPoint(a.X + b.X, a.Y + b.Y);
    public static MapPoint operator -(MapPoint a, MapPoint b) => new MapPoint(a.X - b.X, a.Y - b.Y);
    public static MapPoint operator *(MapPoint point, float factor) => new MapPoint(point.X * factor, point.Y * factor);
    public static MapPoint operator *(float factor, MapPoint point) => point * factor;

    public override string ToString() => $"({X:F2}, {Y:F2})";
}

/// <summary>A position together with the direction something faces there.</summary>
public readonly struct MapPose
{
    public readonly MapPoint Position;
    public readonly MapPoint Forward;

    public MapPose(MapPoint position, MapPoint forward)
    {
        Position = position;
        Forward = forward;
    }
}
