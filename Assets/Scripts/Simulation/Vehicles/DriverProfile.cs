using System;

/// <summary>How someone likes to drive. Every car gets a slightly different profile so traffic looks natural.</summary>
public class DriverProfile
{
    public DriverProfile(float speedFactor, float maxAcceleration, float comfortableBraking, float timeGap)
    {
        SpeedFactor = speedFactor;
        MaxAcceleration = maxAcceleration;
        ComfortableBraking = comfortableBraking;
        TimeGap = timeGap;
    }

    /// <summary>1.0 = drives exactly at the speed limit, 0.9 = 10% slower.</summary>
    public float SpeedFactor { get; }

    /// <summary>How fast the driver speeds up, in m/s².</summary>
    public float MaxAcceleration { get; }

    /// <summary>How hard the driver likes to brake, in m/s².</summary>
    public float ComfortableBraking { get; }

    /// <summary>How many seconds behind the car ahead the driver likes to stay.</summary>
    public float TimeGap { get; }

    /// <summary>An everyday driver: somewhere between in a hurry and very calm.</summary>
    public static DriverProfile RandomDriver(Random random)
    {
        float calmness = (float)random.NextDouble();   // 0 = in a hurry, 1 = very calm
        return new DriverProfile(
            speedFactor: Blend(1.1f, 0.85f, calmness),
            maxAcceleration: Blend(2.5f, 1.5f, calmness),
            comfortableBraking: Blend(3.5f, 2.5f, calmness),
            timeGap: Blend(0.9f, 1.8f, calmness));
    }

    /// <summary>Taxis are autonomous: they drive smoothly and exactly at the speed limit.</summary>
    public static DriverProfile AutonomousTaxi()
    {
        return new DriverProfile(speedFactor: 1f, maxAcceleration: 2f, comfortableBraking: 3f, timeGap: 1.2f);
    }

    static float Blend(float from, float to, float amount) => from + (to - from) * amount;
}
