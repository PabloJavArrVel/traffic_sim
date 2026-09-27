using System;

/// <summary>
/// Builds the RoadNetwork from the CityMap. These are the same rules written in the Legend sheet of CityMap.xlsx:
///
///  1. Straight: a car can drive into the next cell in its direction, unless that cell points straight back at it.
///     If the next cell points sideways, the car turns there and follows the new arrow.
///  2. Turn: a car can turn into the cell to its left or right when that cell's arrow points away from it.
///  3. Junction: a cell that cars can drive into from more than one direction.
///     Crossing: a junction that cars can also leave in more than one direction.
///  4. Lane change: a car can move one cell forward and one cell sideways into a parallel lane, when the cell
///     beside it and the cell ahead of it point the same way too. Cars don't change lanes in or next to junctions.
///  5. Traffic lights are grouped into intersections (see TrafficLightPlanner).
/// </summary>
public static class RoadNetworkBuilder
{
    public static RoadNetwork Build(CityMap map, SimulationSettings settings)
    {
        var network = new RoadNetwork(map.Rows, map.Columns, settings.MetersPerCell);

        AddStreetCells(map, network, settings.MetersPerCell);

        foreach (RoadCell cell in network.Cells)
            AddDriveLinks(cell, network);

        MarkJunctions(network);

        foreach (RoadCell cell in network.Cells)
            AddLaneChanges(cell, network);

        TrafficLightPlanner.GroupIntoIntersections(network, settings);
        return network;
    }

    static void AddStreetCells(CityMap map, RoadNetwork network, float metersPerCell)
    {
        for (int row = 0; row < map.Rows; row++)
        {
            for (int column = 0; column < map.Columns; column++)
            {
                var position = new GridPosition(row, column);
                MapCell mapCell = map.CellAt(position);
                if (!mapCell.IsStreet) continue;

                var center = new MapPoint(column * metersPerCell, row * metersPerCell);
                var cell = new RoadCell(position, mapCell.TrafficDirection, center);
                network.AddCell(cell);

                if (mapCell.HasTrafficLight)
                {
                    cell.TrafficLight = new TrafficLight(cell);
                    network.AddTrafficLight(cell.TrafficLight);
                }
            }
        }
    }

    // Rules 1 and 2.
    static void AddDriveLinks(RoadCell cell, RoadNetwork network)
    {
        Direction direction = cell.TrafficDirection;
        float length = network.MetersPerCell;

        RoadCell ahead = network.CellAt(cell.Position.Step(direction));
        bool aheadPointsBackAtUs = ahead != null && ahead.TrafficDirection == direction.Opposite();
        if (ahead != null && !aheadPointsBackAtUs)
            network.AddLink(new CellLink(cell, ahead, length, direction));

        foreach (Direction side in new[] { direction.TurnLeft(), direction.TurnRight() })
        {
            RoadCell beside = network.CellAt(cell.Position.Step(side));
            bool streetLeavesToThisSide = beside != null && beside.TrafficDirection == side;
            if (streetLeavesToThisSide)
                network.AddLink(new CellLink(cell, beside, length, side));
        }
    }

    // Rule 3. Only drive links exist at this point, so lane changes are not counted.
    static void MarkJunctions(RoadNetwork network)
    {
        foreach (RoadCell cell in network.Cells)
        {
            cell.IsJunction = cell.Entrances.Count >= 2;
            cell.IsCrossing = cell.IsJunction && cell.Exits.Count >= 2;
        }
    }

    // Rule 4.
    static void AddLaneChanges(RoadCell cell, RoadNetwork network)
    {
        Direction direction = cell.TrafficDirection;
        RoadCell ahead = network.CellAt(cell.Position.Step(direction));

        foreach (Direction side in new[] { direction.TurnLeft(), direction.TurnRight() })
        {
            RoadCell beside = network.CellAt(cell.Position.Step(side));
            RoadCell target = network.CellAt(cell.Position.Step(direction).Step(side));

            bool parallelLanes = SameDirection(beside, direction) && SameDirection(ahead, direction) && SameDirection(target, direction);
            if (!parallelLanes) continue;

            bool nearJunction = cell.IsJunction || beside.IsJunction || ahead.IsJunction || target.IsJunction;
            if (nearJunction) continue;

            float length = network.MetersPerCell * MathF.Sqrt(2f);
            network.AddLink(new CellLink(cell, target, length, direction, beside, ahead));
        }
    }

    static bool SameDirection(RoadCell cell, Direction direction)
    {
        return cell != null && cell.TrafficDirection == direction;
    }
}
