using System.Collections.Generic;

/// <summary>Every street cell of the city, how they connect, and the traffic lights. Built by RoadNetworkBuilder.</summary>
public class RoadNetwork
{
    readonly Dictionary<GridPosition, RoadCell> cellsByPosition = new Dictionary<GridPosition, RoadCell>();
    readonly List<RoadCell> cells = new List<RoadCell>();
    readonly List<TrafficLight> trafficLights = new List<TrafficLight>();
    readonly List<TrafficLightController> trafficLightControllers = new List<TrafficLightController>();

    public RoadNetwork(int rows, int columns, float metersPerCell)
    {
        Rows = rows;
        Columns = columns;
        MetersPerCell = metersPerCell;
    }

    public int Rows { get; }
    public int Columns { get; }

    /// <summary>How wide one cell of the map is, in meters.</summary>
    public float MetersPerCell { get; }

    /// <summary>All street cells, row by row (top row first).</summary>
    public IReadOnlyList<RoadCell> Cells => cells;

    public IReadOnlyList<TrafficLight> TrafficLights => trafficLights;

    /// <summary>One controller per intersection with traffic lights.</summary>
    public IReadOnlyList<TrafficLightController> TrafficLightControllers => trafficLightControllers;

    /// <summary>The street cell at a position, or null if there is no street there.</summary>
    public RoadCell CellAt(GridPosition position)
    {
        return cellsByPosition.TryGetValue(position, out RoadCell cell) ? cell : null;
    }

    public void AddCell(RoadCell cell)
    {
        cellsByPosition.Add(cell.Position, cell);
        cells.Add(cell);
    }

    public void AddLink(CellLink link)
    {
        link.From.Exits.Add(link);
        link.To.Entrances.Add(link);
    }

    public void AddTrafficLight(TrafficLight light) => trafficLights.Add(light);

    public void AddTrafficLightController(TrafficLightController controller) => trafficLightControllers.Add(controller);
}
