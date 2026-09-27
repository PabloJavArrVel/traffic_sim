using System.IO;
using NUnit.Framework;

public class ExperimentTests
{
    [Test]
    public void ReadsTheScenariosOfExperimentsXlsx()
    {
        var scenarios = ExperimentFileLoader.LoadScenarios();

        ExperimentScenario baseline = scenarios.Find(s => s.Name == "Baseline");
        ExperimentScenario doubleFleet = scenarios.Find(s => s.Name == "DoubleFleet");
        Assert.That(baseline, Is.Not.Null);
        Assert.That(doubleFleet.Taxis, Is.EqualTo(2 * baseline.Taxis));
        Assert.That(doubleFleet.AmbientCars, Is.EqualTo(baseline.AmbientCars), "empty cells keep the baseline value");
    }

    [Test]
    public void AShortRunMeasuresTrafficAndWritesTheResultFiles()
    {
        var scenario = new ExperimentScenario { Name = "Short", DurationMinutes = 3f, WarmupMinutes = 1f, SecondsBetweenPedestrians = 10f };
        CityMap map = CityMapLoader.LoadCityMap("CityMap.xlsx");

        ExperimentRun run = ExperimentRunner.Run(map, scenario, seed: 3);

        Assert.That(run.Metrics.Samples.Count, Is.EqualTo(18), "one sample every 10 s for 3 minutes");
        Assert.That(run.Summary.AverageSpeedKmh, Is.GreaterThan(5f).And.LessThan(40f));
        Assert.That(run.Summary.ShareStopped, Is.InRange(0f, 1f));
        Assert.That(run.Summary.AverageQueuePerIntersection.Length, Is.EqualTo(6));

        string folder = Path.Combine(Path.GetTempPath(), "traffic_sim_test_results");
        ExperimentResultsWriter.Write(folder, new System.Collections.Generic.List<ExperimentRun> { run });
        foreach (string file in new[] { "summary.csv", "samples.csv", "rides.csv", "intersections.csv" })
            Assert.That(File.Exists(Path.Combine(folder, file)), Is.True, file);
        Assert.That(File.ReadAllLines(Path.Combine(folder, "samples.csv")).Length, Is.EqualTo(19), "header + 18 samples");
    }
}
