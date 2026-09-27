using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Starts and runs the simulation. When you press Play it:
///   1. reads the city map (Assets/StreamingAssets/CityMap.xlsx) and builds the road network,
///   2. creates the World with ambient cars, taxis and a few pedestrians,
///   3. every frame advances the World in small fixed steps and adds new pedestrians from time to time.
/// The GameObjects you see are created and moved by the WorldView.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    /// <summary>The simulation always advances in steps of this many seconds, whatever the frame rate.</summary>
    public const float StepSeconds = 0.05f;

    /// <summary>If a frame took very long, don't try to catch up more than this many steps at once.</summary>
    const int MaxStepsPerFrame = 10;

    [Header("City map")]
    [Tooltip("Excel file inside Assets/StreamingAssets. The simulation reads its 'Map' sheet.")]
    public string cityMapFile = "CityMap.xlsx";

    [Tooltip("Where the center of cell A1 of the map is in the scene.")]
    public Vector3 cellA1Position = Vector3.zero;

    [Tooltip("Size of one map cell in Unity units (one road tile).")]
    public float cellSize = 1.2f;

    [Header("Scene references")]
    public WorldView worldView;

    [Tooltip("Optional: the follow camera, so it can follow cars, taxis and pedestrians.")]
    public CameraFollowController followCam;

    [Header("Traffic")]
    public int ambientCarCount = 30;
    public int taxiCount = 3;

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
    float secondsUntilNextPedestrian;

    public World World { get; private set; }
    public MapPlacement Placement { get; private set; }

    /// <summary>How far we are from the last simulation step to the next one (0 to 1). Used to draw smooth movement.</summary>
    public float StepProgress => secondsNotSimulatedYet / StepSeconds;

    void Start()
    {
        try
        {
            CreateWorld();
        }
        catch (CityMapException problem)
        {
            Debug.LogError(problem.Message);
            enabled = false;
            return;
        }

        worldView.Initialize(this);
        worldView.ShowTrafficLights(World.Network);

        for (int i = 0; i < ambientCarCount; i++) ShowVehicle(World.SpawnAmbientCar());
        for (int i = 0; i < taxiCount; i++) ShowVehicle(World.SpawnTaxi());
        for (int i = 0; i < initialPedestrians; i++) ShowPedestrian(World.SpawnPedestrian(pedestrianPatienceSeconds));
        secondsUntilNextPedestrian = secondsBetweenPedestrians;

        Debug.Log($"[Simulation] {CityMapValidator.Describe(World.Network)}" +
                  $"{World.Vehicles.Count} vehicles and {World.Pedestrians.Count} pedestrians created.");
    }

    void CreateWorld()
    {
        var settings = new SimulationSettings();
        RoadNetwork network = CityMapLoader.LoadRoadNetwork(cityMapFile, settings);
        Placement = new MapPlacement(cellA1Position, cellSize, settings.MetersPerCell);

        int seed = randomSeed != 0 ? randomSeed : Environment.TickCount;
        World = new World(network, settings, seed);
    }

    void Update()
    {
        float simulatedSeconds = Time.deltaTime * simulationSpeed;
        RunSimulationSteps(simulatedSeconds);
        AddPedestriansFromTimeToTime(simulatedSeconds);
        RemoveFinishedPedestrians();
    }

    void RunSimulationSteps(float seconds)
    {
        secondsNotSimulatedYet += seconds;

        int steps = 0;
        while (secondsNotSimulatedYet >= StepSeconds && steps < MaxStepsPerFrame)
        {
            World.Tick(StepSeconds);
            secondsNotSimulatedYet -= StepSeconds;
            steps++;
        }

        // Too far behind (the game was paused or a frame took very long): skip the rest instead of freezing.
        if (steps == MaxStepsPerFrame) secondsNotSimulatedYet = Math.Min(secondsNotSimulatedYet, StepSeconds);
    }

    void AddPedestriansFromTimeToTime(float seconds)
    {
        secondsUntilNextPedestrian -= seconds;
        if (secondsUntilNextPedestrian > 0f) return;

        secondsUntilNextPedestrian = secondsBetweenPedestrians;
        if (World.Pedestrians.Count < maxPedestrians)
            ShowPedestrian(World.SpawnPedestrian(pedestrianPatienceSeconds));
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
        if (vehicle == null) return;   // no free place left in the city
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
