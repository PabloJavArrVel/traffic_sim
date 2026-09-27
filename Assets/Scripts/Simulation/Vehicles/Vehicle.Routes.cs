using System;
using System.Collections.Generic;

/// <summary>How a vehicle plans and changes its route. See Vehicle.cs.</summary>
public abstract partial class Vehicle
{
    /// <summary>
    /// Where new routes start: the last path cell we already hold (we can't give those back),
    /// or at least the cell the front bumper is driving into.
    /// </summary>
    protected int IndexWhereNewRoutesStart()
    {
        int lastHeld = FirstPathIndexNotHeld() - 1;
        return Math.Min(Path.LastIndex, Math.Max(Path.FrontIndex + 1, lastHeld));
    }

    /// <summary>Changes the destination. Returns false if there is no way to get there.</summary>
    protected bool DriveTo(RoadCell destination, World world)
    {
        int start = IndexWhereNewRoutesStart();
        List<RoadCell> route = RouteFinder.FindRoute(Path.Cells[start], destination, world.Settings);
        if (route == null) return false;
        Path.ReplaceAfter(start, route);
        return true;
    }

    /// <summary>Adds a trip to a new destination at the end of the current path.</summary>
    protected void ContinueTo(RoadCell destination, World world)
    {
        List<RoadCell> route = RouteFinder.FindRoute(Path.LastCell, destination, world.Settings);
        if (route != null) Path.Extend(route);
    }

    /// <summary>Meters the car would drive to reach a cell (following roads, not in a straight line).</summary>
    public float DrivingDistanceTo(RoadCell destination, World world)
    {
        int start = IndexWhereNewRoutesStart();
        List<RoadCell> route = RouteFinder.FindRoute(Path.Cells[start], destination, world.Settings);
        if (route == null) return float.PositiveInfinity;
        return Path.DistanceToCenterOf(start) + RouteFinder.LengthOf(route);
    }

    /// <summary>The planned lane change is blocked: stay in our lane and find another way to the destination.</summary>
    bool TryKeepingStraight(int laneChangeStartIndex, CellLink laneChange, World world)
    {
        List<RoadCell> route = RouteFinder.FindRoute(laneChange.CellAhead, Path.LastCell, world.Settings);
        if (route == null) return false;

        route.Insert(0, Path.Cells[laneChangeStartIndex]);
        Path.ReplaceAfter(laneChangeStartIndex, route);
        return true;
    }

    /// <summary>
    /// We have been stuck for a long time (not at a red light): maybe the road ahead is jammed.
    /// Try a route to the same destination that avoids the cell we are waiting for.
    /// </summary>
    void LookForAnotherWay(World world)
    {
        int start = IndexWhereNewRoutesStart();
        if (start >= Path.LastIndex) return;

        RoadCell blockedCell = Path.Cells[start + 1];
        List<RoadCell> route = RouteFinder.FindRoute(Path.Cells[start], Path.LastCell, world.Settings, cellToAvoid: blockedCell);
        if (route == null) return;

        Path.ReplaceAfter(start, route);
        world.Statistics.DetoursTaken++;
    }
}
