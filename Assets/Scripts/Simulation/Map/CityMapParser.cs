using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Turns the text of each Excel cell into a CityMap.
///
///   > &lt; ^ v          street, traffic flows east / west / north / south
///   S> S&lt; S^ Sv      street with a traffic light, traffic flows in the arrow's direction
///   X               building
///   R               roundabout island
///   0-9             parking spot (not used yet)
///   (empty)         nothing
/// </summary>
public static class CityMapParser
{
    public static CityMap Parse(string[,] cellTexts)
    {
        int rows = cellTexts.GetLength(0);
        int columns = cellTexts.GetLength(1);
        var cells = new MapCell[rows, columns];
        var problems = new List<string>();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                var position = new GridPosition(row, column);
                cells[row, column] = ParseCell(cellTexts[row, column], position, problems);
            }
        }

        if (problems.Count > 0)
            throw new CityMapException("The city map has problems:\n" + string.Join("\n", problems));

        return new CityMap(cells);
    }

    static MapCell ParseCell(string text, GridPosition position, List<string> problems)
    {
        string code = (text ?? "").Trim();

        if (code == "")
            return MapCell.Other(MapCellKind.Nothing);

        if (TryReadArrow(code, out Direction direction))
            return MapCell.Street(direction, hasTrafficLight: false);

        bool isTrafficLight = code.Length == 2 && (code[0] == 'S' || code[0] == 's');
        if (isTrafficLight && TryReadArrow(code.Substring(1), out direction))
            return MapCell.Street(direction, hasTrafficLight: true);

        if (code == "S" || code == "s")
        {
            problems.Add($"Cell {position}: a traffic light needs to say which way traffic flows, for example S> or Sv.");
            return MapCell.Other(MapCellKind.Nothing);
        }

        if (code == "X" || code == "x")
            return MapCell.Other(MapCellKind.Building);

        if (code == "R" || code == "r")
            return MapCell.Other(MapCellKind.RoundaboutIsland);

        if (double.TryParse(code, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            return MapCell.Other(MapCellKind.ParkingSpot);

        problems.Add($"Cell {position}: unknown code '{code}'. See the Legend sheet for the codes you can use.");
        return MapCell.Other(MapCellKind.Nothing);
    }

    static bool TryReadArrow(string code, out Direction direction)
    {
        switch (code)
        {
            case ">": direction = Direction.East; return true;
            case "<": direction = Direction.West; return true;
            case "^": direction = Direction.North; return true;
            case "v":
            case "V": direction = Direction.South; return true;
            default: direction = Direction.North; return false;
        }
    }
}
