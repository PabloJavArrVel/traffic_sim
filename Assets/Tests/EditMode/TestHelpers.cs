using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Builds small city maps from text. Each string is one row; cell codes are separated by spaces.</summary>
static class TestMaps
{
    public static CityMap Parse(params string[] rows)
    {
        var codesPerRow = new List<string[]>();
        int columns = 0;
        foreach (string row in rows)
        {
            string[] codes = row.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            codesPerRow.Add(codes);
            columns = Math.Max(columns, codes.Length);
        }

        var texts = new string[rows.Length, columns];
        for (int row = 0; row < rows.Length; row++)
            for (int column = 0; column < columns; column++)
                texts[row, column] = column < codesPerRow[row].Length ? codesPerRow[row][column] : "";

        return CityMapParser.Parse(texts);
    }

    public static RoadNetwork Build(params string[] rows)
    {
        return RoadNetworkBuilder.Build(Parse(rows), new SimulationSettings());
    }

    /// <summary>The street cell at an Excel address, for example "C2".</summary>
    public static RoadCell Cell(RoadNetwork network, string excelAddress)
    {
        int column = excelAddress[0] - 'A';
        int row = int.Parse(excelAddress.Substring(1)) - 1;
        RoadCell cell = network.CellAt(new GridPosition(row, column));
        Assert.That(cell, Is.Not.Null, $"There is no street at {excelAddress}.");
        return cell;
    }

    public static RoadNetwork LoadRealCity()
    {
        return CityMapLoader.LoadRoadNetwork("CityMap.xlsx", new SimulationSettings());
    }
}

/// <summary>Checks that the safety rules hold, looking at the cars in two independent ways.</summary>
static class TrafficChecks
{
    /// <summary>Every car must hold every cell under it, so no other car can be there.</summary>
    public static void AssertCarsHoldTheCellsUnderThem(World world)
    {
        foreach (Vehicle vehicle in world.Vehicles)
        {
            foreach (RoadCell cell in vehicle.Path.CellsUnderCar(vehicle.Length))
            {
                if (cell.Holder != vehicle)
                    Assert.Fail($"At {world.Time:F2}s car {vehicle.Id} is on {cell.Position} but that cell is held by " +
                                $"{(cell.Holder == null ? "nobody" : "car " + cell.Holder.Id)}.");
            }
        }
    }

    /// <summary>
    /// Geometric check that doesn't trust the cell bookkeeping: we draw each car as a line from its rear bumper to
    /// its front bumper, and two of those lines must never come closer than 'minimumDistance' meters.
    /// </summary>
    public static void AssertNoCarsOverlap(World world, float minimumDistance = 0.3f)
    {
        var bodies = new List<(Vehicle vehicle, MapPoint rear, MapPoint front)>();
        foreach (Vehicle vehicle in world.Vehicles)
        {
            MapPoint front = PositionBehindFront(vehicle, 0f);
            MapPoint rear = PositionBehindFront(vehicle, vehicle.Length);
            bodies.Add((vehicle, rear, front));
        }

        for (int a = 0; a < bodies.Count; a++)
        {
            for (int b = a + 1; b < bodies.Count; b++)
            {
                float distance = DistanceBetweenSegments(bodies[a].rear, bodies[a].front, bodies[b].rear, bodies[b].front);
                if (distance < minimumDistance)
                    Assert.Fail($"At {world.Time:F2}s cars {bodies[a].vehicle.Id} and {bodies[b].vehicle.Id} are {distance:F2} m apart " +
                                $"(car {bodies[a].vehicle.Id} front at {bodies[a].front}, car {bodies[b].vehicle.Id} front at {bodies[b].front}).");
            }
        }
    }

    static MapPoint PositionBehindFront(Vehicle vehicle, float meters)
    {
        PathPoint point = vehicle.Path.PointBehindFront(meters);
        return PathGeometry.PoseAt(vehicle.Path, point, cornerRadius: 0f).Position;
    }

    static float DistanceBetweenSegments(MapPoint a1, MapPoint a2, MapPoint b1, MapPoint b2)
    {
        if (SegmentsCross(a1, a2, b1, b2)) return 0f;
        return Math.Min(Math.Min(DistanceToSegment(a1, b1, b2), DistanceToSegment(a2, b1, b2)),
                        Math.Min(DistanceToSegment(b1, a1, a2), DistanceToSegment(b2, a1, a2)));
    }

    static float DistanceToSegment(MapPoint point, MapPoint start, MapPoint end)
    {
        MapPoint segment = end - start;
        float lengthSquared = MapPoint.Dot(segment, segment);
        float t = lengthSquared > 0f ? MapPoint.Dot(point - start, segment) / lengthSquared : 0f;
        t = Math.Max(0f, Math.Min(1f, t));
        return MapPoint.Distance(point, start + segment * t);
    }

    static bool SegmentsCross(MapPoint a1, MapPoint a2, MapPoint b1, MapPoint b2)
    {
        float Side(MapPoint origin, MapPoint to, MapPoint point) =>
            (to.X - origin.X) * (point.Y - origin.Y) - (to.Y - origin.Y) * (point.X - origin.X);

        float d1 = Side(b1, b2, a1), d2 = Side(b1, b2, a2), d3 = Side(a1, a2, b1), d4 = Side(a1, a2, b2);
        return d1 * d2 < 0f && d3 * d4 < 0f;
    }
}
