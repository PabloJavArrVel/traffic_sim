using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Traffic Simulation > Experiments: runs the scenarios of Experiments.xlsx without graphics, saves the results
/// (CSV files in ExperimentResults/, next to Assets) and shows the averages of each scenario.
/// "Make Excel report" runs analysis/analyze.py on the results (needs Python with pandas and matplotlib).
///
/// To watch a scenario instead, type its name in the 'scenario' field of the SimulationManager and press Play.
/// </summary>
public class ExperimentsWindow : EditorWindow
{
    List<ExperimentScenario> scenarios = new List<ExperimentScenario>();
    readonly HashSet<string> selected = new HashSet<string>();
    List<ScenarioAverages> results;
    string resultsFolder;
    string problem;
    Vector2 scroll;

    public static string ResultsRoot => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ExperimentResults");

    [MenuItem("Traffic Simulation/Experiments")]
    static void Open() => GetWindow<ExperimentsWindow>("Experiments").LoadScenarios();

    void LoadScenarios()
    {
        try
        {
            scenarios = ExperimentFileLoader.LoadScenarios();
            selected.Clear();
            foreach (ExperimentScenario scenario in scenarios) selected.Add(scenario.Name);
            problem = null;
        }
        catch (ExperimentFileException e)
        {
            problem = e.Message;
        }
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox("Scenarios come from Assets/StreamingAssets/Experiments.xlsx. Each runs once per seed, " +
                                "without graphics. To watch one, type its name in the SimulationManager's 'scenario' field and press Play.",
                                MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload scenarios")) LoadScenarios();
            if (GUILayout.Button("Open Experiments.xlsx")) EditorUtility.OpenWithDefaultApp(ExperimentFileLoader.FilePath);
        }
        if (problem != null) EditorGUILayout.HelpBox(problem, MessageType.Error);

        DrawScenarioList();

        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (GUILayout.Button($"Run {selected.Count} scenario(s)", GUILayout.Height(30))) RunSelected();
        }

        DrawResults();
        EditorGUILayout.EndScrollView();
    }

    void DrawScenarioList()
    {
        EditorGUILayout.LabelField("Scenarios", EditorStyles.boldLabel);
        foreach (ExperimentScenario scenario in scenarios)
        {
            bool isSelected = selected.Contains(scenario.Name);
            string label = $"{scenario.Name}   ({scenario.AmbientCars} cars, {scenario.Taxis} taxis, " +
                           $"{scenario.DurationMinutes:0} min x {scenario.Seeds} seeds)";
            bool nowSelected = EditorGUILayout.ToggleLeft(label, isSelected);
            if (nowSelected && !isSelected) selected.Add(scenario.Name);
            if (!nowSelected && isSelected) selected.Remove(scenario.Name);
        }
    }

    void DrawResults()
    {
        if (results == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Averages over all seeds", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(ScenarioAverages.Table(results), MonospaceStyle());
        EditorGUILayout.LabelField("Saved in", resultsFolder);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open results folder")) EditorUtility.RevealInFinder(resultsFolder);
            if (GUILayout.Button("Make Excel report (Python)")) MakeExcelReport(resultsFolder);
        }
    }

    void RunSelected()
    {
        var toRun = scenarios.FindAll(scenario => selected.Contains(scenario.Name));
        try
        {
            CityMap map = CityMapLoader.LoadCityMap("CityMap.xlsx");
            List<ExperimentRun> runs = ExperimentRunner.RunAll(map, toRun, (message, progress) =>
                !EditorUtility.DisplayCancelableProgressBar("Running experiments", message, progress));

            resultsFolder = Path.Combine(ResultsRoot, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            ExperimentResultsWriter.Write(resultsFolder, runs);
            results = ScenarioAverages.From(runs);
            Debug.Log($"[Experiments] {runs.Count} runs saved in {resultsFolder}\n{ScenarioAverages.Table(results)}");
        }
        catch (CityMapException e)
        {
            problem = e.Message;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>Runs analysis/analyze.py on a results folder; it writes report.xlsx and charts into that folder.</summary>
    public static void MakeExcelReport(string folder)
    {
        string projectFolder = Directory.GetParent(Application.dataPath).FullName;
        string script = Path.Combine(projectFolder, "analysis", "analyze.py");

        foreach (string python in new[] { "python3", "python" })
        {
            try
            {
                var start = new ProcessStartInfo(python, $"\"{script}\" \"{folder}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0) Debug.Log($"[Experiments] Report ready.\n{output}");
                    else Debug.LogError($"[Experiments] analyze.py failed:\n{output}");
                    return;
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // This python command doesn't exist here; try the next one.
            }
        }
        Debug.LogError("[Experiments] Python was not found. Install Python 3 with pandas, matplotlib and openpyxl (see analysis/README.md).");
    }

    static GUIStyle MonospaceStyle()
    {
        var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
        style.font = Font.CreateDynamicFontFromOSFont(new[] { "Menlo", "Consolas", "Courier New" }, 11);
        return style;
    }
}
