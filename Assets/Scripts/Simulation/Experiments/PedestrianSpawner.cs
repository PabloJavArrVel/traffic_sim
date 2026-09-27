/// <summary>
/// Adds a new pedestrian every few seconds, as long as there are fewer than the maximum.
/// Used both when you press Play and when experiments run, so both behave the same.
/// </summary>
public class PedestrianSpawner
{
    readonly ExperimentScenario scenario;
    float secondsUntilNext;

    public PedestrianSpawner(ExperimentScenario scenario)
    {
        this.scenario = scenario;
        secondsUntilNext = scenario.SecondsBetweenPedestrians;
    }

    /// <summary>Call after every World.Tick. Returns the new pedestrian, or null if none was added.</summary>
    public Pedestrian Update(World world)
    {
        secondsUntilNext -= world.DeltaTime;
        if (secondsUntilNext > 0f) return null;

        secondsUntilNext = scenario.SecondsBetweenPedestrians;
        if (world.Pedestrians.Count >= scenario.MaxPedestrians) return null;
        return world.SpawnPedestrian(scenario.PedestrianPatienceSeconds);
    }
}
