using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Saves experiment results as CSV files (one folder per batch), ready for analysis/analyze.py:
///   summary.csv        one row per run: the key numbers (RunSummary)
///   samples.csv        one row per run and time interval: how the traffic evolved (MetricsSample)
///   rides.csv          one row per finished ride request (RideRecord)
///   intersections.csv  one row per run and intersection: its average queue
/// </summary>
public static class ExperimentResultsWriter
{
    public static void Write(string folder, List<ExperimentRun> runs)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "summary.csv"), SummaryCsv(runs));
        File.WriteAllText(Path.Combine(folder, "samples.csv"), SamplesCsv(runs));
        File.WriteAllText(Path.Combine(folder, "rides.csv"), RidesCsv(runs));
        File.WriteAllText(Path.Combine(folder, "intersections.csv"), IntersectionsCsv(runs));
    }

    static string SummaryCsv(List<ExperimentRun> runs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("scenario,seed,ambient_cars,taxis,average_speed_kmh,share_stopped,share_stopped_at_red_light," +
                       "share_stopped_behind_car,share_stopped_at_junction,km_per_vehicle_per_hour,detours_taken," +
                       "longest_stop_seconds,share_of_taxis_busy,rides_finished,rides_completed,rides_given_up," +
                       "share_given_up,average_seconds_until_pickup,p90_seconds_until_pickup,average_trip_seconds");
        foreach (ExperimentRun run in runs)
        {
            RunSummary s = run.Summary;
            csv.AppendLine(Row(s.Scenario, s.Seed, s.AmbientCars, s.Taxis, s.AverageSpeedKmh, s.ShareStopped,
                s.ShareStoppedAtRedLight, s.ShareStoppedBehindCar, s.ShareStoppedAtJunction, s.KilometersPerVehiclePerHour,
                s.DetoursTaken, s.LongestStopSeconds, s.ShareOfTaxisBusy, s.RidesFinished, s.RidesCompleted,
                s.RidesGivenUp, s.ShareGivenUp, s.AverageSecondsUntilPickup, s.Percentile90SecondsUntilPickup,
                s.AverageTripSeconds));
        }
        return csv.ToString();
    }

    static string SamplesCsv(List<ExperimentRun> runs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("scenario,seed,time_seconds,average_speed_kmh,share_stopped,share_stopped_at_red_light," +
                       "share_stopped_behind_car,share_stopped_at_junction,km_driven,share_of_taxis_busy,pedestrians_waiting");
        foreach (ExperimentRun run in runs)
        {
            foreach (MetricsSample m in run.Metrics.Samples)
            {
                csv.AppendLine(Row(run.Scenario.Name, run.Seed, m.Time, m.AverageSpeedKmh, m.ShareStopped,
                    m.ShareStoppedAtRedLight, m.ShareStoppedBehindCar, m.ShareStoppedAtJunction, m.KilometersDriven,
                    m.ShareOfTaxisBusy, m.PedestriansWaiting));
            }
        }
        return csv.ToString();
    }

    static string RidesCsv(List<ExperimentRun> runs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("scenario,seed,pedestrian,requested_at,seconds_until_taxi_assigned,seconds_until_pickup,trip_seconds,gave_up");
        foreach (ExperimentRun run in runs)
        {
            foreach (RideRecord ride in run.Metrics.Rides)
            {
                csv.AppendLine(Row(run.Scenario.Name, run.Seed, ride.PedestrianId, ride.RequestedAt,
                    ride.SecondsUntilTaxiAssigned, ride.SecondsUntilPickup, ride.TripSeconds, ride.GaveUp ? 1 : 0));
            }
        }
        return csv.ToString();
    }

    static string IntersectionsCsv(List<ExperimentRun> runs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("scenario,seed,intersection,lights,average_queue");
        foreach (ExperimentRun run in runs)
        {
            IReadOnlyList<TrafficLightController> intersections = run.Network.TrafficLightControllers;
            for (int i = 0; i < intersections.Count; i++)
            {
                csv.AppendLine(Row(run.Scenario.Name, run.Seed, i + 1, CityMapValidator.LightCellsOf(intersections[i]),
                    run.Summary.AverageQueuePerIntersection[i]));
            }
        }
        return csv.ToString();
    }

    /// <summary>One CSV line. Numbers use a dot for decimals; empty cells for "not available" (NaN).</summary>
    static string Row(params object[] values)
    {
        var cells = new List<string>();
        foreach (object value in values)
        {
            if (value is float number)
                cells.Add(float.IsNaN(number) ? "" : number.ToString("0.####", CultureInfo.InvariantCulture));
            else if (value is string text)
                cells.Add(text.Contains(",") ? $"\"{text}\"" : text);
            else
                cells.Add(System.Convert.ToString(value, CultureInfo.InvariantCulture));
        }
        return string.Join(",", cells);
    }
}
