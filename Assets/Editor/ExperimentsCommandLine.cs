using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs every scenario of Experiments.xlsx from a terminal, for long batches:
///
///   Unity -batchmode -quit -projectPath . -executeMethod ExperimentsCommandLine.RunAll [-excelReport]
///
/// Results go to ExperimentResults/&lt;date&gt;/. With -excelReport it also runs analysis/analyze.py.
/// </summary>
public static class ExperimentsCommandLine
{
    public static void RunAll()
    {
        try
        {
            CityMap map = CityMapLoader.LoadCityMap("CityMap.xlsx");
            List<ExperimentScenario> scenarios = ExperimentFileLoader.LoadScenarios();

            List<ExperimentRun> runs = ExperimentRunner.RunAll(map, scenarios, (message, progress) =>
            {
                Console.WriteLine($"[Experiments] {progress:P0} {message}");
                return true;
            });

            string folder = Path.Combine(ExperimentsWindow.ResultsRoot, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            ExperimentResultsWriter.Write(folder, runs);
            Debug.Log($"[Experiments] {runs.Count} runs saved in {folder}\n{ScenarioAverages.Table(ScenarioAverages.From(runs))}");

            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-excelReport") >= 0)
                ExperimentsWindow.MakeExcelReport(folder);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            EditorApplication.Exit(1);
        }
    }
}
