using System.Collections.Generic;


public class TrafficNode
{
    public int id;
    public List<TrafficEdge> Outgoing = new();
    public TrafficLight Light;

    public TrafficNode(int id)
    {
        this.id = id;
    }
}