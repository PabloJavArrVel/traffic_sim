using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Starts and runs the simulation. When you press Play it:
///   1. reads the city map (Assets/StreamingAssets/CityMap.xlsx) and builds the road network,
///   2. creates the World with the cars, taxis and pedestrians of the scenario,
///   3. every frame advances the World in small fixed steps and adds new pedestrians from time to time.
///
/// The scenario is the numbers below, or, if you type a name in 'scenario', that row of Experiments.xlsx.
/// The GameObjects you see are created and moved by the WorldView.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    /// <summary>If a frame took very long, don't try to catch up more than this many steps at once.</summary>
    const int MaxStepsPerFrame = 10;

    [Header("City map")]
    [Tooltip("Excel file inside Assets/StreamingAssets. The simulation reads its 'Map' sheet.")]
    public string cityMapFile = "CityMap.xlsx";

    [Tooltip("Where the center of cell A1 of the map is in the scene.")]
    public Vector3 cellA1Position = Vector3.zero;

    [Tooltip("Size of one map cell in Unity units (one road tile).")]
    public float cellSize = 1.2f;

    [Header("Scenario")]
    [Tooltip("Empty: use the numbers below. Or the Name of a row of Assets/StreamingAssets/Experiments.xlsx.")]
    public string scenario = "";

    [Header("Scene references")]
    public WorldView worldView;

    [Tooltip("Optional: the follow camera, so it can follow cars, taxis and pedestrians.")]
    public CameraFollowController followCam;

    [Header("Traffic")]
    public int ambientCarCount = 30;
    public int taxiCount = 3;

    [Tooltip("0 = every driver in a hurry, 1 = every driver very calm. Each driver gets a random value between min and max.")]
    [Range(0f, 1f)] public float driverCalmnessMin = 0f;
    [Range(0f, 1f)] public float driverCalmnessMax = 1f;

    [Tooltip("Share of ambient drivers who ignore every traffic rule (shown in red). Taxis always follow the rules.")]
    [Range(0f, 1f)] public float rebelShare = 0f;

    [Tooltip("Speed limit of one-lane streets and roundabouts.")]
    public float streetSpeedLimitKmh = 30f;

    [Tooltip("Speed limit of avenues (two lanes side by side).")]
    public float avenueSpeedLimitKmh = 50f;

    public float greenLightSeconds = 15f;

    [Header("Pedestrians")]
    public int initialPedestrians = 3;
    public int maxPedestrians = 5;

    [Tooltip("Seconds between new pedestrians appearing.")]
    public float secondsBetweenPedestrians = 30f;

    [Tooltip("How long a pedestrian waits for a taxi to be assigned before giving up.")]
    public float pedestrianPatienceSeconds = 120f;

    [Header("Simulation")]
    [Tooltip("1 = real time, 2 = twice as fast, 0 = paused.")]
    [Range(0f, 5f)] public float simulationSpeed = 1f;

    [Tooltip("The same seed gives the same traffic every run. 0 = different every run.")]
    public int randomSeed = 0;

    float secondsNotSimulatedYet;
    PedestrianSpawner pedestrianSpawner;

    public World World { get; private set; }
    public MapPlacement Placement { get; private set; }

    /// <summary>The scenario being played (from the inspector or from Experiments.xlsx).</summary>
    public ExperimentScenario Scenario { get; private set; }

    /// <summary>How far we are from the last simulation step to the next one (0 to 1). Used to draw smooth movement.</summary>
    public float StepProgress => World == null ? 0f : secondsNotSimulatedYet / World.Settings.SimulationStepSeconds;

    void Start()
    {
        try
        {
            Scenario = string.IsNullOrWhiteSpace(scenario) ? ScenarioFromInspector() : ExperimentFileLoader.LoadScenario(scenario);
            CreateWorld();
        }
        catch (Exception problem) when (problem is CityMapException || problem is ExperimentFileException)
        {
            Debug.LogError(problem.Message);
            enabled = false;
            return;
        }

        Scenario.Populate(World);
        pedestrianSpawner = new PedestrianSpawner(Scenario);

        worldView.Initialize(this);
        worldView.ShowTrafficLights(World.Network);
        foreach (Vehicle vehicle in World.Vehicles) ShowVehicle(vehicle);
        foreach (Pedestrian pedestrian in World.Pedestrians) ShowPedestrian(pedestrian);

        Debug.Log($"[Simulation] Scenario '{Scenario.Name}'. {CityMapValidator.Describe(World.Network)}" +
                  $"{World.Vehicles.Count} vehicles and {World.Pedestrians.Count} pedestrians created.");
    }

    ExperimentScenario ScenarioFromInspector()
    {
        return new ExperimentScenario
        {
            Name = "Inspector",
            AmbientCars = ambientCarCount,
            Taxis = taxiCount,
            DriverCalmnessMin = driverCalmnessMin,
            DriverCalmnessMax = driverCalmnessMax,
            RebelShare = rebelShare,
            StreetSpeedLimitKmh = streetSpeedLimitKmh,
            AvenueSpeedLimitKmh = avenueSpeedLimitKmh,
            GreenLightSeconds = greenLightSeconds,
            InitialPedestrians = initialPedestrians,
            MaxPedestrians = maxPedestrians,
            SecondsBetweenPedestrians = secondsBetweenPedestrians,
            PedestrianPatienceSeconds = pedestrianPatienceSeconds
        };
    }

    void CreateWorld()
    {
        SimulationSettings settings = Scenario.CreateSettings();
        RoadNetwork network = CityMapLoader.LoadRoadNetwork(cityMapFile, settings);
        Placement = new MapPlacement(cellA1Position, cellSize, settings.MetersPerCell);

        int seed = randomSeed != 0 ? randomSeed : Environment.TickCount;
        World = new World(network, settings, seed);
    }

    void Update()
    {
        RunSimulationSteps(Time.deltaTime * simulationSpeed);
        RemoveFinishedPedestrians();
    }

    void RunSimulationSteps(float seconds)
    {
        float step = World.Settings.SimulationStepSeconds;
        secondsNotSimulatedYet += seconds;

        int steps = 0;
        while (secondsNotSimulatedYet >= step && steps < MaxStepsPerFrame)
        {
            World.Tick(step);
            ShowPedestrian(pedestrianSpawner.Update(World));
            secondsNotSimulatedYet -= step;
            steps++;
        }

        // Too far behind (the game was paused or a frame took very long): skip the rest instead of freezing.
        if (steps == MaxStepsPerFrame) secondsNotSimulatedYet = Math.Min(secondsNotSimulatedYet, step);
    }

    void RemoveFinishedPedestrians()
    {
        List<Pedestrian> finished = World.RemoveFinishedPedestrians();
        foreach (Pedestrian pedestrian in finished)
        {
            if (followCam != null) followCam.Forget(pedestrian);
            worldView.RemovePedestrian(pedestrian);
        }
    }

    void ShowVehicle(Vehicle vehicle)
    {
        GameObject shown = worldView.ShowVehicle(vehicle);
        if (followCam != null && shown != null) followCam.Follow(vehicle, shown.transform);
    }

    void ShowPedestrian(Pedestrian pedestrian)
    {
        if (pedestrian == null) return;
        GameObject shown = worldView.ShowPedestrian(pedestrian);
        if (followCam != null && shown != null) followCam.Follow(pedestrian, shown.transform);
    }
}
