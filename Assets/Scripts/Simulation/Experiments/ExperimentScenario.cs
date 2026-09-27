/// <summary>
/// Everything you can change in one experiment: how many cars and taxis, how many passengers, what kind of drivers,
/// traffic light timing, and how long and how many times to run it. One row of Experiments.xlsx is one scenario;
/// the defaults below are the baseline city.
/// </summary>
public class ExperimentScenario
{
    public string Name = "Baseline";

    // Traffic
    public int AmbientCars = 30;
    public int Taxis = 3;

    // Passengers
    public int InitialPedestrians = 3;
    public int MaxPedestrians = 5;
    public float SecondsBetweenPedestrians = 30f;
    public float PedestrianPatienceSeconds = 120f;

    // Drivers: calmness 0 = in a hurry, 1 = very calm (see DriverProfile). Rebels ignore every traffic rule.
    public float DriverCalmnessMin = 0f;
    public float DriverCalmnessMax = 1f;
    public float RebelShare = 0f;

    // Streets
    public float StreetSpeedLimitKmh = 30f;
    public float AvenueSpeedLimitKmh = 50f;
    public float GreenLightSeconds = 15f;

    // How to run it
    public float DurationMinutes = 20f;
    public float WarmupMinutes = 2f;   // the first minutes are left out of the summary (the city starts empty)
    public int Seeds = 5;              // how many times to repeat it, each with a different random seed
    public int FirstSeed = 1;

    public SimulationSettings CreateSettings()
    {
        return new SimulationSettings
        {
            DriverCalmnessMin = DriverCalmnessMin,
            DriverCalmnessMax = DriverCalmnessMax,
            RebelShare = RebelShare,
            StreetSpeedLimitKmh = StreetSpeedLimitKmh,
            AvenueSpeedLimitKmh = AvenueSpeedLimitKmh,
            GreenLightSeconds = GreenLightSeconds
        };
    }

    /// <summary>Adds the scenario's cars, taxis and first pedestrians to a new world.</summary>
    public void Populate(World world)
    {
        for (int i = 0; i < AmbientCars; i++) world.SpawnAmbientCar();
        for (int i = 0; i < Taxis; i++) world.SpawnTaxi();
        for (int i = 0; i < InitialPedestrians; i++) world.SpawnPedestrian(PedestrianPatienceSeconds);
    }
}
