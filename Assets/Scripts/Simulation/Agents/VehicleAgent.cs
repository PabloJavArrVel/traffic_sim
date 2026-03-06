using System;

public abstract class VehicleAgent : Agent
{
    public Random rng = new Random();

    public Lane CurrentLane;
    public int LaneNumber; // which lane (0,1,2) — persists across edges
    public int LaneIndex;  // position in lane.Vehicles list — changes constantly
    public float Position;
    public float Speed;
    public float Length = 4.5f;

    public override void Act(World world)
    {
        Move(world);
    }

    public void Move(World world)
    {
        Position += Speed * world.DeltaTime;

        MaintainLaneOrder();

        if (Position >= CurrentLane.Edge.Length)
        {
            MoveToNextEdge();
        }
    }

    public void MaintainLaneOrder()
    {
        int i = LaneIndex;

        while (i < CurrentLane.Vehicles.Count - 1 &&
               Position > CurrentLane.Vehicles[i + 1].Position)
        {
            var other = CurrentLane.Vehicles[i + 1];

            CurrentLane.Vehicles[i + 1] = this;
            CurrentLane.Vehicles[i] = other;

            LaneIndex++;
            other.LaneIndex--;

            i++;
        }
    }

    public void MoveToNextEdge()
    {
        var node = CurrentLane.Edge.to;

        if (node.Outgoing.Count == 0)
        {
            Speed = 0;
            return;
        }

        var nextEdge = node.Outgoing[rng.Next(node.Outgoing.Count)];
        // In VehicleAgent.MoveToNextEdge
        var nextLane = nextEdge.Lanes[Math.Min(LaneNumber, nextEdge.Lanes.Count - 1)];

        bool forcedMerge = LaneNumber >= nextEdge.Lanes.Count;

        if (!forcedMerge && !nextLane.IsFree(0, Length))
        {
            Position = CurrentLane.Edge.Length - 0.1f;
            return;
        }

        CurrentLane.RemoveVehicle(this);

        CurrentLane = nextLane;
        Position = 0;
        LaneIndex = nextLane.Vehicles.Count;
        nextLane.Vehicles.Add(this);
    }
}