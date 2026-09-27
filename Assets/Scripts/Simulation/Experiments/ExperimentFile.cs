using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Reads the scenarios from the "Scenarios" sheet of Experiments.xlsx.
/// The first row has the column names (the same names as the fields of ExperimentScenario, like "Taxis");
/// every other row is one scenario. Empty cells keep the baseline value, so you only fill in what changes.
/// </summary>
public static class ExperimentFile
{
    public const string SheetName = "Scenarios";

    public static List<ExperimentScenario> ReadScenarios(byte[] xlsxFileBytes)
    {
        string[,] cells = XlsxSheetReader.ReadSheet(xlsxFileBytes, SheetName);
        var scenarios = new List<ExperimentScenario>();

        for (int row = 1; row < cells.GetLength(0); row++)
        {
            if (string.IsNullOrWhiteSpace(cells[row, 0])) continue;   // a row without a name is ignored

            var scenario = new ExperimentScenario();
            for (int column = 0; column < cells.GetLength(1); column++)
            {
                string columnName = cells[0, column].Trim();
                string value = cells[row, column].Trim();
                if (columnName == "" || value == "") continue;
                SetValue(scenario, columnName, value, row);
            }
            scenarios.Add(scenario);
        }
        return scenarios;
    }

    static void SetValue(ExperimentScenario scenario, string column, string value, int row)
    {
        switch (column)
        {
            case "Name": scenario.Name = value; break;
            case "AmbientCars": scenario.AmbientCars = WholeNumber(value, column, row); break;
            case "Taxis": scenario.Taxis = WholeNumber(value, column, row); break;
            case "InitialPedestrians": scenario.InitialPedestrians = WholeNumber(value, column, row); break;
            case "MaxPedestrians": scenario.MaxPedestrians = WholeNumber(value, column, row); break;
            case "SecondsBetweenPedestrians": scenario.SecondsBetweenPedestrians = Number(value, column, row); break;
            case "PedestrianPatienceSeconds": scenario.PedestrianPatienceSeconds = Number(value, column, row); break;
            case "DriverCalmnessMin": scenario.DriverCalmnessMin = Number(value, column, row); break;
            case "DriverCalmnessMax": scenario.DriverCalmnessMax = Number(value, column, row); break;
            case "SpeedLimitKmh": scenario.SpeedLimitKmh = Number(value, column, row); break;
            case "GreenLightSeconds": scenario.GreenLightSeconds = Number(value, column, row); break;
            case "DurationMinutes": scenario.DurationMinutes = Number(value, column, row); break;
            case "WarmupMinutes": scenario.WarmupMinutes = Number(value, column, row); break;
            case "Seeds": scenario.Seeds = WholeNumber(value, column, row); break;
            case "FirstSeed": scenario.FirstSeed = WholeNumber(value, column, row); break;
            case "Description": break;   // notes for people, ignored by the simulation
            default:
                throw new ExperimentFileException($"Experiments sheet: unknown column '{column}'. See the Help sheet for the column names.");
        }
    }

    static float Number(string value, string column, int row)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number)) return number;
        throw new ExperimentFileException($"Experiments sheet, row {row + 1}: '{value}' in column {column} is not a number.");
    }

    static int WholeNumber(string value, string column, int row)
    {
        return (int)System.Math.Round(Number(value, column, row));
    }
}
