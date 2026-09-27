using NUnit.Framework;

/// <summary>
/// Long runs on the real city, like pressing Play and watching for a while, but without graphics.
/// They check, the whole time, that no two cars overlap, and at the end that traffic kept flowing and taxis did rides.
/// </summary>
public class CitySoakTests
{
    static readonly float Step = new SimulationSettings().SimulationStepSeconds;

    [Test]
    public void TenMinutesOfCityTrafficWithoutOverlapsOrJams()
    {
        World world = CreateCity(randomSeed: 12345, ambientCars: 30, taxis: 3);
        float secondsUntilNextPedestrian = 0f;

        int steps = (int)(10f * 60f / Step);
        for (int step = 0; step < steps; step++)
        {
            world.Tick(Step);

            secondsUntilNextPedestrian -= Step;
            if (secondsUntilNextPedestrian <= 0f && world.Pedestrians.Count < 5)
            {
                world.SpawnPedestrian(patienceSeconds: 120f);
                secondsUntilNextPedestrian = 20f;
            }
            world.RemoveFinishedPedestrians();

            TrafficChecks.AssertCarsHoldTheCellsUnderThem(world);
            if (step % 5 == 0) TrafficChecks.AssertNoCarsOverlap(world);
        }

        // Queues at red lights are normal (up to a few light cycles), gridlock is not: every car keeps moving.
        foreach (Vehicle vehicle in world.Vehicles)
            Assert.That(vehicle.DistanceDriven, Is.GreaterThan(800f), $"{vehicle.GetType().Name} {vehicle.Id} kept driving");
        Assert.That(world.Statistics.LongestStopSeconds, Is.LessThan(180f), "no car was stuck for three minutes");
        Assert.That(world.Statistics.RidesCompleted, Is.GreaterThanOrEqualTo(5), "taxis completed rides");

        // Everybody follows the rules in this city: no crashes, no red lights run, nobody speeding.
        Assert.That(world.Metrics.Collisions, Is.Empty);
        foreach (MetricsSample sample in world.Metrics.Samples)
        {
            Assert.That(sample.RedLightsRun, Is.Zero, $"red light run at {sample.Time}s");
            Assert.That(sample.ShareSpeeding, Is.Zero, $"speeding at {sample.Time}s");
        }
    }

    [Test]
    public void ATaxiPicksUpAPassengerAndDropsThemOff()
    {
        World world = CreateCity(randomSeed: 99, ambientCars: 10, taxis: 1);
        Pedestrian passenger = world.SpawnPedestrian(patienceSeconds: 600f);

        for (int step = 0; step < (int)(300f / Step) && passenger.State != PedestrianState.Arrived; step++)
            world.Tick(Step);

        Assert.That(passenger.State, Is.EqualTo(PedestrianState.Arrived));
        Assert.That(world.FleetManager.Taxis[0].RidesCompleted, Is.EqualTo(1));
    }

    [Test]
    public void TheSameSeedGivesTheSameTraffic()
    {
        World first = CreateCity(randomSeed: 5, ambientCars: 20, taxis: 2);
        World second = CreateCity(randomSeed: 5, ambientCars: 20, taxis: 2);

        for (int step = 0; step < 600; step++)
        {
            first.Tick(Step);
            second.Tick(Step);
        }

        for (int i = 0; i < first.Vehicles.Count; i++)
            Assert.That(first.Vehicles[i].DistanceDriven, Is.EqualTo(second.Vehicles[i].DistanceDriven).Within(0.0001f));
    }

    static World CreateCity(int randomSeed, int ambientCars, int taxis)
    {
        var world = new World(TestMaps.LoadRealCity(), new SimulationSettings(), randomSeed);
        for (int i = 0; i < ambientCars; i++) Assert.That(world.SpawnAmbientCar(), Is.Not.Null);
        for (int i = 0; i < taxis; i++) Assert.That(world.SpawnTaxi(), Is.Not.Null);
        return world;
    }
}
