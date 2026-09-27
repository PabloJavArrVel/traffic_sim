using System;
using System.Collections.Generic;

/// <summary>
/// A car driving through the city. Ambient cars and taxis are both vehicles.
/// (This class is split in three files: this one, Vehicle.LookingAhead.cs and Vehicle.Routes.cs.)
///
/// A street cell has room for one car: two cars on the same cell have crashed.
///
/// Law-abiding drivers (every taxi and most ambient cars) never crash into each other. They only drive into cells they
/// have reserved, and they follow every rule in TrafficRules: lights, speed limits and right of way.
/// Rebels ignore every rule. They run red lights, speed, don't give way, and drive into cells other cars have
/// reserved. They still brake for cars they can see in front of them, but when they can't stop in time, they crash.
///
/// Every simulation step a car:
///   Deliberate - decides where to go (PlanRoute), looks at the road ahead (reserving the free cells it will need
///                soon), finds the first thing it must stop for and chooses its speed so it stops smoothly before it
///                (Intelligent Driver Model, see CarFollowing).
///   Act        - moves forward and gives back the cells it has left behind.
/// A crashed car does nothing until the World tows it away.
/// </summary>
public abstract partial class Vehicle : Agent
{
    // The cells this car holds, in driving order: the cells under it first, then the ones reserved ahead.
    readonly List<RoadCell> heldCells = new List<RoadCell>();

    // How long we had been stuck when we last looked for another way (we look again every few seconds stuck).
    float secondsStuckAtLastDetour;

    protected Vehicle(int id, RoadCell cellBehind, RoadCell startCell, DriverProfile driver, float length, bool followsTrafficRules)
    {
        Id = id;
        Driver = driver;
        Length = length;
        FollowsTrafficRules = followsTrafficRules;
        PlaceAt(cellBehind, startCell);
    }

    public int Id { get; }
    public DriverProfile Driver { get; }
    public float Length { get; }
    public VehiclePath Path { get; private set; }

    /// <summary>False for rebels, who ignore every traffic rule.</summary>
    public bool FollowsTrafficRules { get; }

    /// <summary>Meters per second.</summary>
    public float Speed { get; private set; }

    /// <summary>The direction the car is driving right now.</summary>
    public Direction Heading => Path.FrontLink.Heading;

    /// <summary>The cell the front bumper is on (cells meet halfway between their centers).</summary>
    public RoadCell CellAtFront => Path.FrontDistance < Path.FrontLink.Length / 2f ? Path.FrontLink.From : Path.FrontLink.To;

    /// <summary>What made the car slow down or stop in the last step (for the HUD and for debugging).</summary>
    public ObstacleReason WaitingFor { get; private set; }

    /// <summary>How long the car has been stopped for an unusual reason (red lights and arrivals don't count).</summary>
    public float SecondsStuck { get; private set; }

    /// <summary>Meters driven since the car appeared.</summary>
    public float DistanceDriven { get; private set; }

    /// <summary>Simulation time when the car started waiting at a junction (infinity when it isn't waiting).</summary>
    public float WaitingSince { get; private set; } = float.PositiveInfinity;

    /// <summary>Simulation time of the car's crash, or NaN if it isn't crashed.</summary>
    public float CrashedAt { get; private set; } = float.NaN;

    public bool IsCrashed => !float.IsNaN(CrashedAt);

    /// <summary>How many times a tow truck has moved the car to a new place after a crash.</summary>
    public int TimesTowed { get; private set; }

    public IReadOnlyList<RoadCell> HeldCells => heldCells;

    /// <summary>Decides where the car is going. Called every step before the car looks at the road ahead.</summary>
    protected abstract void PlanRoute(World world);

    /// <summary>Called after the car was towed to a new place, so it can plan its route again.</summary>
    protected virtual void OnTowed(World world) { }

    /// <summary>A new car decides where it is going as soon as it appears, so other drivers can see where it will drive.</summary>
    public void PlanFirstRoute(World world) => PlanRoute(world);

