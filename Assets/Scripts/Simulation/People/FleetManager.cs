using System.Collections.Generic;

/// <summary>
/// The taxi dispatcher. Pedestrians ask it for a ride and it sends the closest free taxi
/// (closest by driving distance, not in a straight line). Requests are served first come, first served.
/// </summary>
public class FleetManager : Agent
{
    readonly List<Taxi> taxis = new List<Taxi>();
    readonly List<Pedestrian> queue = new List<Pedestrian>();

    public IReadOnlyList<Taxi> Taxis => taxis;

    /// <summary>People waiting for a taxi to be assigned, in the order they asked.</summary>
    public IReadOnlyList<Pedestrian> Queue => queue;

    public void AddTaxi(Taxi taxi) => taxis.Add(taxi);

    public void RequestRide(Pedestrian pedestrian)
    {
        if (!queue.Contains(pedestrian)) queue.Add(pedestrian);
    }

    public override void Deliberate(World world)
    {
        queue.RemoveAll(pedestrian => pedestrian.State != PedestrianState.WaitingForTaxi);

        while (queue.Count > 0)
        {
            Pedestrian next = queue[0];
            Taxi taxi = ClosestFreeTaxi(next.PickupCell, world);
            if (taxi == null) return;   // every taxi is busy: the queue keeps its order

            taxi.Assign(next, world);
            next.TaxiAssigned(world.Time);
            queue.RemoveAt(0);
        }
    }

    Taxi ClosestFreeTaxi(RoadCell pickupCell, World world)
    {
        Taxi closest = null;
        float shortestDistance = float.PositiveInfinity;
        foreach (Taxi taxi in taxis)
        {
            if (!taxi.IsAvailable) continue;

            float distance = taxi.DrivingDistanceTo(pickupCell, world);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = taxi;
            }
        }
        return closest;
    }
}
