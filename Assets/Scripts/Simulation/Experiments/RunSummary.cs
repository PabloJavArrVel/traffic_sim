using System;
using System.Collections.Generic;

/// <summary>
/// The key numbers of one run, measured after the warm-up (the first minutes, while the city fills up, are left out).
/// Rides count when they were requested after the warm-up and finished before the end of the run.
/// Numbers that can't be measured (for example the waiting time when no ride was completed) are NaN, not 0.
/// </summary>
public class RunSummary
{
    public string Scenario;
    public int Seed;
    public int AmbientCars;
    public int Taxis;
    public int RebelCars;

    // Traffic
    public float AverageSpeedKmh;
    public float ShareStopped;
    public float ShareStoppedAtRedLight;
    public float ShareStoppedBehindCar;
    public float ShareStoppedAtJunction;
    public float KilometersPerVehiclePerHour;
    public int DetoursTaken;
    public float LongestStopSeconds;

    // Rules and safety
    public float ShareSpeeding;
    public int RedLightsRun;
    public int Collisions;
    public int CollisionsInvolvingRebels;
    public int CollisionsBetweenLawAbidingDrivers;   // should always be 0
    public int CollisionsInvolvingTaxis;

    // Taxi service
    public float ShareOfTaxisBusy;
    public int RidesFinished;
    public int RidesCompleted;
    public int RidesGivenUp;
    public float ShareGivenUp = float.NaN;
    public float AverageSecondsUntilPickup = float.NaN;
    public float Percentile90SecondsUntilPickup = float.NaN;
    public float AverageTripSeconds = float.NaN;

    /// <summary>Average number of stopped cars near each intersection (numbered as in the Check City Map report).</summary>
    public float[] AverageQueuePerIntersection;

    public static RunSummary From(ExperimentScenario scenario, int seed, World world)
    {
        float warmupEnd = scenario.WarmupMinutes * 60f;
        var summary = new RunSummary
        {
            Scenario = scenario.Name,
            Seed = seed,
            AmbientCars = scenario.AmbientCars,
            Taxis = scenario.Taxis,
            DetoursTaken = world.Statistics.DetoursTaken,
            LongestStopSeconds = world.Statistics.LongestStopSeconds
        };

        foreach (Vehicle vehicle in world.Vehicles)
            if (!vehicle.FollowsTrafficRules) summary.RebelCars++;

        AddTrafficNumbers(summary, world, warmupEnd);
        AddRideNumbers(summary, world.Metrics.Rides, warmupEnd);
        AddCollisionNumbers(summary, world.Metrics.Collisions, warmupEnd);
        return summary;
    }

    static void AddTrafficNumbers(RunSummary summary, World world, float warmupEnd)
    {
        var afterWarmup = new List<MetricsSample>();
        foreach (MetricsSample sample in world.Metrics.Samples)
            if (sample.Time > warmupEnd) afterWarmup.Add(sample);

        int intersections = world.Network.TrafficLightControllers.Count;
        summary.AverageQueuePerIntersection = new float[intersections];
        if (afterWarmup.Count == 0) return;

        float kilometers = 0f, seconds = 0f;
        foreach (MetricsSample sample in afterWarmup)
        {
            summary.AverageSpeedKmh += sample.AverageSpeedKmh / afterWarmup.Count;
            summary.ShareStopped += sample.ShareStopped / afterWarmup.Count;
            summary.ShareStoppedAtRedLight += sample.ShareStoppedAtRedLight / afterWarmup.Count;
            summary.ShareStoppedBehindCar += sample.ShareStoppedBehindCar / afterWarmup.Count;
            summary.ShareStoppedAtJunction += sample.ShareStoppedAtJunction / afterWarmup.Count;
            summary.ShareOfTaxisBusy += sample.ShareOfTaxisBusy / afterWarmup.Count;
            summary.ShareSpeeding += sample.ShareSpeeding / afterWarmup.Count;
            summary.RedLightsRun += sample.RedLightsRun;
            kilometers += sample.KilometersDriven;
            seconds += world.Metrics.SampleSeconds;
            for (int i = 0; i < intersections; i++)
                summary.AverageQueuePerIntersection[i] += sample.AverageQueuePerIntersection[i] / afterWarmup.Count;
        }

        float vehicleHours = world.Vehicles.Count * seconds / 3600f;
        summary.KilometersPerVehiclePerHour = vehicleHours > 0f ? kilometers / vehicleHours : 0f;
    }

    static void AddCollisionNumbers(RunSummary summary, IReadOnlyList<CollisionRecord> collisions, float warmupEnd)
    {
        foreach (CollisionRecord collision in collisions)
        {
            if (collision.Time < warmupEnd) continue;
            summary.Collisions++;
            if (collision.RebelInvolved) summary.CollisionsInvolvingRebels++;
            else summary.CollisionsBetweenLawAbidingDrivers++;
            if (collision.TaxiInvolved) summary.CollisionsInvolvingTaxis++;
        }
    }

    static void AddRideNumbers(RunSummary summary, IReadOnlyList<RideRecord> rides, float warmupEnd)
    {
        var pickupTimes = new List<float>();
        float tripSeconds = 0f;
        foreach (RideRecord ride in rides)
        {
            if (ride.RequestedAt < warmupEnd) continue;

            summary.RidesFinished++;
            if (ride.GaveUp)
            {
                summary.RidesGivenUp++;
                continue;
            }
            summary.RidesCompleted++;
            pickupTimes.Add(ride.SecondsUntilPickup);
            tripSeconds += ride.TripSeconds;
        }

        if (summary.RidesFinished > 0) summary.ShareGivenUp = (float)summary.RidesGivenUp / summary.RidesFinished;
        if (pickupTimes.Count == 0) return;

        pickupTimes.Sort();
        float total = 0f;
        foreach (float time in pickupTimes) total += time;
        summary.AverageSecondsUntilPickup = total / pickupTimes.Count;
        summary.Percentile90SecondsUntilPickup = pickupTimes[(int)Math.Floor(0.9f * (pickupTimes.Count - 1))];
        summary.AverageTripSeconds = tripSeconds / pickupTimes.Count;
    }
}
