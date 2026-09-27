using System.Collections.Generic;

/// <summary>
/// One street cell of the city: a place a car can be. A cell has room for one car: two cars on it have crashed.
///
/// Law-abiding cars never crash because a cell belongs to at most one car at a time (its Holder) and they only drive
/// into cells they hold. A car holds the cells under it plus the cells it has reserved just ahead of it.
/// Rebels also reserve free cells, but they drive into cells other cars hold (see Vehicle).
/// </summary>
public class RoadCell
{
    public RoadCell(GridPosition position, Direction trafficDirection, MapPoint center)
    {
        Position = position;
        TrafficDirection = trafficDirection;
        Center = center;
    }

    public GridPosition Position { get; }
    public Direction TrafficDirection { get; }

    /// <summary>The middle of the cell, in meters.</summary>
    public MapPoint Center { get; }

    /// <summary>The traffic light at the end of this cell, or null if there is none.</summary>
    public TrafficLight TrafficLight { get; set; }

    /// <summary>
    /// True when cars can drive into this cell from more than one direction (not counting lane changes).
    /// Cars take turns at junctions: whoever has waited longest goes first.
    /// </summary>
    public bool IsJunction { get; set; }

    /// <summary>
    /// A junction that cars can also leave in more than one direction, so different streams of traffic cross here.
    /// Cars only drive into a crossing when they can drive all the way through it: a car stopped inside a crossing
    /// would block the traffic that crosses its path.
    /// </summary>
    public bool IsCrossing { get; set; }

    /// <summary>Where cars can drive from here.</summary>
    public List<CellLink> Exits { get; } = new List<CellLink>();

    /// <summary>Where cars can arrive from.</summary>
    public List<CellLink> Entrances { get; } = new List<CellLink>();

    /// <summary>Meters per second. See TrafficRules for how it is chosen.</summary>
    public float SpeedLimit { get; set; }

    /// <summary>True when the cell is part of a street with two or more lanes side by side.</summary>
    public bool IsAvenue { get; set; }

    /// <summary>True for the street cells right around a roundabout island (an R cell of the map).</summary>
    public bool IsInRoundabout { get; set; }

    /// <summary>The car that owns this cell right now (it is on it or has reserved it), or null.</summary>
    public Vehicle Holder { get; private set; }

    public bool IsFree => Holder == null;

    /// <summary>The cars whose bodies are on this cell right now (updated by the World every step). More than one = a crash.</summary>
    public List<Vehicle> CarsOnIt { get; } = new List<Vehicle>();

    /// <summary>The link from this cell to a neighbouring cell, or null if cars can't drive there directly.</summary>
    public CellLink LinkTo(RoadCell other)
    {
        foreach (CellLink link in Exits)
            if (link.To == other) return link;
        return null;
    }

    public void Reserve(Vehicle vehicle)
    {
        if (Holder != null && Holder != vehicle)
            throw new System.InvalidOperationException($"Cell {Position} is already held by car {Holder.Id}.");
        Holder = vehicle;
    }

    public void Release(Vehicle vehicle)
    {
        if (Holder == vehicle) Holder = null;
    }

    public override string ToString() => $"{Position} {TrafficDirection.ToArrow()}";
}
