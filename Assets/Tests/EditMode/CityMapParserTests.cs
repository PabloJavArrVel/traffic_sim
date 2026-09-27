using NUnit.Framework;

public class CityMapParserTests
{
    [Test]
    public void ReadsArrowsLightsAndBuildings()
    {
        CityMap map = TestMaps.Parse("> S< ^ Sv X R 7");

        Assert.That(map.CellAt(new GridPosition(0, 0)).TrafficDirection, Is.EqualTo(Direction.East));
        Assert.That(map.CellAt(new GridPosition(0, 1)).HasTrafficLight, Is.True);
        Assert.That(map.CellAt(new GridPosition(0, 1)).TrafficDirection, Is.EqualTo(Direction.West));
        Assert.That(map.CellAt(new GridPosition(0, 2)).TrafficDirection, Is.EqualTo(Direction.North));
        Assert.That(map.CellAt(new GridPosition(0, 3)).TrafficDirection, Is.EqualTo(Direction.South));
        Assert.That(map.CellAt(new GridPosition(0, 4)).Kind, Is.EqualTo(MapCellKind.Building));
        Assert.That(map.CellAt(new GridPosition(0, 5)).Kind, Is.EqualTo(MapCellKind.RoundaboutIsland));
        Assert.That(map.CellAt(new GridPosition(0, 6)).Kind, Is.EqualTo(MapCellKind.ParkingSpot));
    }

    [Test]
    public void ATrafficLightWithoutDirectionIsReportedWithItsExcelAddress()
    {
        var problem = Assert.Throws<CityMapException>(() => TestMaps.Parse("> S >"));
        StringAssert.Contains("B1", problem.Message);
    }

    [Test]
    public void AnUnknownCodeIsReportedWithItsExcelAddress()
    {
        var problem = Assert.Throws<CityMapException>(() => TestMaps.Parse("> >", "> Q"));
        StringAssert.Contains("B2", problem.Message);
        StringAssert.Contains("'Q'", problem.Message);
    }
}
