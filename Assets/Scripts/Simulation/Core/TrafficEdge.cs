using System.Collections.Generic;

public class TrafficEdge
{
    public TrafficNode from;
    public TrafficNode to;

    public float Length;
    public int SpeedLimit;

    public List<Lane> Lanes = new();

    public TrafficEdge(TrafficNode from, TrafficNode to)
    {
        this.from = from;
        this.to = to;
    }
}