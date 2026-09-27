using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
/// Runs scenarios without graphics, as fast as the computer can: twenty minutes of city take a few seconds per core.
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
    /// Runs every scenario once per seed, several runs at the same time (one per processor core): every run has its
    /// own World, so they don't affect each other. The results come back in the order of the scenarios and seeds.
    ///
    /// 'reportProgress' (optional) is called from the calling thread with a message and the overall progress (0 to 1),
    /// and can return false to cancel; the runs that finished are returned.
    /// </summary>
    public static List<ExperimentRun> RunAll(CityMap map, List<ExperimentScenario> scenarios, Func<string, float, bool> reportProgress = null)
    {
        var jobs = new List<(ExperimentScenario scenario, int seed)>();
        foreach (ExperimentScenario scenario in scenarios)
            for (int i = 0; i < scenario.Seeds; i++)
                jobs.Add((scenario, scenario.FirstSeed + i));

        var results = new ExperimentRun[jobs.Count];
        int finishedRuns = 0;
        var cancel = new CancellationTokenSource();

        Task allRuns = Task.Run(() => Parallel.For(0, jobs.Count, jobIndex =>
        {
            if (cancel.IsCancellationRequested) return;
            (ExperimentScenario scenario, int seed) = jobs[jobIndex];
            results[jobIndex] = Run(map, scenario, seed, _ => !cancel.IsCancellationRequested);
            Interlocked.Increment(ref finishedRuns);
        }));

        // While the runs work, this thread reports progress (Unity only lets the main thread draw a progress bar).
        while (!allRuns.Wait(200))
        {
            string message = $"{finishedRuns} of {jobs.Count} runs finished, using {Environment.ProcessorCount} cores";
            bool keepGoing = reportProgress == null || reportProgress(message, (float)finishedRuns / jobs.Count);
            if (!keepGoing) cancel.Cancel();
        }

        var finished = new List<ExperimentRun>();
        foreach (ExperimentRun run in results)
            if (run != null) finished.Add(run);   // cancelled runs are left out
        return finished;
    }
}
