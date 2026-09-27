using System;
using System.Collections.Generic;

/// <summary>
/// A car driving through the city (ambient cars and taxis are both vehicles).
///
/// The rule that keeps cars from overlapping, even at junctions:
///     a street cell belongs to at most one car at a time, and a car only drives into cells it holds.
///
/// Every simulation step a car:
///   Deliberate - decides where to go (PlanRoute), reserves the free cells it will need soon, finds the first thing
///                it must stop for (a red light, a car, a busy junction...) and chooses its speed so it stops smoothly
///                before it (Intelligent Driver Model, see CarFollowing).
///   Act        - moves forward, never past the cells it holds, and gives back the cells it has left behind.
/// </summary>
public abstract class Vehicle : Agent
{
    // The cells this car holds, in driving order: the cells under it first, then the ones reserved ahead.
    readonly List<RoadCell> heldCells = new List<RoadCell>();

    // Turn-taking at junctions: the cells we are stopped waiting for (null when not waiting).
    List<RoadCell> cellsWaitingFor;

    // How long we had been stuck when we last looked for another way (we look again every few seconds stuck).
    float secondsStuckAtLastDetour;

    protected Vehicle(int id, RoadCell cellBehind, RoadCell startCell, DriverProfile driver, float length)
    {
        Id = id;
        Driver = driver;
        Length = length;
        Path = new VehiclePath(cellBehind, startCell);
        WaitingSince = float.PositiveInfinity;

        foreach (RoadCell cell in Path.CellsUnderCar(Length))
            Take(cell);
    }

    public int Id { get; }
    public DriverProfile Driver { get; }
    public float Length { get; }
    public VehiclePath Path { get; }

    /// <summary>Meters per second.</summary>
    public float Speed { get; private set; }

    /// <summary>The direction the car is driving right now.</summary>
    public Direction Heading => Path.FrontLink.Heading;

    /// <summary>What made the car slow down or stop in the last step (for the HUD and for debugging).</summary>
    public ObstacleReason WaitingFor { get; private set; }

    /// <summary>How long the car has been stopped for an unusual reason (red lights and arrivals don't count).</summary>
    public float SecondsStuck { get; private set; }

    /// <summary>Meters driven since the car appeared.</summary>
    public float DistanceDriven { get; private set; }

    /// <summary>Simulation time when the car started waiting at a junction (infinity when it isn't waiting).</summary>
    public float WaitingSince { get; private set; }

    public IReadOnlyList<RoadCell> HeldCells => heldCells;

    /// <summary>Decides where the car is going. Called every step before the car reserves cells ahead.</summary>
    protected abstract void PlanRoute(World world);

    public override void Deliberate(World world)
    {
        PlanRoute(world);

        bool stuckForAWhile = SecondsStuck - secondsStuckAtLastDetour > world.Settings.SecondsStuckBeforeLookingForAnotherWay;
        if (stuckForAWhile)
        {
            LookForAnotherWay(world);
            secondsStuckAtLastDetour = SecondsStuck;
        }

        Obstacle obstacle = ReserveCellsAhead(world);
        WaitingFor = obstacle.Reason;
        Speed = ChooseSpeed(obstacle, world);
    }

    public override void Act(World world)
    {
        DriveForward(world.DeltaTime);
        GiveBackCellsBehind();
        CountSecondsStuck(world);
    }

    // ------------------------------------------------------------------
    // Reserving the road ahead
    // ------------------------------------------------------------------

