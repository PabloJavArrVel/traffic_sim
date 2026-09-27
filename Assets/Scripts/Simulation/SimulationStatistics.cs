using System;

/// <summary>Numbers about how the simulation is going, for the HUD and the tests.</summary>
public class SimulationStatistics
{
    public int RidesCompleted;
    public int RidesGivenUp;

    /// <summary>How many times a stuck car took a detour.</summary>
    public int DetoursTaken;

    /// <summary>The longest time any car has been stuck (not counting red lights).</summary>
    public float LongestStopSeconds { get; private set; }

    public void RecordStop(float secondsStuck)
    {
        LongestStopSeconds = Math.Max(LongestStopSeconds, secondsStuck);
    }
}
