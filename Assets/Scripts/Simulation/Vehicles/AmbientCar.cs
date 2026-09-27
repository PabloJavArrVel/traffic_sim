/// <summary>
/// Background traffic: drives to a random place in the city, then to another one, forever.
/// Most ambient drivers follow the traffic rules; rebels (followsTrafficRules = false) ignore them.
/// </summary>
public class AmbientCar : Vehicle
{
    public AmbientCar(int id, RoadCell cellBehind, RoadCell startCell, DriverProfile driver, float length, bool followsTrafficRules)
        : base(id, cellBehind, startCell, driver, length, followsTrafficRules)
    {
    }

    protected override void PlanRoute(World world)
    {
        // Always keep some route ahead: when it is about to run out, pick the next random destination.
        bool routeEndsSoon = Path.RemainingLength < world.Settings.MetersPerCell * 4f;
        if (routeEndsSoon)
            ContinueTo(world.PickRandomDestination(), world);
    }
}