    /// <summary>
    /// Reserves the cells the car will reach soon (as far as it needs to be able to stop) and returns the first
    /// thing it can't drive past.
    /// </summary>
    Obstacle ReserveCellsAhead(World world)
    {
        float lookAhead = BrakingDistance(Speed) + world.Settings.MetersPerCell;
        bool waitingAtJunction = false;
        bool alreadyTriedKeepingStraight = false;
        Obstacle obstacle = Obstacle.None;

        int index = FirstPathIndexNotHeld();
        while (true)
        {
            if (index > Path.LastIndex)
            {
                obstacle = Obstacle.StopAt(Path.RemainingLength, ObstacleReason.EndOfRoute);
                break;
            }

            float distanceToCell = Path.DistanceToEntryOf(index);
            if (distanceToCell > lookAhead) break;   // far enough away: we'll reserve it in a later step

            CellLink linkIntoCell = Path.LinkAt(index - 1);
            if (MustStopForTrafficLight(linkIntoCell.From, distanceToCell))
            {
                obstacle = Obstacle.StopAt(distanceToCell, ObstacleReason.RedLight);
                break;
            }

            List<RoadCell> cellsNeeded = CellsToTakeTogether(index, out int lastIndexNeeded);
            Vehicle blocker = OtherCarHolding(cellsNeeded) ?? CarThatWaitedLongerFor(cellsNeeded);
            if (blocker == null)
            {
                foreach (RoadCell cell in cellsNeeded) Take(cell);
                index = lastIndexNeeded + 1;
                continue;
            }

            // The planned lane change is blocked: stay in our lane and find another way instead of stopping.
            if (linkIntoCell.IsLaneChange && !alreadyTriedKeepingStraight)
            {
                alreadyTriedKeepingStraight = true;
                if (TryKeepingStraight(index - 1, linkIntoCell, world)) continue;
            }

            if (ContainsJunction(cellsNeeded))
            {
                WaitAtJunction(cellsNeeded, world.Time);
                waitingAtJunction = true;
            }
            obstacle = ObstacleFor(blocker, cellsNeeded, linkIntoCell, distanceToCell);
            break;
        }

        if (!waitingAtJunction) StopWaitingAtJunction();
        return obstacle;
    }

    /// <summary>
    /// Usually just the next cell of the path. Two exceptions:
    ///  - a lane change needs its whole 2x2 block: the cell beside us, the cell ahead of us and the target cell;
    ///  - a crossing is taken together with the cells after it, up to the first cell that isn't a crossing,
    ///    so the car never stops inside a crossing and blocks the traffic crossing its path.
    /// </summary>
    List<RoadCell> CellsToTakeTogether(int index, out int lastIndexNeeded)
    {
        var cells = new List<RoadCell>();
        CellLink linkIntoCell = Path.LinkAt(index - 1);
        lastIndexNeeded = index;

        if (linkIntoCell.IsLaneChange)
        {
            cells.Add(linkIntoCell.CellBeside);
            cells.Add(linkIntoCell.CellAhead);
            cells.Add(linkIntoCell.To);
            return cells;
        }

        cells.Add(Path.Cells[index]);
        while (Path.Cells[lastIndexNeeded].IsCrossing && lastIndexNeeded < Path.LastIndex)
        {
            lastIndexNeeded++;
            cells.Add(Path.Cells[lastIndexNeeded]);
        }
        return cells;
    }

    bool MustStopForTrafficLight(RoadCell cell, float distanceToStopLine)
    {
        TrafficLight light = cell.TrafficLight;
        if (light == null || light.Color == LightColor.Green) return false;
        if (light.Color == LightColor.Red) return true;

        // Yellow: stop if we can do it comfortably, otherwise it's safer to keep going.
        return BrakingDistance(Speed) <= distanceToStopLine;
    }

    Vehicle OtherCarHolding(List<RoadCell> cells)
    {
        foreach (RoadCell cell in cells)
            if (cell.Holder != null && cell.Holder != this) return cell.Holder;
        return null;
    }

