/// <summary>Background traffic: drives to a random place in the city, then to another one, forever.</summary>
public class AmbientCar : Vehicle
{
    public AmbientCar(int id, RoadCell cellBehind, RoadCell startCell, DriverProfile driver, float length)
        : base(id, cellBehind, startCell, driver, length)
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