    // ------------------------------------------------------------------
    // The agent loop
    // ------------------------------------------------------------------

    public override void Deliberate(World world)
    {
        if (IsCrashed)
        {
            Speed = 0f;
            WaitingFor = ObstacleReason.Crash;
            return;
        }

        PlanRoute(world);

        bool stuckForAWhile = SecondsStuck - secondsStuckAtLastDetour > world.Settings.SecondsStuckBeforeLookingForAnotherWay;
        if (stuckForAWhile)
        {
            LookForAnotherWay(world);
            secondsStuckAtLastDetour = SecondsStuck;
        }

        Obstacle obstacle = FollowsTrafficRules ? LookAheadFollowingTheRules(world) : LookAheadIgnoringTheRules(world);
        WaitingFor = obstacle.Reason;
        Speed = ChooseSpeed(obstacle, world);
    }

    public override void Act(World world)
    {
        if (IsCrashed) return;

        DriveForward(world);
        GiveBackCellsBehind();
        CountSecondsStuck(world);
    }

    // ------------------------------------------------------------------
    // Speed
    // ------------------------------------------------------------------

    float ChooseSpeed(Obstacle obstacle, World world)
    {
        SimulationSettings settings = world.Settings;
        float desiredSpeed = FollowsTrafficRules ? CellAtFront.SpeedLimit * Driver.SpeedFactor : settings.RebelSpeed;
        desiredSpeed = Math.Min(desiredSpeed, SpeedAllowedBeforeWhatIsAhead(settings));

        // At the end of the route we want to stop right at the destination, not a gap before it.
        float stoppedGap = obstacle.Reason == ObstacleReason.EndOfRoute ? 0f : settings.StoppedGap;

        float acceleration = CarFollowing.Acceleration(Speed, desiredSpeed, obstacle, stoppedGap, Driver);
        float newSpeed = Math.Max(0f, Speed + acceleration * world.DeltaTime);

        // Law-abiding drivers never go over the speed limit, not even for a moment.
        if (FollowsTrafficRules) newSpeed = Math.Min(newSpeed, SlowestLimitWithin(newSpeed * world.DeltaTime));
        return newSpeed;
    }

    /// <summary>The lowest speed limit of the cells the front bumper will be on while driving the next 'distance' meters.</summary>
    float SlowestLimitWithin(float distance)
    {
        float slowest = CellAtFront.SpeedLimit;
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
        {
            if (Path.DistanceToEntryOf(i) > distance) break;
            slowest = Math.Min(slowest, Path.Cells[i].SpeedLimit);
        }
        return slowest;
    }

    /// <summary>
    /// Everybody slows down before corners; law-abiding drivers also slow down before entering a slower street.
    /// For each of those points ahead we brake so we reach it at its speed (speed² = target² + 2 · braking · distance).
    /// </summary>
    float SpeedAllowedBeforeWhatIsAhead(SimulationSettings settings)
    {
        float lookAhead = BrakingDistance(settings.RebelSpeed) + settings.MetersPerCell;
        float allowed = float.PositiveInfinity;

        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
        {
            float distanceToCell = Path.DistanceToEntryOf(i);
            if (distanceToCell > lookAhead) break;

            if (FollowsTrafficRules)
                allowed = Math.Min(allowed, SpeedToArriveAt(Path.Cells[i].SpeedLimit, distanceToCell));

            bool turnsAtThisCell = i < Path.LastIndex && Path.LinkAt(i - 1).Heading != Path.LinkAt(i).Heading;
            if (turnsAtThisCell)
                allowed = Math.Min(allowed, SpeedToArriveAt(settings.TurnSpeed, Path.DistanceToCenterOf(i)));
        }
        return allowed;
    }

    float SpeedToArriveAt(float targetSpeed, float distance)
    {
        return MathF.Sqrt(targetSpeed * targetSpeed + 2f * Driver.ComfortableBraking * Math.Max(0f, distance));
    }

    float BrakingDistance(float speed) => speed * speed / (2f * Driver.ComfortableBraking);

