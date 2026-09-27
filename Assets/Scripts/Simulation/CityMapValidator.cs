using System.Collections.Generic;
using System.Text;

/// <summary>
/// Looks for problems in a city map that would make cars get stuck or leave streets unused, and describes the map.
/// Used by the "Traffic Simulation > Check City Map" menu in Unity and by the tests.
/// </summary>
public static class CityMapValidator
{
    /// <summary>Everything that is wrong with the map, in words. An empty list means the map is fine.</summary>
    public static List<string> FindProblems(RoadNetwork network)
    {
        var problems = new List<string>();
        HashSet<RoadCell> connected = ConnectedCells.LargestGroup(network);

        foreach (RoadCell cell in network.Cells)
        {
            if (cell.Exits.Count == 0)
                problems.Add($"{cell.Position}: this street goes nowhere (cars can't drive on from here).");
            else if (cell.Entrances.Count == 0)
                problems.Add($"{cell.Position}: no street leads into this cell.");
            else if (!connected.Contains(cell))
                problems.Add($"{cell.Position}: cars can't drive from here to the rest of the city and back.");
        }

        foreach (TrafficLight light in network.TrafficLights)
        {
            if (light.Cell.IsJunction)
                problems.Add($"{light.Cell.Position}: this traffic light is inside a junction, so cars would stop in the middle of it.");
        }

        return problems;
    }

    /// <summary>A short description: how many cells, junctions and lights, and which lights work together.</summary>
    public static string Describe(RoadNetwork network)
    {
        int junctionCells = 0;
        foreach (RoadCell cell in network.Cells)
            if (cell.IsJunction) junctionCells++;

        var text = new StringBuilder();
        text.AppendLine($"{network.Cells.Count} street cells, {junctionCells} junction cells, " +
                        $"{network.TrafficLights.Count} traffic lights in {network.TrafficLightControllers.Count} intersections.");

        int number = 1;
        foreach (TrafficLightController intersection in network.TrafficLightControllers)
        {
            text.AppendLine($"  Intersection {number}: {LightCellsOf(intersection)}");
            number++;
        }
        return text.ToString();
    }

    /// <summary>The cells of an intersection's lights, for example "east-west P1 | north-south Q2 R2".</summary>
    public static string LightCellsOf(TrafficLightController intersection)
    {
        return $"east-west {CellsOf(intersection, eastWest: true)} | north-south {CellsOf(intersection, eastWest: false)}";
    }

    static string CellsOf(TrafficLightController intersection, bool eastWest)
    {
        var cells = new List<string>();
        foreach (TrafficLight light in intersection.Lights)
            if (light.TrafficDirection.IsEastWest() == eastWest) cells.Add(light.Cell.Position.ExcelAddress);
        return cells.Count > 0 ? string.Join(" ", cells) : "(none)";
    }
}
