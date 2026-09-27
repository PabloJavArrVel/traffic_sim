/// <summary>The city as described by the "Map" sheet of CityMap.xlsx: a grid of cells.</summary>
public class CityMap
{
    readonly MapCell[,] cells;

    public CityMap(MapCell[,] cells)
    {
        this.cells = cells;
    }

    public int Rows => cells.GetLength(0);
    public int Columns => cells.GetLength(1);

    public bool IsInside(GridPosition position)
    {
        return position.Row >= 0 && position.Row < Rows
            && position.Column >= 0 && position.Column < Columns;
    }

    /// <summary>The cell at a position. Positions outside the map count as empty.</summary>
    public MapCell CellAt(GridPosition position)
    {
        return IsInside(position) ? cells[position.Row, position.Column] : MapCell.Other(MapCellKind.Nothing);
    }
}
