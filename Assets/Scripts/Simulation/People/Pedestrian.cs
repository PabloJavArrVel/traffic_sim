public enum PedestrianState
{
    WaitingForTaxi,
    TaxiOnTheWay,
    Riding,
    Arrived,
    GaveUp
}

/// <summary>
/// A person on the sidewalk who wants a taxi ride from one street cell to another.
/// They ask the FleetManager for a taxi; if none is assigned before their patience runs out, they give up.
/// Once a taxi is on its way they wait for it.
/// </summary>
public class Pedestrian : Agent
{
    bool askedForTaxi;

    public Pedestrian(int id, RoadCell pickupCell, RoadCell destinationCell, float patienceSeconds, float requestedAt)
    {
        Id = id;
        PickupCell = pickupCell;
        DestinationCell = destinationCell;
        SecondsLeftBeforeGivingUp = patienceSeconds;
        RequestedAt = requestedAt;
    }

    public int Id { get; }

    /// <summary>The street cell next to the sidewalk where the person waits.</summary>
    public RoadCell PickupCell { get; }

    public RoadCell DestinationCell { get; }

    public PedestrianState State { get; private set; } = PedestrianState.WaitingForTaxi;

    public float SecondsLeftBeforeGivingUp { get; private set; }

    public bool IsFinished => State == PedestrianState.Arrived || State == PedestrianState.GaveUp;

    // Simulation times of each step of the ride (NaN until it happens). Used by SimulationMetrics.
    public float RequestedAt { get; }
    public float TaxiAssignedAt { get; private set; } = float.NaN;
    public float PickedUpAt { get; private set; } = float.NaN;
    public float FinishedAt { get; private set; } = float.NaN;

    public override void Deliberate(World world)
    {
        if (!askedForTaxi)
        {
            world.FleetManager.RequestRide(this);
            askedForTaxi = true;
        }

        if (State != PedestrianState.WaitingForTaxi) return;

        SecondsLeftBeforeGivingUp -= world.DeltaTime;
        if (SecondsLeftBeforeGivingUp <= 0f)
        {
            State = PedestrianState.GaveUp;
            FinishedAt = world.Time;
            world.Statistics.RidesGivenUp++;
            world.Metrics.RecordRide(this);
        }
    }

    public void TaxiAssigned(float now)
    {
        State = PedestrianState.TaxiOnTheWay;
        TaxiAssignedAt = now;
    }

    public void GetIn(float now)
    {
        State = PedestrianState.Riding;
        PickedUpAt = now;
    }

    public void GetOut(float now)
    {
        State = PedestrianState.Arrived;
        FinishedAt = now;
    }
}
