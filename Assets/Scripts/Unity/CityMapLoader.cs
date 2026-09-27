using System.IO;
using UnityEngine;

/// <summary>
/// Reads the city map from Assets/StreamingAssets and builds the road network.
/// Used when you press Play, by the Scene view gizmos and by the "Traffic Simulation" editor menu.
/// </summary>
public static class CityMapLoader
{
    /// <summary>The sheet of the Excel file that describes the streets.</summary>
    public const string SheetName = "Map";

    public static string PathTo(string fileName)
    {
        return Path.Combine(Application.streamingAssetsPath, fileName);
    }

    public static RoadNetwork LoadRoadNetwork(string fileName, SimulationSettings settings)
    {
        return RoadNetworkBuilder.Build(LoadCityMap(fileName), settings);
    }

    public static CityMap LoadCityMap(string fileName)
    {
        string path = PathTo(fileName);
        if (!File.Exists(path))
            throw new CityMapException($"City map not found: {path}");

        string[,] cellTexts = XlsxSheetReader.ReadSheet(ReadFileEvenIfOpenInExcel(path), SheetName);
        return CityMapParser.Parse(cellTexts);
    }

    /// <summary>Excel keeps the file open while you edit it; FileShare.ReadWrite lets us read it anyway.</summary>
    static byte[] ReadFileEvenIfOpenInExcel(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
