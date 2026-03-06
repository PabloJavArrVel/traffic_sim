public class Pedestriant : Agent
{
    public TrafficNode CurrentNode;
    public TrafficNode Destination;

    public override void Perceive(World world)
    {
        // observe nearby taxis or wait state
    }

    public override void Deliberate(World world)
    {
        // decide to request ride
    }

    public override void Act(World world)
    {
        // send request to FleetManager
    }
}