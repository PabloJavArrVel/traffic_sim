using NUnit.Framework;

/// <summary>Cars on small hand-made maps: following, red lights and merging.</summary>
public class DrivingTests
{
    const float Step = 0.05f;

    // A one-lane loop, driven clockwise.
    static readonly string[] Ring =
    {
        "> > > > v",
        "^ X X X v",
        "^ < < < <"
    };

    // The same loop with a traffic light at C1 (eastbound).
    static readonly string[] RingWithLight =
    {
        "> > S> > v",
        "^ X X  X v",
        "^ < <  < <"
    };

    // Two loops that share their right part: they merge at C1 (the side street from C2 gives way) and split at C3.
    static readonly string[] TwoLoops =
    {
        "> > > > v",
        "^ X ^ X v",
        "^ < < < <"
    };

    [Test]
    public void CarsFollowEachOtherWithoutOverlapping()
    {
        World world = CreateWorld(Ring, cars: 4);

        RunAndCheck(world, seconds: 120f);

        foreach (Vehicle car in world.Vehicles)
            Assert.That(car.DistanceDriven, Is.GreaterThan(200f), $"car {car.Id} kept driving");
    }

    [Test]
    public void ACarStopsAtARedLightAndGoesOnGreen()
    {
        World world = CreateWorld(RingWithLight, cars: 1);
        Vehicle car = world.Vehicles[0];
        RoadCell afterTheLight = TestMaps.Cell(world.Network, "D1");

        // The light is green for 15 s, yellow for 3 s and then red until second 40.
        // (Cars brake smoothly and creep the last meter, so we look near the end of the red light.)
        RunAndCheck(world, seconds: 38f);
        Assert.That(world.Network.TrafficLights[0].Color, Is.EqualTo(LightColor.Red));
        Assert.That(car.Speed, Is.LessThan(0.1f), "stopped");
        Assert.That(car.WaitingFor, Is.EqualTo(ObstacleReason.RedLight));
        Assert.That(afterTheLight.Holder, Is.Not.SameAs(car), "it stopped before the stop line");

        float distanceBefore = car.DistanceDriven;
        RunAndCheck(world, seconds: 17f);   // green again from second 40 to 55
        Assert.That(car.DistanceDriven, Is.GreaterThan(distanceBefore + 20f), "it drove on after the light turned green");
    }

    [Test]
    public void SideStreetCarsStillGetIntoTheMainStreet()
    {
        World world = CreateWorld(TwoLoops, cars: 5);

        RunAndCheck(world, seconds: 180f);

        foreach (Vehicle car in world.Vehicles)
            Assert.That(car.DistanceDriven, Is.GreaterThan(200f), $"car {car.Id} kept driving");
        Assert.That(world.Statistics.LongestStopSeconds, Is.LessThan(60f), "giving way never means waiting forever");
    }

    static World CreateWorld(string[] map, int cars)
    {
        var world = new World(TestMaps.Build(map), new SimulationSettings(), randomSeed: 7);
        for (int i = 0; i < cars; i++)
            Assert.That(world.SpawnAmbientCar(), Is.Not.Null, "found a free place for every car");
        return world;
    }

    static void RunAndCheck(World world, float seconds)
    {
        int steps = (int)(seconds / Step);
        for (int i = 0; i < steps; i++)
        {
            world.Tick(Step);
            TrafficChecks.AssertCarsHoldTheCellsUnderThem(world);
            TrafficChecks.AssertNoCarsOverlap(world);
        }
    }
}
