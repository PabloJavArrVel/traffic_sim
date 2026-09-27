using System;

/// <summary>
/// A cell of the city grid. Row 0 is the top row of the Excel sheet and Column 0 is column A,
/// so row 12, column 6 is the cell Excel calls "G13".
/// </summary>
public readonly struct GridPosition : IEquatable<GridPosition>
{
    public readonly int Row;
    public readonly int Column;

    public GridPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }

    /// <summary>The neighbouring cell in the given direction.</summary>
    public GridPosition Step(Direction direction)
    {
        return new GridPosition(Row + direction.RowStep(), Column + direction.ColumnStep());
    }

    /// <summary>True for the 8 cells around this one (including diagonals).</summary>
    public bool Touches(GridPosition other)
    {
        int rowDistance = Math.Abs(Row - other.Row);
        int columnDistance = Math.Abs(Column - other.Column);
        return !Equals(other) && rowDistance <= 1 && columnDistance <= 1;
    }

    /// <summary>The address Excel shows for this cell, for example "G13".</summary>
    public string ExcelAddress => ColumnLetters(Column) + (Row + 1);

    static string ColumnLetters(int column)
    {
        // 0 -> A, 25 -> Z, 26 -> AA ...
        string letters = "";
        int number = column + 1;
        while (number > 0)
        {
            int remainder = (number - 1) % 26;
            letters = (char)('A' + remainder) + letters;
            number = (number - 1) / 26;
        }
        return letters;
    }

    public bool Equals(GridPosition other) => Row == other.Row && Column == other.Column;
    public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
    public override int GetHashCode() => Row * 1000 + Column;
    public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
    public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
    public override string ToString() => ExcelAddress;
}
