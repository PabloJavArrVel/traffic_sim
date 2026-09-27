using System;
using System.Collections.Generic;

/// <summary>
/// The key numbers of one run, measured after the warm-up (the first minutes, while the city fills up, are left out).
/// Rides count when they were requested after the warm-up and finished before the end of the run.
/// </summary>
public class RunSummary
{
    public string Scenario;
    public int Seed;
    public int AmbientCars;
    public int Taxis;

    // Traffic
    public float AverageSpeedKmh;
    public float ShareStopped;
    public float ShareStoppedAtRedLight;
    public float ShareStoppedBehindCar;
    public float ShareStoppedAtJunction;
    public float KilometersPerVehiclePerHour;
    public int DetoursTaken;
    public float LongestStopSeconds;

    // Taxi service
    public float ShareOfTaxisBusy;
    public int RidesFinished;
    public int RidesCompleted;
    public int RidesGivenUp;
    public float ShareGivenUp;
    public float AverageSecondsUntilPickup;
    public float Percentile90SecondsUntilPickup;
    public float AverageTripSeconds;

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

        AddTrafficNumbers(summary, world, warmupEnd);
        AddRideNumbers(summary, world.Metrics.Rides, warmupEnd);
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
            kilometers += sample.KilometersDriven;
            seconds += world.Metrics.SampleSeconds;
            for (int i = 0; i < intersections; i++)
                summary.AverageQueuePerIntersection[i] += sample.AverageQueuePerIntersection[i] / afterWarmup.Count;
        }

        float vehicleHours = world.Vehicles.Count * seconds / 3600f;
        summary.KilometersPerVehiclePerHour = vehicleHours > 0f ? kilometers / vehicleHours : 0f;
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

        summary.ShareGivenUp = summary.RidesFinished > 0 ? (float)summary.RidesGivenUp / summary.RidesFinished : 0f;
        if (pickupTimes.Count == 0) return;

        pickupTimes.Sort();
        float total = 0f;
        foreach (float time in pickupTimes) total += time;
        summary.AverageSecondsUntilPickup = total / pickupTimes.Count;
        summary.Percentile90SecondsUntilPickup = pickupTimes[(int)Math.Floor(0.9f * (pickupTimes.Count - 1))];
        summary.AverageTripSeconds = tripSeconds / pickupTimes.Count;
    }
}
