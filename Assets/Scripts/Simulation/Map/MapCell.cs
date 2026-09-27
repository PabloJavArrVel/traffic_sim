/// <summary>What a cell of the Excel map contains.</summary>
public enum MapCellKind
{
    Nothing,
    Street,
    Building,
    RoundaboutIsland,
    ParkingSpot
}

/// <summary>One cell of the Excel map after reading its text (see the Legend sheet of CityMap.xlsx).</summary>
public readonly struct MapCell
{
    public readonly MapCellKind Kind;

    /// <summary>Which way traffic flows. Only meaningful for streets.</summary>
    public readonly Direction TrafficDirection;

    /// <summary>True for street cells written as S>, S&lt;, S^ or Sv.</summary>
    public readonly bool HasTrafficLight;

    MapCell(MapCellKind kind, Direction trafficDirection, bool hasTrafficLight)
    {
        Kind = kind;
        TrafficDirection = trafficDirection;
        HasTrafficLight = hasTrafficLight;
    }

    public bool IsStreet => Kind == MapCellKind.Street;

    public static MapCell Street(Direction trafficDirection, bool hasTrafficLight)
    {
        return new MapCell(MapCellKind.Street, trafficDirection, hasTrafficLight);
    }

    public static MapCell Other(MapCellKind kind)
    {
        return new MapCell(kind, Direction.North, false);
    }
}
