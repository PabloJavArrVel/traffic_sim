using System.Collections.Generic;

public class AutonomousTaxi : VehicleAgent
{
    private Queue<TrafficNode> path = new();

    public override void Perceive(World world)
    {
        // check traffic lights
        // check node occupancy
    }

    public override void Deliberate(World world)
    {
        //
    }

    public override void Act(World world)
    {
        //
    }
}