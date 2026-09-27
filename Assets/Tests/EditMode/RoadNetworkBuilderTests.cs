using NUnit.Framework;

/// <summary>The rules that turn the arrows of the map into places cars can drive (see RoadNetworkBuilder).</summary>
public class RoadNetworkBuilderTests
{
    [Test]
    public void CarsDriveStraightAhead()
    {
        RoadNetwork network = TestMaps.Build("> > >");

        Assert.That(TestMaps.Cell(network, "A1").LinkTo(TestMaps.Cell(network, "B1")), Is.Not.Null);
        Assert.That(TestMaps.Cell(network, "B1").LinkTo(TestMaps.Cell(network, "A1")), Is.Null, "no driving backwards");
    }

    [Test]
    public void CarsNeverDriveIntoACellPointingBackAtThem()
    {
        RoadNetwork network = TestMaps.Build("> <");

        Assert.That(TestMaps.Cell(network, "A1").Exits, Is.Empty);
    }

    [Test]
    public void CarsTurnWhenTheCellAheadPointsSideways()
    {
        RoadNetwork network = TestMaps.Build(
            "> > >",
            "X ^ X");

        Assert.That(TestMaps.Cell(network, "B2").LinkTo(TestMaps.Cell(network, "B1")), Is.Not.Null);
    }

    [Test]
    public void CarsTurnIntoASideStreetThatLeavesAwayFromThem()
    {
        RoadNetwork leaving = TestMaps.Build(
            "> > >",
            "X v X");
        RoadNetwork arriving = TestMaps.Build(
            "> > >",
            "X ^ X");

        Assert.That(TestMaps.Cell(leaving, "B1").LinkTo(TestMaps.Cell(leaving, "B2")), Is.Not.Null);
        Assert.That(TestMaps.Cell(arriving, "B1").LinkTo(TestMaps.Cell(arriving, "B2")), Is.Null,
            "the side street flows towards us, so we can't turn into it");
    }

    [Test]
    public void ACellEnteredFromTwoDirectionsIsAJunction()
    {
        RoadNetwork network = TestMaps.Build(
            "> > >",
            "X ^ X");

        Assert.That(TestMaps.Cell(network, "B1").IsJunction, Is.True);
        Assert.That(TestMaps.Cell(network, "A1").IsJunction, Is.False);
        Assert.That(TestMaps.Cell(network, "C1").IsJunction, Is.False);
    }

    [Test]
    public void ParallelLanesGetLaneChangesAwayFromJunctions()
    {
        RoadNetwork network = TestMaps.Build(
            "> > > >",
            "> > > >");

        CellLink laneChange = TestMaps.Cell(network, "A1").LinkTo(TestMaps.Cell(network, "B2"));
        Assert.That(laneChange, Is.Not.Null);
        Assert.That(laneChange.IsLaneChange, Is.True);
        Assert.That(laneChange.CellBeside, Is.SameAs(TestMaps.Cell(network, "A2")));
        Assert.That(laneChange.CellAhead, Is.SameAs(TestMaps.Cell(network, "B1")));
        Assert.That(TestMaps.Cell(network, "B2").LinkTo(TestMaps.Cell(network, "C1")).IsLaneChange, Is.True);
    }

    [Test]
    public void TouchingTrafficLightsFormOneIntersectionThatAlternates()
    {
        RoadNetwork network = TestMaps.Build(
            "X S^ X",
            "> S> >",
            "X ^  X");
        TrafficLightController intersection = network.TrafficLightControllers[0];
        TrafficLight eastbound = TestMaps.Cell(network, "B2").TrafficLight;
        TrafficLight northbound = TestMaps.Cell(network, "B1").TrafficLight;

        Assert.That(network.TrafficLightControllers.Count, Is.EqualTo(1));
        Assert.That(eastbound.Color, Is.EqualTo(LightColor.Green), "east-west starts green");
        Assert.That(northbound.Color, Is.EqualTo(LightColor.Red));

        var settings = new SimulationSettings();
        intersection.Tick(settings.GreenLightSeconds);
        Assert.That(eastbound.Color, Is.EqualTo(LightColor.Yellow));

        intersection.Tick(settings.YellowLightSeconds);
        Assert.That(eastbound.Color, Is.EqualTo(LightColor.Red), "all red");
        Assert.That(northbound.Color, Is.EqualTo(LightColor.Red), "all red");

        intersection.Tick(settings.AllRedSeconds);
        Assert.That(northbound.Color, Is.EqualTo(LightColor.Green), "then north-south gets green");
        Assert.That(eastbound.Color, Is.EqualTo(LightColor.Red));
    }
}
