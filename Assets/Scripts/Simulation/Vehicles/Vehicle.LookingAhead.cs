using System;
using System.Collections.Generic;

/// <summary>How a vehicle looks at the road ahead: as a law-abiding driver or as a rebel. See Vehicle.cs.</summary>
public abstract partial class Vehicle
{
    // ------------------------------------------------------------------
    // Law-abiding drivers
    // ------------------------------------------------------------------

    /// <summary>
    /// Reserves the cells the car will reach soon (as far as it needs to be able to stop), following every traffic
    /// rule, and returns the first thing it can't drive past.
    /// </summary>
    Obstacle LookAheadFollowingTheRules(World world)
    {
        GiveBackCellsBeyondARedLight();

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
            if (TrafficRules.MustStopForLight(linkIntoCell.From.TrafficLight, BrakingDistance(Speed), distanceToCell))
            {
                obstacle = Obstacle.StopAt(distanceToCell, ObstacleReason.RedLight);
                break;
            }

            List<RoadCell> cellsNeeded = CellsToTakeTogether(index, out int lastIndexNeeded);
            Vehicle blocker = OtherCarIn(cellsNeeded) ?? CarWithRightOfWay(index, lastIndexNeeded, world);
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
                if (float.IsPositiveInfinity(WaitingSince)) WaitingSince = world.Time;
                waitingAtJunction = true;
            }
            obstacle = ObstacleFor(blocker, cellsNeeded[0], linkIntoCell, distanceToCell);
            break;
        }

        if (!waitingAtJunction) WaitingSince = float.PositiveInfinity;
        return Nearest(obstacle, RebelInsideTheCellsWeHold());
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

    /// <summary>
    /// A light can turn red after we reserved the cells past it. If we can still stop comfortably,
    /// a law-abiding driver gives those cells back and stops at the line.
    /// </summary>
    void GiveBackCellsBeyondARedLight()
    {
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
        {
            RoadCell cell = Path.Cells[i];
            if (cell.Holder != this) return;

            float distanceToStopLine = Path.DistanceToEntryOf(i);
            TrafficLight light = Path.Cells[i - 1].TrafficLight;
            bool canStillStop = distanceToStopLine > 0f && BrakingDistance(Speed) <= distanceToStopLine;
            if (light != null && light.Color != LightColor.Green && canStillStop)
            {
                GiveBackHeldCellsFrom(cell);
                return;
            }
        }
    }

    void GiveBackHeldCellsFrom(RoadCell firstCellToGiveBack)
    {
        int position = heldCells.IndexOf(firstCellToGiveBack);
        for (int i = heldCells.Count - 1; i >= position; i--)
        {
            heldCells[i].Release(this);
            heldCells.RemoveAt(i);
        }
    }

    /// <summary>Another car holding one of these cells or physically on it, or null.</summary>
    Vehicle OtherCarIn(List<RoadCell> cells)
    {
        foreach (RoadCell cell in cells)
        {
            if (cell.Holder != null && cell.Holder != this) return cell.Holder;
            Vehicle carOnCell = OtherCarPhysicallyOn(cell);
            if (carOnCell != null) return carOnCell;
        }
        return null;
    }

    Vehicle OtherCarPhysicallyOn(RoadCell cell)
    {
        foreach (Vehicle car in cell.CarsOnIt)
            if (car != this) return car;
        return null;
    }

    /// <summary>
    /// A rebel may drive into cells we have reserved. If we see one there we brake for it
    /// (and crash if we can't stop in time).
    /// </summary>
    Obstacle RebelInsideTheCellsWeHold()
    {
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
        {
            RoadCell cell = Path.Cells[i];
            if (cell.Holder != this) break;

            Vehicle intruder = OtherCarPhysicallyOn(cell);
            if (intruder != null)
                return ObstacleFor(intruder, cell, Path.LinkAt(i - 1), Math.Max(0f, Path.DistanceToEntryOf(i)));
        }
        return Obstacle.None;
    }

    /// <summary>
    /// Right of way at junctions (see TrafficRules): another car that goes before us at one of the junctions we are
    /// about to enter, and that will get there soon, or null.
    /// </summary>
    Vehicle CarWithRightOfWay(int firstIndex, int lastIndex, World world)
    {
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            RoadCell junction = Path.Cells[i];
            if (!junction.IsJunction) continue;

            CellLink ourEntrance = Path.LinkAt(i - 1);
            int ourRightOfWay = TrafficRules.RightOfWay(ourEntrance);
            foreach (Vehicle other in world.Vehicles)
            {
                if (other == this || !other.WillEnterSoon(junction, out CellLink theirEntrance)) continue;
                if (theirEntrance == ourEntrance) continue;   // same street as us: normal following

                int theirRightOfWay = TrafficRules.RightOfWay(theirEntrance);
                bool theyGoFirst = theirRightOfWay > ourRightOfWay
                                   || (theirRightOfWay == ourRightOfWay && other.WaitingSince < WaitingSince);
                if (theyGoFirst) return other;
            }
        }
        return null;
    }

    /// <summary>
    /// True when this car will drive into 'junction' within a few seconds (or is waiting right in front of it) and
    /// nothing will stop it: a law-abiding car with a red light, or with the junction blocked, is not coming.
    /// </summary>
    bool WillEnterSoon(RoadCell junction, out CellLink entrance)
    {
        entrance = null;
        if (IsCrashed) return false;

        int index = -1;
        for (int i = Path.FrontIndex + 1; i <= Path.LastIndex; i++)
        {
            if (Path.Cells[i] == junction) { index = i; break; }
        }
        if (index < 0) return false;

        float distance = Path.DistanceToEntryOf(index);
        float closeEnough = Math.Max(Path.LinkAt(index - 1).Length, Speed * TrafficRules.SecondsToLookForTrafficWithRightOfWay);
        if (distance > closeEnough) return false;

        entrance = Path.LinkAt(index - 1);
        if (!FollowsTrafficRules) return true;   // rebels come whatever happens

        for (int i = Path.FrontIndex + 1; i <= index; i++)
        {
            TrafficLight light = Path.Cells[i - 1].TrafficLight;
            if (TrafficRules.MustStopForLight(light, BrakingDistance(Speed), Path.DistanceToEntryOf(i))) return false;
        }

        bool moving = Speed > 0.5f;
        return moving || OtherCarIn(CellsToTakeTogether(index, out _)) == null;
    }

    static bool ContainsJunction(List<RoadCell> cells)
    {
        foreach (RoadCell cell in cells)
            if (cell.IsJunction) return true;
        return false;
    }

    // ------------------------------------------------------------------
    // Rebels
    // ------------------------------------------------------------------

    /// <summary>
    /// Rebels ignore traffic lights, speed limits and right of way, and don't wait for cells other cars have reserved.
    /// They reserve the free cells they need, like everybody, and they brake for cars they can see in front of them.
    /// </summary>
    Obstacle LookAheadIgnoringTheRules(World world)
    {
        float lookAhead = BrakingDistance(Speed) + world.Settings.MetersPerCell;
        for (int index = Path.FrontIndex + 1; index <= Path.LastIndex; index++)
        {
            float distanceToCell = Path.DistanceToEntryOf(index);
            if (distanceToCell > lookAhead) return Obstacle.None;

            CellLink linkIntoCell = Path.LinkAt(index - 1);
            var cellsOfThisStep = new List<RoadCell> { Path.Cells[index] };
            if (linkIntoCell.IsLaneChange)
            {
                cellsOfThisStep.Add(linkIntoCell.CellBeside);
                cellsOfThisStep.Add(linkIntoCell.CellAhead);
            }

            foreach (RoadCell cell in cellsOfThisStep)
            {
                Vehicle carOnCell = OtherCarPhysicallyOn(cell);
                if (carOnCell != null) return ObstacleFor(carOnCell, cell, linkIntoCell, distanceToCell);
            }
            foreach (RoadCell cell in cellsOfThisStep)
                if (cell.IsFree) Take(cell);
        }
        return Obstacle.StopAt(Path.RemainingLength, ObstacleReason.EndOfRoute);
    }

    // ------------------------------------------------------------------
    // Shared by both
    // ------------------------------------------------------------------

    /// <summary>
    /// A car ahead of us in the very next cell and driving our way is followed at its speed.
    /// Anything else (cross traffic, a busy junction, a blocked lane change) is treated like a wall.
    /// </summary>
    static Obstacle ObstacleFor(Vehicle blocker, RoadCell blockedCell, CellLink linkIntoCell, float distanceToCell)
    {
        bool followingCarAhead = !linkIntoCell.IsLaneChange
                                 && (blockedCell.Holder == blocker || blockedCell.CarsOnIt.Contains(blocker))
                                 && blocker.Heading == linkIntoCell.Heading;
        if (followingCarAhead)
            return new Obstacle(distanceToCell, blocker.Speed, ObstacleReason.CarAhead);

        ObstacleReason reason = linkIntoCell.IsLaneChange ? ObstacleReason.BusyLane : ObstacleReason.BusyJunction;
        return Obstacle.StopAt(distanceToCell, reason);
    }

    static Obstacle Nearest(Obstacle first, Obstacle second)
    {
        return second.Distance < first.Distance ? second : first;
    }
}
