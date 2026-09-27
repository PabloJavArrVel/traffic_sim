using System;
using System.Collections.Generic;

/// <summary>The result of running one scenario once (with one seed).</summary>
public class ExperimentRun
{
    public ExperimentScenario Scenario;
    public int Seed;
    public RoadNetwork Network;
    public SimulationMetrics Metrics;
    public RunSummary Summary;
}

/// <summary>
/// Runs scenarios without graphics, as fast as the computer can. Twenty minutes of city take about a second.
/// </summary>
public static class ExperimentRunner
{
    /// <summary>
    /// Runs a scenario once. 'reportProgress' (optional) gets a number from 0 to 1 from time to time and can return
    /// false to cancel; a cancelled run returns null.
    /// </summary>
    public static ExperimentRun Run(CityMap map, ExperimentScenario scenario, int seed, Func<float, bool> reportProgress = null)
    {
        SimulationSettings settings = scenario.CreateSettings();
        RoadNetwork network = RoadNetworkBuilder.Build(map, settings);
        var world = new World(network, settings, seed);
        scenario.Populate(world);
        var pedestrians = new PedestrianSpawner(scenario);

        int steps = (int)Math.Round(scenario.DurationMinutes * 60f / settings.SimulationStepSeconds);
        for (int step = 0; step < steps; step++)
        {
            world.Tick(settings.SimulationStepSeconds);
            pedestrians.Update(world);
            world.RemoveFinishedPedestrians();

            bool timeToReport = step % 2000 == 0;
            if (timeToReport && reportProgress != null && !reportProgress((float)step / steps)) return null;
        }

        return new ExperimentRun
        {
            Scenario = scenario,
            Seed = seed,
            Network = network,
            Metrics = world.Metrics,
            Summary = RunSummary.From(scenario, seed, world)
        };
    }

    /// <summary>
    /// Runs every scenario once per seed. 'reportProgress' gets a message and the overall progress (0 to 1) and can
    /// return false to cancel; the runs finished so far are returned.
    /// </summary>
    public static List<ExperimentRun> RunAll(CityMap map, List<ExperimentScenario> scenarios, Func<string, float, bool> reportProgress = null)
    {
        var runs = new List<ExperimentRun>();
        int totalRuns = 0;
        foreach (ExperimentScenario scenario in scenarios) totalRuns += scenario.Seeds;

        foreach (ExperimentScenario scenario in scenarios)
        {
            for (int i = 0; i < scenario.Seeds; i++)
            {
                int seed = scenario.FirstSeed + i;
                string message = $"{scenario.Name}, seed {seed} ({runs.Count + 1} of {totalRuns})";
                ExperimentRun run = Run(map, scenario, seed, progressInRun =>
                    reportProgress == null || reportProgress(message, (runs.Count + progressInRun) / totalRuns));

                if (run == null) return runs;   // cancelled
                runs.Add(run);
            }
        }
        return runs;
    }
}
