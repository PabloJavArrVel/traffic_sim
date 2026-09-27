using System;

/// <summary>
/// The Intelligent Driver Model (IDM), a standard formula for how hard a driver speeds up or brakes:
///
///     acceleration = a * (1 - (v / v0)^4 - (s* / s)^2)
///     s*           = s0 + v * T + v * (v - vObstacle) / (2 * sqrt(a * b))
///
///   v   current speed               v0  speed the driver wants
///   s   free space to the obstacle  s*  space the driver would like to have
///   a   max acceleration            b   comfortable braking
///   T   time gap                    s0  space to keep when stopped
///
/// With nothing ahead the car speeds up to v0; with something ahead it slows down smoothly so it stops s0 before it.
/// </summary>
public static class CarFollowing
{
    /// <summary>Stronger braking than this is never used (an emergency stop).</summary>
    const float HardestBraking = 8f;

    public static float Acceleration(float speed, float desiredSpeed, Obstacle obstacle, float stoppedGap, DriverProfile driver)
    {
        float speedRatio = speed / Math.Max(desiredSpeed, 0.1f);
        float freeRoadTerm = 1f - speedRatio * speedRatio * speedRatio * speedRatio;

        float obstacleTerm = 0f;
        if (obstacle.Exists)
        {
            float closingSpeed = speed - obstacle.Speed;
            float brakingReaction = speed * closingSpeed / (2f * MathF.Sqrt(driver.MaxAcceleration * driver.ComfortableBraking));
            float wantedSpace = stoppedGap + Math.Max(0f, speed * driver.TimeGap + brakingReaction);
            float space = Math.Max(obstacle.Distance, 0.01f);
            obstacleTerm = (wantedSpace / space) * (wantedSpace / space);
        }

        float acceleration = driver.MaxAcceleration * (freeRoadTerm - obstacleTerm);
        return Math.Max(acceleration, -HardestBraking);
    }
}
