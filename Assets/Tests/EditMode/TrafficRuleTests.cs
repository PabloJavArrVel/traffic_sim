using NUnit.Framework;

/// <summary>Speed limits, right of way, and what happens when some drivers ignore the rules.</summary>
public class TrafficRuleTests
{
    const float Step = 0.05f;

    [Test]
    public void AvenuesAreFasterThanStreetsAndRoundaboutsAreSlow()
    {
        RoadNetwork city = TestMaps.LoadRealCity();
        var settings = new SimulationSettings();

        Assert.That(TestMaps.Cell(city, "X5").SpeedLimit, Is.EqualTo(settings.AvenueSpeedLimit), "North-South main: two lanes");
        Assert.That(TestMaps.Cell(city, "D6").SpeedLimit, Is.EqualTo(settings.StreetSpeedLimit), "HP-1: one lane");
        Assert.That(TestMaps.Cell(city, "F13").IsInRoundabout, Is.True);
        Assert.That(TestMaps.Cell(city, "F13").SpeedLimit, Is.EqualTo(settings.StreetSpeedLimit), "two lanes, but inside the roundabout");
    }

    [Test]
    public void CarsGoingStraightGoBeforeCarsTurningIn()
    {
        // A loop, driven clockwise, with a side street coming down into C3.
        RoadNetwork network = TestMaps.Build(
            "X X v X X",
            "X X v X X",
            "> > > > v",
            "^ X X X v",
            "^ < < < <");
        var world = new World(network, new SimulationSettings(), randomSeed: 2);

        // Both cars are the same distance from C3. The side-street car is added first, so it decides first.
        Vehicle sideStreetCar = world.SpawnAmbientCarAt(TestMaps.Cell(network, "C1"), TestMaps.Cell(network, "C2"));
        Vehicle mainStreetCar = world.SpawnAmbientCarAt(TestMaps.Cell(network, "A3"), TestMaps.Cell(network, "B3"));

        RoadCell junction = TestMaps.Cell(network, "C3");
        Vehicle firstIn = RunUntilSomeoneHolds(world, junction);
        Assert.That(firstIn, Is.SameAs(mainStreetCar), "the car going straight has the right of way");
        Assert.That(sideStreetCar.WaitingFor, Is.EqualTo(ObstacleReason.BusyJunction));
    }

    [Test]
    public void CarsInTheRoundaboutGoBeforeCarsEnteringIt()
    {
        // A small roundabout around the island at C4, driven clockwise, with a street coming down into D3.
        RoadNetwork network = TestMaps.Build(
            "X X X v X",
            "X X X v X",
            "X > > v X",
            "X ^ R v X",
            "X ^ < < X");
        var world = new World(network, new SimulationSettings(), randomSeed: 2);

        Vehicle enteringCar = world.SpawnAmbientCarAt(TestMaps.Cell(network, "D1"), TestMaps.Cell(network, "D2"));
        Vehicle carInTheRoundabout = world.SpawnAmbientCarAt(TestMaps.Cell(network, "B3"), TestMaps.Cell(network, "C3"));

        Vehicle firstIn = RunUntilSomeoneHolds(world, TestMaps.Cell(network, "D3"));
        Assert.That(firstIn, Is.SameAs(carInTheRoundabout), "the car already in the roundabout goes first");
        Assert.That(enteringCar.WaitingFor, Is.EqualTo(ObstacleReason.BusyJunction));
    }

    [Test]
    public void OnlyRebelsCrashRunRedLightsOrSpeed()
    {
        var settings = new SimulationSettings { RebelShare = 0.3f };
        var world = new World(TestMaps.LoadRealCity(), settings, randomSeed: 21);
        for (int i = 0; i < 30; i++) world.SpawnAmbientCar();
        for (int i = 0; i < 3; i++) world.SpawnTaxi();

        int redLightsRun = 0;
        for (int step = 0; step < (int)(10f * 60f / Step); step++)
        {
            world.Tick(Step);
            foreach (Vehicle vehicle in world.Vehicles)
            {
                if (vehicle.FollowsTrafficRules && vehicle.Speed > vehicle.CellAtFront.SpeedLimit + 0.001f)
                    Assert.Fail($"Law-abiding car {vehicle.Id} drove at {vehicle.Speed * 3.6f:F1} km/h in a {vehicle.CellAtFront.SpeedLimit * 3.6f:F0} zone.");
            }
        }
        foreach (MetricsSample sample in world.Metrics.Samples) redLightsRun += sample.RedLightsRun;

        Assert.That(world.Metrics.Collisions.Count, Is.GreaterThan(0), "rebels cause crashes");
        foreach (CollisionRecord collision in world.Metrics.Collisions)
            Assert.That(collision.RebelInvolved, Is.True, $"crash at {collision.Cell}: {collision.FirstKind} and {collision.SecondKind}");
        Assert.That(redLightsRun, Is.GreaterThan(0), "rebels run red lights");
    }

    static Vehicle RunUntilSomeoneHolds(World world, RoadCell cell)
    {
        for (int step = 0; step < 200 && cell.Holder == null; step++)
            world.Tick(Step);
        Assert.That(cell.Holder, Is.Not.Null, "somebody entered the junction");
        return cell.Holder;
    }
}
