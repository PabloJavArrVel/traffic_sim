using UnityEngine;

/// <summary>
/// Converts simulation positions (meters on the map, X east and Y south) into Unity world positions
/// (X east, Z north). Cell A1 of the Excel map is at 'cellA1Position' and each cell is 'cellSize' units wide.
/// </summary>
public class MapPlacement
{
    readonly Vector3 cellA1Position;
    readonly float unitsPerMeter;

    public MapPlacement(Vector3 cellA1Position, float cellSize, float metersPerCell)
    {
        this.cellA1Position = cellA1Position;
        MetersPerCell = metersPerCell;
        unitsPerMeter = cellSize / metersPerCell;
    }

    public float MetersPerCell { get; }

    public Vector3 ToWorld(MapPoint point, float height = 0f)
    {
        return cellA1Position + new Vector3(point.X * unitsPerMeter, height, -point.Y * unitsPerMeter);
    }

    public Vector3 ToWorldDirection(MapPoint direction)
    {
        return new Vector3(direction.X, 0f, -direction.Y).normalized;
    }

    public Vector3 ToWorldDirection(Direction direction)
    {
        return ToWorldDirection(direction.ToMapVector());
    }

    public float ToWorldLength(float meters)
    {
        return meters * unitsPerMeter;
    }
}