    /// <summary>
    /// Taking turns at junctions: if another car has been waiting longer for any of these cells, and those cells
    /// are free for it right now, it goes first.
    /// </summary>
    Vehicle CarThatWaitedLongerFor(List<RoadCell> cells)
    {
        foreach (RoadCell cell in cells)
        {
            foreach (Vehicle other in cell.CarsWaiting)
            {
                bool waitedLonger = other != this && other.WaitingSince < WaitingSince;
                if (waitedLonger && other.CanTakeTheCellsItIsWaitingFor()) return other;
            }
        }
        return null;
    }

    bool CanTakeTheCellsItIsWaitingFor()
    {
        foreach (RoadCell cell in cellsWaitingFor)
            if (cell.Holder != null && cell.Holder != this) return false;
        return true;
    }

    static bool ContainsJunction(List<RoadCell> cells)
    {
        foreach (RoadCell cell in cells)
            if (cell.IsJunction) return true;
        return false;
    }

    void WaitAtJunction(List<RoadCell> cells, float now)
    {
        bool alreadyWaitingHere = cellsWaitingFor != null && cellsWaitingFor[0] == cells[0];
        if (alreadyWaitingHere) return;

        StopWaitingAtJunction();
        cellsWaitingFor = cells;
        WaitingSince = now;
        foreach (RoadCell cell in cells) cell.CarsWaiting.Add(this);
    }

    void StopWaitingAtJunction()
    {
        if (cellsWaitingFor == null) return;
        foreach (RoadCell cell in cellsWaitingFor) cell.CarsWaiting.Remove(this);
        cellsWaitingFor = null;
        WaitingSince = float.PositiveInfinity;
    }

    /// <summary>
    /// A car ahead of us, driving our way and blocking the very next cell, is followed at its speed.
    /// Anything else (cross traffic, a busy junction, a blocked lane change) is treated like a wall.
    /// </summary>
    static Obstacle ObstacleFor(Vehicle blocker, List<RoadCell> cellsNeeded, CellLink linkIntoCell, float distanceToCell)
    {
        bool followingCarAhead = !linkIntoCell.IsLaneChange
                                 && cellsNeeded[0].Holder == blocker
                                 && blocker.Heading == linkIntoCell.Heading;
        if (followingCarAhead)
            return new Obstacle(distanceToCell, blocker.Speed, ObstacleReason.CarAhead);

        ObstacleReason reason = linkIntoCell.IsLaneChange ? ObstacleReason.BusyLane : ObstacleReason.BusyJunction;
        return Obstacle.StopAt(distanceToCell, reason);
    }

    // ------------------------------------------------------------------
    // Speed
    // ------------------------------------------------------------------

    float ChooseSpeed(Obstacle obstacle, World world)
    {
        SimulationSettings settings = world.Settings;
        float desiredSpeed = settings.SpeedLimit * Driver.SpeedFactor;
        desiredSpeed = Math.Min(desiredSpeed, SpeedAllowedBeforeNextTurn(settings));

        // At the end of the route we want to stop right at the destination, not a gap before it.
        float stoppedGap = obstacle.Reason == ObstacleReason.EndOfRoute ? 0f : settings.StoppedGap;

        float acceleration = CarFollowing.Acceleration(Speed, desiredSpeed, obstacle, stoppedGap, Driver);
        return Math.Max(0f, Speed + acceleration * world.DeltaTime);
    }

    /// <summary>Cars slow down before corners: brake so we reach the next corner at turning speed.</summary>
    float SpeedAllowedBeforeNextTurn(SimulationSettings settings)
    {
        float lookAhead = BrakingDistance(settings.SpeedLimit * 1.2f) + settings.MetersPerCell;
        for (int i = Path.FrontIndex; i + 1 < Path.LastIndex; i++)
        {
            float distanceToCorner = Path.DistanceToCenterOf(i + 1);
            if (distanceToCorner > lookAhead) break;

            bool turnsThere = Path.LinkAt(i).Heading != Path.LinkAt(i + 1).Heading;
            if (turnsThere)
                return MathF.Sqrt(settings.TurnSpeed * settings.TurnSpeed + 2f * Driver.ComfortableBraking * Math.Max(0f, distanceToCorner));
        }
        return float.PositiveInfinity;
    }

