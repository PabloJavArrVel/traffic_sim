using System.Collections.Generic;

/// <summary>
/// Finds the shortest way to drive between two cells, using Dijkstra's algorithm:
/// keep a list of cells we can reach and how far they are; always continue from the closest one.
/// </summary>
public static class RouteFinder
{
    /// <summary>
    /// The cells to drive through, starting with 'start' and ending with 'destination',
    /// or null if there is no way. 'cellToAvoid' (optional) is never used.
    /// </summary>
    public static List<RoadCell> FindRoute(RoadCell start, RoadCell destination, SimulationSettings settings, RoadCell cellToAvoid = null)
    {
        if (start == destination) return new List<RoadCell> { start };

        var distanceTo = new Dictionary<RoadCell, float> { [start] = 0f };
        var cameFrom = new Dictionary<RoadCell, RoadCell>();
        var finished = new HashSet<RoadCell>();
        var toVisit = new List<RoadCell> { start };

        while (toVisit.Count > 0)
        {
            RoadCell current = TakeClosest(toVisit, distanceTo);
            if (current == destination) return BuildRoute(cameFrom, start, destination);
            finished.Add(current);

            foreach (CellLink link in current.Exits)
            {
                RoadCell next = link.To;
                if (next == cellToAvoid || finished.Contains(next)) continue;

                float distanceThroughCurrent = distanceTo[current] + CostOf(link, settings);
                bool seenBefore = distanceTo.TryGetValue(next, out float bestSoFar);
                if (seenBefore && distanceThroughCurrent >= bestSoFar) continue;

                if (!seenBefore) toVisit.Add(next);
                distanceTo[next] = distanceThroughCurrent;
                cameFrom[next] = current;
            }
        }

        return null;
    }

    /// <summary>Meters from the first to the last cell of a route.</summary>
    public static float LengthOf(List<RoadCell> route)
    {
        float length = 0f;
        for (int i = 0; i + 1 < route.Count; i++)
            length += route[i].LinkTo(route[i + 1]).Length;
        return length;
    }

    /// <summary>
    /// How "long" a link counts when planning: its real length, plus a little extra for lane changes (so cars only
    /// change lanes when it helps), plus a lot extra when a car is stopped there right now (so cars drive around jams).
    /// </summary>
    static float CostOf(CellLink link, SimulationSettings settings)
    {
        float cost = link.Length;
        if (link.IsLaneChange) cost += settings.LaneChangeExtraCost;

        Vehicle carThere = link.To.Holder;
        if (carThere != null && carThere.Speed < 0.5f) cost += settings.StoppedCarExtraCost;
        return cost;
    }

    static RoadCell TakeClosest(List<RoadCell> cells, Dictionary<RoadCell, float> distanceTo)
    {
        int closestIndex = 0;
        for (int i = 1; i < cells.Count; i++)
            if (distanceTo[cells[i]] < distanceTo[cells[closestIndex]])
                closestIndex = i;

        RoadCell closest = cells[closestIndex];
        cells.RemoveAt(closestIndex);
        return closest;
    }

    static List<RoadCell> BuildRoute(Dictionary<RoadCell, RoadCell> cameFrom, RoadCell start, RoadCell destination)
    {
        var route = new List<RoadCell> { destination };
        RoadCell cell = destination;
        while (cell != start)
        {
            cell = cameFrom[cell];
            route.Add(cell);
        }
        route.Reverse();
        return route;
    }
}
