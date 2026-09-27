public enum TaxiState
{
    Cruising,       // free, driving around waiting for a job
    GoingToPickup,  // driving to a passenger
    Boarding,       // stopped while the passenger gets in
    Carrying,       // driving the passenger to their destination
    DroppingOff     // stopped while the passenger gets out
}

/// <summary>
/// An autonomous taxi. It cruises around until the FleetManager assigns it a passenger, drives to them, waits
/// while they get in, drives them to their destination and lets them out. Then it is free again.
/// </summary>
public class Taxi : Vehicle
{
    float secondsAtCurb;

    public Taxi(int id, RoadCell cellBehind, RoadCell startCell, float length)
        : base(id, cellBehind, startCell, DriverProfile.AutonomousTaxi(), length)
    {
    }

    public TaxiState State { get; private set; } = TaxiState.Cruising;

    /// <summary>The passenger the taxi is going to pick up or is carrying, or null.</summary>
    public Pedestrian Passenger { get; private set; }

    public bool IsAvailable => State == TaxiState.Cruising;

    public int RidesCompleted { get; private set; }

    /// <summary>Meters left to the pickup or to the destination.</summary>
    public float DistanceToGo => Path.RemainingLength;

    public void Assign(Pedestrian passenger, World world)
    {
        Passenger = passenger;
        State = TaxiState.GoingToPickup;
        DriveTo(passenger.PickupCell, world);
    }

    protected override void PlanRoute(World world)
    {
        switch (State)
        {
            case TaxiState.Cruising:
                bool routeEndsSoon = Path.RemainingLength < world.Settings.MetersPerCell * 4f;
                if (routeEndsSoon) ContinueTo(world.PickRandomDestination(), world);
                break;

            case TaxiState.GoingToPickup:
                if (HasArrived()) StopAtCurb(TaxiState.Boarding);
                break;

            case TaxiState.Boarding:
                if (WaitedAtCurb(world))
                {
                    Passenger.GetIn();
                    State = TaxiState.Carrying;
                    DriveTo(Passenger.DestinationCell, world);
                }
                break;

            case TaxiState.Carrying:
                if (HasArrived()) StopAtCurb(TaxiState.DroppingOff);
                break;

            case TaxiState.DroppingOff:
                if (WaitedAtCurb(world))
                {
                    Passenger.GetOut();
                    Passenger = null;
                    RidesCompleted++;
                    world.Statistics.RidesCompleted++;
                    State = TaxiState.Cruising;
                }
                break;
        }
    }

    /// <summary>The route ends at the pickup or destination cell; we have arrived when we are stopped there.</summary>
    bool HasArrived() => Path.RemainingLength < 1f && Speed < 0.3f;

    void StopAtCurb(TaxiState nextState)
    {
        State = nextState;
        secondsAtCurb = 0f;
    }

    bool WaitedAtCurb(World world)
    {
        secondsAtCurb += world.DeltaTime;
        return secondsAtCurb >= world.Settings.SecondsToGetInOrOut;
    }
}
