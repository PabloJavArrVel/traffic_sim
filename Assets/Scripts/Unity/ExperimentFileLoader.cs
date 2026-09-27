using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Reads Assets/StreamingAssets/Experiments.xlsx. Used by the SimulationManager and the Experiments window.</summary>
public static class ExperimentFileLoader
{
    public const string FileName = "Experiments.xlsx";

    public static string FilePath => Path.Combine(Application.streamingAssetsPath, FileName);

    public static List<ExperimentScenario> LoadScenarios()
    {
        if (!File.Exists(FilePath))
            throw new ExperimentFileException($"Experiments file not found: {FilePath}");

        // FileShare.ReadWrite lets us read the file even while it is open in Excel.
        using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return ExperimentFile.ReadScenarios(memory.ToArray());
        }
    }

    public static ExperimentScenario LoadScenario(string name)
    {
        var names = new List<string>();
        foreach (ExperimentScenario scenario in LoadScenarios())
        {
            if (scenario.Name == name) return scenario;
            names.Add(scenario.Name);
        }
        throw new ExperimentFileException($"There is no scenario called '{name}' in {FileName}. Its scenarios are: {string.Join(", ", names)}.");
    }
}
