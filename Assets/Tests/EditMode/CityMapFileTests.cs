using System.IO;
using NUnit.Framework;

/// <summary>Reading Assets/StreamingAssets/CityMap.xlsx and checking the real city.</summary>
public class CityMapFileTests
{
    static byte[] RealCityMapBytes() => File.ReadAllBytes(CityMapLoader.PathTo("CityMap.xlsx"));

    [Test]
    public void ReadsTheCellsOfTheMapSheet()
    {
        string[,] cells = XlsxSheetReader.ReadSheet(RealCityMapBytes(), "Map");

        Assert.That(cells.GetLength(0), Is.EqualTo(25), "rows");
        Assert.That(cells.GetLength(1), Is.EqualTo(25), "columns");
        Assert.That(cells[0, 0], Is.EqualTo(">"), "A1");
        Assert.That(cells[0, 15], Is.EqualTo("S>"), "P1");
        Assert.That(cells[12, 6], Is.EqualTo("Sv"), "G13");
        Assert.That(cells[12, 3], Is.EqualTo("R"), "D13");
        Assert.That(cells[1, 9], Is.EqualTo("7"), "J2 (a parking spot number)");
    }

    [Test]
    public void AMissingSheetGivesAClearMessage()
    {
        var problem = Assert.Throws<CityMapException>(() => XlsxSheetReader.ReadSheet(RealCityMapBytes(), "Streets"));
        StringAssert.Contains("'Streets'", problem.Message);
        StringAssert.Contains("Map", problem.Message);   // it lists the sheets that do exist
    }

    [Test]
    public void TheRealCityHasNoProblems()
    {
        RoadNetwork network = TestMaps.LoadRealCity();

        Assert.That(CityMapValidator.FindProblems(network), Is.Empty);
        Assert.That(ConnectedCells.LargestGroup(network).Count, Is.EqualTo(network.Cells.Count),
            "every street cell can reach every other one");
    }

    [Test]
    public void TheRealCityHasSixIntersectionsWithLights()
    {
        RoadNetwork network = TestMaps.LoadRealCity();

        Assert.That(network.Cells.Count, Is.EqualTo(361));
        Assert.That(network.TrafficLights.Count, Is.EqualTo(20));
        Assert.That(network.TrafficLightControllers.Count, Is.EqualTo(6));

        foreach (TrafficLightController intersection in network.TrafficLightControllers)
        {
            bool hasEastWest = false, hasNorthSouth = false;
            foreach (TrafficLight light in intersection.Lights)
            {
                if (light.TrafficDirection.IsEastWest()) hasEastWest = true;
                else hasNorthSouth = true;
            }
            Assert.That(hasEastWest && hasNorthSouth, Is.True, "each intersection alternates two directions");
        }
    }
}
