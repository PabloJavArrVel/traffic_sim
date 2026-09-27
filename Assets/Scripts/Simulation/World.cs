using System;
using System.Collections.Generic;

/// <summary>
/// Everything in the simulation: the road network, the agents (cars, taxis, pedestrians and the taxi dispatcher)
/// and the clock.
///
/// Each Tick moves time forward a little: first the traffic lights change if it's their time, then every agent
/// perceives, then every agent deliberates, then every agent acts (see Agent).
/// </summary>
public class World
{
    readonly List<Agent> agents = new List<Agent>();
    readonly List<Vehicle> vehicles = new List<Vehicle>();
    readonly List<Pedestrian> pedestrians = new List<Pedestrian>();

    // Cells where cars appear and drive to: in the connected part of the city, not in a junction, without a light.
    readonly List<RoadCell> ordinaryCells = new List<RoadCell>();

    // Ordinary cells with a sidewalk on their right side: where people wait for a taxi.
    readonly List<RoadCell> curbCells = new List<RoadCell>();

    int nextVehicleId = 1;
    int nextPedestrianId = 1;

    public World(RoadNetwork network, SimulationSettings settings, int randomSeed)
    {
        Network = network;
        Settings = settings;
        Random = new Random(randomSeed);
        FleetManager = new FleetManager();
        agents.Add(FleetManager);
        FindOrdinaryAndCurbCells();
    }

    public RoadNetwork Network { get; }
    public SimulationSettings Settings { get; }
    public Random Random { get; }
    public FleetManager FleetManager { get; }
    public SimulationStatistics Statistics { get; } = new SimulationStatistics();

    /// <summary>Measurements taken while the simulation runs (speeds, queues, rides...), for experiments.</summary>
    public SimulationMetrics Metrics { get; } = new SimulationMetrics();

    /// <summary>Seconds since the simulation started.</summary>
    public float Time { get; private set; }

    /// <summary>Length of the current step, in seconds.</summary>
    public float DeltaTime { get; private set; }

    public IReadOnlyList<Vehicle> Vehicles => vehicles;
    public IReadOnlyList<Pedestrian> Pedestrians => pedestrians;

    public void Tick(float seconds)
    {
        DeltaTime = seconds;
        Time += seconds;

        foreach (TrafficLightController intersection in Network.TrafficLightControllers)
            intersection.Tick(seconds);

        foreach (Agent agent in agents) agent.Perceive(this);
        foreach (Agent agent in agents) agent.Deliberate(this);
        foreach (Agent agent in agents) agent.Act(this);

        Metrics.RecordStep(this);
    }

    // ------------------------------------------------------------------
    // Adding agents
    // ------------------------------------------------------------------

    /// <summary>Adds a car at a random free place. Returns null if there was no free place.</summary>
    public AmbientCar SpawnAmbientCar()
    {
        if (!TryFindPlaceForNewCar(out RoadCell cellBehind, out RoadCell startCell)) return null;

        DriverProfile driver = DriverProfile.RandomDriver(Random, Settings.DriverCalmnessMin, Settings.DriverCalmnessMax);
        var car = new AmbientCar(nextVehicleId++, cellBehind, startCell, driver, Settings.CarLength);
        AddVehicle(car);
        return car;
    }

    /// <summary>Adds a taxi at a random free place. Returns null if there was no free place.</summary>
    public Taxi SpawnTaxi()
    {
        if (!TryFindPlaceForNewCar(out RoadCell cellBehind, out RoadCell startCell)) return null;

        var taxi = new Taxi(nextVehicleId++, cellBehind, startCell, Settings.CarLength);
        AddVehicle(taxi);
        FleetManager.AddTaxi(taxi);
        return taxi;
    }

    /// <summary>Adds a person waiting for a taxi at a random sidewalk. Returns null if there was no free sidewalk.</summary>
    public Pedestrian SpawnPedestrian(float patienceSeconds)
    {
        RoadCell pickup = RandomCurbWithoutPedestrian();
        if (pickup == null) return null;

        RoadCell destination = pickup;
        while (destination == pickup)
            destination = curbCells[Random.Next(curbCells.Count)];

        var pedestrian = new Pedestrian(nextPedestrianId++, pickup, destination, patienceSeconds, requestedAt: Time);
        agents.Add(pedestrian);
        pedestrians.Add(pedestrian);
        return pedestrian;
    }

    /// <summary>Removes the pedestrians who arrived or gave up, and returns them (so their GameObjects can go too).</summary>
    public List<Pedestrian> RemoveFinishedPedestrians()
    {
        List<Pedestrian> finished = pedestrians.FindAll(pedestrian => pedestrian.IsFinished);
        foreach (Pedestrian pedestrian in finished)
        {
            pedestrians.Remove(pedestrian);
            agents.Remove(pedestrian);
        }
        return finished;
    }

    /// <summary>A random place for a car to drive to.</summary>
    public RoadCell PickRandomDestination()
    {
        return ordinaryCells[Random.Next(ordinaryCells.Count)];
    }

    void AddVehicle(Vehicle vehicle)
    {
        agents.Add(vehicle);
        vehicles.Add(vehicle);
    }

    /// <summary>
    /// A new car needs a free ordinary cell for its front and the free cell behind it for its rear
    /// (a car is 4.5 m long and a cell 5.4 m, so the rear sticks into the previous cell).
    /// </summary>
    bool TryFindPlaceForNewCar(out RoadCell cellBehind, out RoadCell startCell)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            RoadCell candidate = ordinaryCells[Random.Next(ordinaryCells.Count)];
            if (!candidate.IsFree) continue;

            CellLink wayIn = OnlyDriveLinkInto(candidate);
            if (wayIn == null || !wayIn.From.IsFree || wayIn.From.IsJunction) continue;

            cellBehind = wayIn.From;
            startCell = candidate;
            return true;
        }

        cellBehind = null;
        startCell = null;
        return false;
    }

    static CellLink OnlyDriveLinkInto(RoadCell cell)
    {
        CellLink found = null;
        foreach (CellLink link in cell.Entrances)
        {
            if (link.IsLaneChange) continue;
            if (found != null) return null;   // more than one way in
            found = link;
        }
        return found;
    }

    RoadCell RandomCurbWithoutPedestrian()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            RoadCell candidate = curbCells[Random.Next(curbCells.Count)];
            bool taken = pedestrians.Exists(pedestrian => pedestrian.PickupCell == candidate);
            if (!taken) return candidate;
        }
        return null;
    }

    void FindOrdinaryAndCurbCells()
    {
        HashSet<RoadCell> connected = ConnectedCells.LargestGroup(Network);
        foreach (RoadCell cell in Network.Cells)
        {
            bool ordinary = connected.Contains(cell) && !cell.IsJunction && cell.TrafficLight == null;
            if (!ordinary) continue;

            ordinaryCells.Add(cell);
            if (HasSidewalkOnTheRight(cell)) curbCells.Add(cell);
        }

        if (ordinaryCells.Count == 0)
            throw new CityMapException("The city map has no streets cars can drive on.");
        if (curbCells.Count < 2)
            throw new CityMapException("The city map needs at least two streets with a sidewalk for pedestrians.");
    }

    /// <summary>True when the cell to the right of the traffic is not a street (so there is a sidewalk).</summary>
    public bool HasSidewalkOnTheRight(RoadCell cell)
    {
        Direction right = cell.TrafficDirection.TurnRight();
        return Network.CellAt(cell.Position.Step(right)) == null;
    }
}
