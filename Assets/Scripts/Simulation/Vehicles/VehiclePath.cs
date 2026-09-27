using System;
using System.Collections.Generic;

/// <summary>A point on a vehicle's path: on the link that starts at Cells[Index], Distance meters after that cell's center.</summary>
public readonly struct PathPoint
{
    public readonly int Index;
    public readonly float Distance;

    public PathPoint(int index, float distance)
    {
        Index = index;
        Distance = distance;
    }
}

/// <summary>
/// The cells a vehicle drives through, in order, and where its front bumper is on them.
///
/// Cars drive from the center of one cell to the center of the next along a CellLink.
/// The front bumper is on the link from Cells[FrontIndex] to Cells[FrontIndex + 1],
/// FrontDistance meters after the center of Cells[FrontIndex].
/// The path also keeps a couple of cells behind the car, so we always know where its rear bumper is.
/// </summary>
public class VehiclePath
{
    /// <summary>Cells kept behind the front: enough to find the rear bumper and to draw the car around corners.</summary>
    const int CellsKeptBehindFront = 2;

    readonly List<RoadCell> cells = new List<RoadCell>();

    /// <summary>A path whose front bumper is at the center of 'startCell'; the car's rear is towards 'cellBehind'.</summary>
    public VehiclePath(RoadCell cellBehind, RoadCell startCell)
    {
        cells.Add(cellBehind);
        cells.Add(startCell);
        FrontIndex = 0;
        FrontDistance = LinkAt(0).Length;
    }

    public IReadOnlyList<RoadCell> Cells => cells;
    public int FrontIndex { get; private set; }
    public float FrontDistance { get; private set; }

    public int LastIndex => cells.Count - 1;
    public RoadCell LastCell => cells[LastIndex];

    /// <summary>The link from Cells[index] to Cells[index + 1].</summary>
    public CellLink LinkAt(int index) => cells[index].LinkTo(cells[index + 1]);

    /// <summary>The link the front bumper is on.</summary>
    public CellLink FrontLink => LinkAt(FrontIndex);

    /// <summary>Meters from the front bumper to the center of Cells[index] (index must not be behind the front).</summary>
    public float DistanceToCenterOf(int index)
    {
        float distance = -FrontDistance;   // the center of Cells[FrontIndex] is FrontDistance meters behind us
        for (int i = FrontIndex; i < index; i++)
            distance += LinkAt(i).Length;
        return distance;
    }

    /// <summary>
    /// Meters from the front bumper to the point where it enters Cells[index].
    /// Two cells meet halfway between their centers. A lane change needs all its cells before it starts,
    /// so for a lane change the entry point is where the lane change begins.
    /// </summary>
    public float DistanceToEntryOf(int index)
    {
        CellLink linkIntoCell = LinkAt(index - 1);
        float entryPointOnLink = linkIntoCell.IsLaneChange ? 0f : linkIntoCell.Length / 2f;
        return DistanceToCenterOf(index - 1) + entryPointOnLink;
    }

    /// <summary>Meters from the front bumper to the center of the last cell of the path.</summary>
    public float RemainingLength => DistanceToCenterOf(LastIndex);

    public void AdvanceFront(float meters)
    {
        FrontDistance += meters;

        // Move on to the next link while the front has gone past the end of the current one.
        while (FrontIndex < LastIndex - 1 && FrontDistance >= FrontLink.Length)
        {
            FrontDistance -= FrontLink.Length;
            FrontIndex++;
        }

        // The front never goes past the center of the last cell.
        if (FrontDistance > FrontLink.Length)
            FrontDistance = FrontLink.Length;
    }

    /// <summary>Continues the path with a route that starts at the current last cell.</summary>
    public void Extend(List<RoadCell> route)
    {
        for (int i = 1; i < route.Count; i++)
            cells.Add(route[i]);
    }

    /// <summary>Keeps the path up to Cells[index] and continues with a route that starts at Cells[index].</summary>
    public void ReplaceAfter(int index, List<RoadCell> route)
    {
        cells.RemoveRange(index + 1, cells.Count - index - 1);
        Extend(route);
    }

    /// <summary>Forgets cells far behind the car so the list doesn't grow forever.</summary>
    public void ForgetCellsFarBehind()
    {
        while (FrontIndex > CellsKeptBehindFront)
        {
            cells.RemoveAt(0);
            FrontIndex--;
        }
    }

    /// <summary>The point that is 'meters' behind the front bumper (for example the middle of the car).</summary>
    public PathPoint PointBehindFront(float meters)
    {
        int index = FrontIndex;
        float distance = FrontDistance - meters;
        while (distance < 0f && index > 0)
        {
            index--;
            distance += LinkAt(index).Length;
        }
        return new PathPoint(index, Math.Max(0f, distance));
    }

    /// <summary>
    /// The cells under a car of the given length. The first half of a link belongs to the cell it starts in and
    /// the second half to the cell it ends in; a lane change covers the whole 2x2 block of cells it sweeps through.
    /// </summary>
    public List<RoadCell> CellsUnderCar(float carLength)
    {
        var cellsUnderCar = new List<RoadCell>();
        int index = FrontIndex;
        float carEndsAt = FrontDistance;   // on this link, the car covers [carStartsAt, carEndsAt]
        float lengthLeft = carLength;      // how much of the car we still have to place, going backwards

        while (true)
        {
            CellLink link = LinkAt(index);
            float carStartsAt = Math.Max(0f, carEndsAt - lengthLeft);
            AddCellsCoveredOnLink(link, carStartsAt, carEndsAt, cellsUnderCar);

            lengthLeft -= carEndsAt - carStartsAt;
            if (lengthLeft <= 0.001f || index == 0) break;

            index--;
            carEndsAt = LinkAt(index).Length;
        }

        return cellsUnderCar;
    }

    static void AddCellsCoveredOnLink(CellLink link, float from, float to, List<RoadCell> result)
    {
        if (link.IsLaneChange)
        {
            AddOnce(result, link.From);
            AddOnce(result, link.CellBeside);
            AddOnce(result, link.CellAhead);
            AddOnce(result, link.To);
            return;
        }

        float halfway = link.Length / 2f;
        if (from < halfway) AddOnce(result, link.From);
        if (to > halfway) AddOnce(result, link.To);
    }

    static void AddOnce(List<RoadCell> list, RoadCell cell)
    {
        if (!list.Contains(cell)) list.Add(cell);
    }
}