    // ------------------------------------------------------------------
    // Moving
    // ------------------------------------------------------------------

    void DriveForward(World world)
    {
        float seconds = world.DeltaTime;
        float distance = Speed * seconds;

        // Law-abiding cars never drive past the cells they hold. Rebels drive on (and crash if something is there).
        if (FollowsTrafficRules)
        {
            float allowed = Math.Max(0f, DistanceToFirstCellNotHeld());
            if (distance > allowed)
            {
                distance = allowed;
                Speed = distance / seconds;
            }
        }
        distance = Math.Min(distance, Path.RemainingLength);

        CountRedLightIfWeRunOne(distance, world);
        Path.AdvanceFront(distance);
        DistanceDriven += distance;
    }

    float DistanceToFirstCellNotHeld()
    {
        int index = FirstPathIndexNotHeld();
        return index > Path.LastIndex ? Path.RemainingLength : Path.DistanceToEntryOf(index);
    }

    /// <summary>Crossing a stop line while its light is red is recorded as a violation (only rebels do it).</summary>
    void CountRedLightIfWeRunOne(float distanceThisStep, World world)
    {
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
        {
            float distanceToCell = Path.DistanceToEntryOf(i);
            if (distanceToCell > distanceThisStep) return;

            TrafficLight light = Path.Cells[i - 1].TrafficLight;
            bool crossesTheStopLineNow = distanceToCell > 0f;
            if (light != null && crossesTheStopLineNow && light.Color == LightColor.Red)
                world.Metrics.RecordRedLightRun();
        }
    }

    void GiveBackCellsBehind()
    {
        List<RoadCell> cellsUnderCar = Path.CellsUnderCar(Length);
        while (heldCells.Count > 0 && !cellsUnderCar.Contains(heldCells[0]))
        {
            heldCells[0].Release(this);
            heldCells.RemoveAt(0);
        }
        Path.ForgetCellsFarBehind();
    }

    void CountSecondsStuck(World world)
    {
        bool normalWait = WaitingFor == ObstacleReason.RedLight || WaitingFor == ObstacleReason.EndOfRoute;
        bool stopped = Speed < 0.1f;
        if (stopped && !normalWait)
        {
            SecondsStuck += world.DeltaTime;
            world.Statistics.RecordStop(SecondsStuck);
        }
        else
        {
            SecondsStuck = 0f;
            secondsStuckAtLastDetour = 0f;
        }
    }

    void Take(RoadCell cell)
    {
        if (cell.Holder == this) return;
        cell.Reserve(this);
        heldCells.Add(cell);
    }

    void GiveBackAllCells()
    {
        foreach (RoadCell cell in heldCells) cell.Release(this);
        heldCells.Clear();
    }

    int FirstPathIndexNotHeld()
    {
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
            if (Path.Cells[i].Holder != this) return i;
        return Path.LastIndex + 1;
    }

    // ------------------------------------------------------------------
    // Crashes
    // ------------------------------------------------------------------

    /// <summary>The car hit (or was hit by) another car: it stops where it is and waits for the tow truck.</summary>
    public void Crash(float now)
    {
        if (IsCrashed) return;
        CrashedAt = now;
        Speed = 0f;
        WaitingSince = float.PositiveInfinity;
    }

    /// <summary>The tow truck takes the car away: it gives back every cell and starts again somewhere else.</summary>
    public void TowTo(RoadCell cellBehind, RoadCell startCell, World world)
    {
        GiveBackAllCells();
        PlaceAt(cellBehind, startCell);
        CrashedAt = float.NaN;
        TimesTowed++;
        OnTowed(world);
    }

    void PlaceAt(RoadCell cellBehind, RoadCell startCell)
    {
        Path = new VehiclePath(cellBehind, startCell);
        Speed = 0f;
        SecondsStuck = 0f;
        secondsStuckAtLastDetour = 0f;
        foreach (RoadCell cell in Path.CellsUnderCar(Length)) Take(cell);
    }
}
