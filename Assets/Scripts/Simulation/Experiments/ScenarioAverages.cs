using System.Collections.Generic;
using System.Text;

/// <summary>The key numbers of one scenario, averaged over all its seeds. Used for the tables in Unity and the log.</summary>
public class ScenarioAverages
{
    public string Scenario;
    public int Runs;
    public float AverageSpeedKmh;
    public float ShareStopped;
    public float ShareOfTaxisBusy;
    public float RidesCompleted;
    public float ShareGivenUp;
    public float AverageSecondsUntilPickup;
    public float LongestStopSeconds;
    public float Collisions;
    public float RedLightsRun;

    int runsWithFinishedRides;
    int runsWithPickups;

    /// <summary>One entry per scenario, in the order the scenarios first appear.</summary>
    public static List<ScenarioAverages> From(List<ExperimentRun> runs)
    {
        var averages = new List<ScenarioAverages>();
        var byName = new Dictionary<string, ScenarioAverages>();
        foreach (ExperimentRun run in runs)
        {
            if (!byName.TryGetValue(run.Scenario.Name, out ScenarioAverages scenario))
            {
                scenario = new ScenarioAverages { Scenario = run.Scenario.Name };
                byName[run.Scenario.Name] = scenario;
                averages.Add(scenario);
            }

            RunSummary s = run.Summary;
            scenario.Runs++;
            scenario.AverageSpeedKmh += s.AverageSpeedKmh;
            scenario.ShareStopped += s.ShareStopped;
            scenario.ShareOfTaxisBusy += s.ShareOfTaxisBusy;
            scenario.RidesCompleted += s.RidesCompleted;
            scenario.LongestStopSeconds += s.LongestStopSeconds;

            // Only runs where these could be measured count for their average.
            if (!float.IsNaN(s.ShareGivenUp)) { scenario.ShareGivenUp += s.ShareGivenUp; scenario.runsWithFinishedRides++; }
            if (!float.IsNaN(s.AverageSecondsUntilPickup)) { scenario.AverageSecondsUntilPickup += s.AverageSecondsUntilPickup; scenario.runsWithPickups++; }
            scenario.Collisions += s.Collisions;
            scenario.RedLightsRun += s.RedLightsRun;
        }

        foreach (ScenarioAverages scenario in averages)
        {
            scenario.AverageSpeedKmh /= scenario.Runs;
            scenario.ShareStopped /= scenario.Runs;
            scenario.ShareOfTaxisBusy /= scenario.Runs;
            scenario.RidesCompleted /= scenario.Runs;
            scenario.ShareGivenUp = scenario.runsWithFinishedRides > 0 ? scenario.ShareGivenUp / scenario.runsWithFinishedRides : float.NaN;
            scenario.AverageSecondsUntilPickup = scenario.runsWithPickups > 0 ? scenario.AverageSecondsUntilPickup / scenario.runsWithPickups : float.NaN;
            scenario.LongestStopSeconds /= scenario.Runs;
            scenario.Collisions /= scenario.Runs;
            scenario.RedLightsRun /= scenario.Runs;
        }
        return averages;
    }

    /// <summary>A plain-text table, for the Unity console or a terminal.</summary>
    public static string Table(List<ScenarioAverages> averages)
    {
        var text = new StringBuilder();
        text.AppendLine($"{"Scenario",-24}{"Runs",5}{"Speed km/h",12}{"Stopped",9}{"Taxis busy",12}{"Rides",7}{"Gave up",9}{"Wait s",8}{"Longest stop s",16}{"Crashes",9}{"Reds run",10}");
        foreach (ScenarioAverages a in averages)
        {
            text.AppendLine($"{a.Scenario,-24}{a.Runs,5}{a.AverageSpeedKmh,12:F1}{a.ShareStopped,9:P0}{a.ShareOfTaxisBusy,12:P0}" +
                            $"{a.RidesCompleted,7:F1}{a.ShareGivenUp,9:P0}{a.AverageSecondsUntilPickup,8:F0}{a.LongestStopSeconds,16:F0}" +
                            $"{a.Collisions,9:F1}{a.RedLightsRun,10:F1}");
        }
        return text.ToString();
    }
}
