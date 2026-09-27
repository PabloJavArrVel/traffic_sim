using System.Collections.Generic;

/// <summary>
/// Groups the traffic lights of the map into intersections and gives each intersection a TrafficLightController.
///
/// Two lights belong to the same intersection when:
///   - they touch each other (also diagonally), or
///   - the traffic they hold back meets in the same junction.
/// </summary>
public static class TrafficLightPlanner
{
    /// <summary>How many cells past a light we look for the junction it guards.</summary>
    const int CellsToLookForJunction = 3;

    public static void GroupIntoIntersections(RoadNetwork network, SimulationSettings settings)
    {
        var junctionGuardedBy = new Dictionary<TrafficLight, HashSet<RoadCell>>();
        foreach (TrafficLight light in network.TrafficLights)
            junctionGuardedBy[light] = JunctionAfter(light.Cell, network);

        // Start with one intersection per light, then keep merging intersections that belong together.
        var intersections = new List<List<TrafficLight>>();
        foreach (TrafficLight light in network.TrafficLights)
            intersections.Add(new List<TrafficLight> { light });

        bool mergedSomething = true;
        while (mergedSomething)
        {
            mergedSomething = false;
            for (int a = 0; a < intersections.Count && !mergedSomething; a++)
            {
                for (int b = a + 1; b < intersections.Count && !mergedSomething; b++)
                {
                    if (!BelongTogether(intersections[a], intersections[b], junctionGuardedBy)) continue;
                    intersections[a].AddRange(intersections[b]);
                    intersections.RemoveAt(b);
                    mergedSomething = true;
                }
            }
        }

        foreach (List<TrafficLight> intersection in intersections)
            network.AddTrafficLightController(new TrafficLightController(intersection, settings));
    }

    static bool BelongTogether(List<TrafficLight> first, List<TrafficLight> second,
                               Dictionary<TrafficLight, HashSet<RoadCell>> junctionGuardedBy)
    {
        foreach (TrafficLight a in first)
        {
            foreach (TrafficLight b in second)
            {
                if (a.Cell.Position.Touches(b.Cell.Position)) return true;
                if (junctionGuardedBy[a].Overlaps(junctionGuardedBy[b])) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The junction a light guards: we follow the street from the light (up to a few cells) until we find
    /// junction cells, and return them together with all the junction cells touching them, because one
    /// intersection is often several cells big.
    /// </summary>
    static HashSet<RoadCell> JunctionAfter(RoadCell lightCell, RoadNetwork network)
    {
        var junction = new HashSet<RoadCell>();
        var visited = new HashSet<RoadCell> { lightCell };
        var cellsToFollow = new List<RoadCell> { lightCell };

        for (int step = 0; step < CellsToLookForJunction && junction.Count == 0; step++)
        {
            var nextCells = new List<RoadCell>();
            foreach (RoadCell cell in cellsToFollow)
            {
                foreach (CellLink link in cell.Exits)
                {
                    if (link.IsLaneChange || !visited.Add(link.To)) continue;
                    if (link.To.IsJunction) AddJunctionAndItsNeighbours(link.To, junction, network);
                    else nextCells.Add(link.To);
                }
            }
            cellsToFollow = nextCells;
        }

        return junction;
    }

    static void AddJunctionAndItsNeighbours(RoadCell start, HashSet<RoadCell> junction, RoadNetwork network)
    {
        var toVisit = new Stack<RoadCell>();
        toVisit.Push(start);
        while (toVisit.Count > 0)
        {
            RoadCell cell = toVisit.Pop();
            if (!junction.Add(cell)) continue;

            foreach (Direction direction in new[] { Direction.North, Direction.East, Direction.South, Direction.West })
            {
                RoadCell neighbour = network.CellAt(cell.Position.Step(direction));
                if (neighbour != null && neighbour.IsJunction) toVisit.Push(neighbour);
            }
        }
    }
}
