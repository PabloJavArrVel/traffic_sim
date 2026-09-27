/// <summary>Why a car is slowing down or waiting.</summary>
public enum ObstacleReason
{
    None,
    CarAhead,
    RedLight,
    BusyJunction,
    BusyLane,
    EndOfRoute
}

/// <summary>The nearest thing a car must not drive past, and how fast that thing is moving.</summary>
public readonly struct Obstacle
{
    public readonly float Distance;
    public readonly float Speed;
    public readonly ObstacleReason Reason;

    public Obstacle(float distance, float speed, ObstacleReason reason)
    {
        Distance = distance;
        Speed = speed;
        Reason = reason;
    }

    public static Obstacle None => new Obstacle(float.PositiveInfinity, 0f, ObstacleReason.None);

    /// <summary>Something that isn't moving: a red light, a busy junction, the end of the route...</summary>
    public static Obstacle StopAt(float distance, ObstacleReason reason) => new Obstacle(distance, 0f, reason);

    public bool Exists => Reason != ObstacleReason.None;
}
