using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    [Header("Params")]
    public float trafficDensity = 0.5f;

    public World world;
    public WorldView worldView;
    public CityRenderer cityRenderer;

    // 3 nodes in a loop, 10 tiles each, 3 lanes
    // Each node has a traffic light, staggered so they're never all the same state
    //
    // Node 0: starts Green
    // Node 1: starts Red
    // Node 2: starts Yellow

    (int fromNode, int toNode, int startCol, int endCol, int row)[] edgeDefinitions =
    {
        (0, 1,  0,  9, 0),
        (1, 2,  9, 19, 0),
        (2, 0, 19,  0, 2),
    };

    void Start()
    {
        NavigationGraph graph = CreateTrafficGraph();
        world = new World(graph);
        CreateLaneViews(graph);
        PopulateTraffic(world);
        worldView.SpawnVehicles(world.Agents);

        Debug.Log($"Spawned {world.Agents.Count} agents");
    }

    void Update()
    {
        world.Tick(Time.deltaTime);
    }

    NavigationGraph CreateTrafficGraph()
    {
        NavigationGraph graph = new NavigationGraph();

        graph.AddNode(new TrafficNode(0));
        graph.AddNode(new TrafficNode(1));
        graph.AddNode(new TrafficNode(2));

        // Stagger light phases so intersection doesn't all go red at once
        graph.nodes[0].Light = new TrafficLight { CurrentState = TrafficLight.State.Green };
        graph.nodes[1].Light = new TrafficLight { CurrentState = TrafficLight.State.Red };
        graph.nodes[2].Light = new TrafficLight { CurrentState = TrafficLight.State.Red };

        float ts = cityRenderer.tileSize;

        foreach (var (fromNode, toNode, startCol, endCol, row) in edgeDefinitions)
        {
            int   tileCount = Mathf.Abs(endCol - startCol) + 1;
            float length    = tileCount * ts;
            graph.AddEdge(fromNode, toNode, 3, length, RoadClass.Primary);
        }

        return graph;
    }

    void CreateLaneViews(NavigationGraph graph)
    {
        float ts = cityRenderer.tileSize;

        int defIndex = 0;
        foreach (var node in graph.nodes.Values)
        {
            foreach (var edge in node.Outgoing)
            {
                var (_, _, startCol, endCol, row) = edgeDefinitions[defIndex++];

                Vector3 start = new Vector3(startCol * ts, 0f, -row * ts);
                Vector3 end   = new Vector3(endCol   * ts, 0f, -row * ts);

                for (int i = 0; i < edge.Lanes.Count; i++)
                {
                    float   offset = (i - (edge.Lanes.Count - 1) / 2f) * 1.5f;
                    Vector3 off    = new Vector3(0, 0, offset);
                    worldView.CreateLaneView(edge.Lanes[i], start + off, end + off);
                }
            }
        }
    }

    void PopulateTraffic(World world)
    {
        foreach (var node in world.Navigation.nodes.Values)
            foreach (var edge in node.Outgoing)
                for (int laneNum = 0; laneNum < edge.Lanes.Count; laneNum++)
                    PopulateLane(world, edge.Lanes[laneNum], laneNum);
    }

    void PopulateLane(World world, Lane lane, int laneNumber)
    {
        if (lane.Edge.from.id != 0) return;

        float spacing = cityRenderer.tileSize;
        int   count   = Mathf.FloorToInt(10 * trafficDensity);

        for (int i = 0; i < count; i++)
        {
            AmbientDriver car = new AmbientDriver();
            car.CurrentLane = lane;
            car.LaneNumber  = laneNumber;
            car.Position    = i * spacing;
            car.LaneIndex   = lane.Vehicles.Count;

            lane.Vehicles.Add(car);
            world.Agents.Add(car);
        }
    }
}