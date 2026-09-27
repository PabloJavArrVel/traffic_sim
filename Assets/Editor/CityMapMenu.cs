using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The "Traffic Simulation" menu in the Unity editor:
///   Check City Map        reads CityMap.xlsx and lists any problem (streets that go nowhere, unreachable streets...)
///   Open City Map in Excel opens the file so you can edit it
/// </summary>
public static class CityMapMenu
{
    [MenuItem("Traffic Simulation/Check City Map")]
    static void CheckCityMap()
    {
        string file = CityMapFileOfOpenScene();
        try
        {
            RoadNetwork network = CityMapLoader.LoadRoadNetwork(file, new SimulationSettings());
            List<string> problems = CityMapValidator.FindProblems(network);

            var report = new StringBuilder();
            report.AppendLine($"City map {file}:");
            report.Append(CityMapValidator.Describe(network));
            if (problems.Count == 0)
            {
                report.AppendLine("No problems found.");
                Debug.Log(report.ToString());
            }
            else
            {
                report.AppendLine($"{problems.Count} problem(s):");
                foreach (string problem in problems) report.AppendLine("  " + problem);
                Debug.LogWarning(report.ToString());
            }
        }
        catch (CityMapException problem)
        {
            Debug.LogError(problem.Message);
        }
    }

    [MenuItem("Traffic Simulation/Open City Map in Excel")]
    static void OpenCityMap()
    {
        EditorUtility.OpenWithDefaultApp(CityMapLoader.PathTo(CityMapFileOfOpenScene()));
    }

    static string CityMapFileOfOpenScene()
    {
        var simulation = Object.FindFirstObjectByType<SimulationManager>();
        return simulation != null ? simulation.cityMapFile : "CityMap.xlsx";
    }
}
