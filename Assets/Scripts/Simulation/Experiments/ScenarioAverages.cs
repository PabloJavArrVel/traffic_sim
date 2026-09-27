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
            scenario.ShareGivenUp += s.ShareGivenUp;
            scenario.AverageSecondsUntilPickup += s.AverageSecondsUntilPickup;
            scenario.LongestStopSeconds += s.LongestStopSeconds;
        }

        foreach (ScenarioAverages scenario in averages)
        {
            scenario.AverageSpeedKmh /= scenario.Runs;
            scenario.ShareStopped /= scenario.Runs;
            scenario.ShareOfTaxisBusy /= scenario.Runs;
            scenario.RidesCompleted /= scenario.Runs;
            scenario.ShareGivenUp /= scenario.Runs;
            scenario.AverageSecondsUntilPickup /= scenario.Runs;
            scenario.LongestStopSeconds /= scenario.Runs;
        }
        return averages;
    }

    /// <summary>A plain-text table, for the Unity console or a terminal.</summary>
    public static string Table(List<ScenarioAverages> averages)
    {
        var text = new StringBuilder();
        text.AppendLine($"{"Scenario",-24}{"Runs",5}{"Speed km/h",12}{"Stopped",9}{"Taxis busy",12}{"Rides",7}{"Gave up",9}{"Wait s",8}{"Longest stop s",16}");
        foreach (ScenarioAverages a in averages)
        {
            text.AppendLine($"{a.Scenario,-24}{a.Runs,5}{a.AverageSpeedKmh,12:F1}{a.ShareStopped,9:P0}{a.ShareOfTaxisBusy,12:P0}" +
                            $"{a.RidesCompleted,7:F1}{a.ShareGivenUp,9:P0}{a.AverageSecondsUntilPickup,8:F0}{a.LongestStopSeconds,16:F0}");
        }
        return text.ToString();
    }
}
