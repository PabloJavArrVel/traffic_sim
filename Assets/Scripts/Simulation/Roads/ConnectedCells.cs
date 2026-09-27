using System.Collections.Generic;

/// <summary>
/// Finds the biggest group of street cells where you can drive from any cell to any other cell.
/// Cars only get destinations inside this group, so they never drive into a part of the city they can't leave.
/// In a well-made map the group contains every street cell (CityMapValidator reports the ones that are left out).
/// </summary>
public static class ConnectedCells
{
    public static HashSet<RoadCell> LargestGroup(RoadNetwork network)
    {
        var largest = new HashSet<RoadCell>();
        var alreadyInAGroup = new HashSet<RoadCell>();

        foreach (RoadCell cell in network.Cells)
        {
            if (alreadyInAGroup.Contains(cell)) continue;

            // The group of 'cell' = the cells we can drive to from it that can also drive back to it.
            HashSet<RoadCell> group = CellsReachableFrom(cell);
            group.IntersectWith(CellsThatCanReach(cell));

            alreadyInAGroup.UnionWith(group);
            if (group.Count > largest.Count) largest = group;
        }

        return largest;
    }

    public static HashSet<RoadCell> CellsReachableFrom(RoadCell start)
    {
        var reached = new HashSet<RoadCell> { start };
        var toVisit = new Queue<RoadCell>();
        toVisit.Enqueue(start);
        while (toVisit.Count > 0)
        {
            foreach (CellLink link in toVisit.Dequeue().Exits)
                if (reached.Add(link.To)) toVisit.Enqueue(link.To);
        }
        return reached;
    }

    public static HashSet<RoadCell> CellsThatCanReach(RoadCell destination)
    {
        var reached = new HashSet<RoadCell> { destination };
        var toVisit = new Queue<RoadCell>();
        toVisit.Enqueue(destination);
        while (toVisit.Count > 0)
        {
            foreach (CellLink link in toVisit.Dequeue().Entrances)
                if (reached.Add(link.From)) toVisit.Enqueue(link.From);
        }
        return reached;
    }
}