    float BrakingDistance(float speed) => speed * speed / (2f * Driver.ComfortableBraking);

    // ------------------------------------------------------------------
    // Moving
    // ------------------------------------------------------------------

    void DriveForward(float seconds)
    {
        float distance = Speed * seconds;
        float allowed = Math.Max(0f, DistanceWeMayDrive());
        if (distance > allowed)
        {
            // We reached the end of the cells we hold: stop there.
            distance = allowed;
            Speed = distance / seconds;
        }
        Path.AdvanceFront(distance);
        DistanceDriven += distance;
    }

    /// <summary>How far the front bumper can move while staying on cells we hold.</summary>
    float DistanceWeMayDrive()
    {
        int index = FirstPathIndexNotHeld();
        return index > Path.LastIndex ? Path.RemainingLength : Path.DistanceToEntryOf(index);
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

    int FirstPathIndexNotHeld()
    {
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
            if (Path.Cells[i].Holder != this) return i;
        return Path.LastIndex + 1;
    }

    // ------------------------------------------------------------------
    // Routes
    // ------------------------------------------------------------------

    /// <summary>
    /// Where new routes start: the last path cell we already hold (we can't give those back),
    /// or at least the cell the front bumper is driving into.
    /// </summary>
    protected int IndexWhereNewRoutesStart()
    {
        int lastHeld = FirstPathIndexNotHeld() - 1;
        return Math.Min(Path.LastIndex, Math.Max(Path.FrontIndex + 1, lastHeld));
    }

    /// <summary>Changes the destination. Returns false if there is no way to get there.</summary>
    protected bool DriveTo(RoadCell destination, World world)
    {
        int start = IndexWhereNewRoutesStart();
        List<RoadCell> route = RouteFinder.FindRoute(Path.Cells[start], destination, world.Settings);
        if (route == null) return false;
        Path.ReplaceAfter(start, route);
        return true;
    }

    /// <summary>Adds a trip to a new destination at the end of the current path.</summary>
    protected void ContinueTo(RoadCell destination, World world)
    {
        List<RoadCell> route = RouteFinder.FindRoute(Path.LastCell, destination, world.Settings);
        if (route != null) Path.Extend(route);
    }

    /// <summary>Meters the car would drive to reach a cell (following roads, not in a straight line).</summary>
    public float DrivingDistanceTo(RoadCell destination, World world)
    {
        int start = IndexWhereNewRoutesStart();
        List<RoadCell> route = RouteFinder.FindRoute(Path.Cells[start], destination, world.Settings);
        if (route == null) return float.PositiveInfinity;
        return Path.DistanceToCenterOf(start) + RouteFinder.LengthOf(route);
    }

    bool TryKeepingStraight(int laneChangeStartIndex, CellLink laneChange, World world)
    {
        List<RoadCell> route = RouteFinder.FindRoute(laneChange.CellAhead, Path.LastCell, world.Settings);
        if (route == null) return false;

        route.Insert(0, Path.Cells[laneChangeStartIndex]);
        Path.ReplaceAfter(laneChangeStartIndex, route);
        return true;
    }

    /// <summary>
    /// We have been stuck for a long time (not at a red light): maybe the road ahead is jammed.
    /// Try a route to the same destination that avoids the cell we are waiting for.
    /// </summary>
    void LookForAnotherWay(World world)
    {
        int start = IndexWhereNewRoutesStart();
        if (start >= Path.LastIndex) return;

        RoadCell blockedCell = Path.Cells[start + 1];
        List<RoadCell> route = RouteFinder.FindRoute(Path.Cells[start], Path.LastCell, world.Settings, cellToAvoid: blockedCell);
        if (route == null) return;

        Path.ReplaceAfter(start, route);
        world.Statistics.DetoursTaken++;
    }
}
