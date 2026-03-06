using UnityEngine;
using System.Collections.Generic;

public class WorldView : MonoBehaviour
{
    public GameObject vehiclePrefab; 
    public World world;
    Dictionary<Lane, LaneView> laneViews = new();

    public void CreateLaneView(Lane lane, Vector3 start, Vector3 end)
    {
        LaneView view = new LaneView
        {
            Lane = lane,
            Start = start,
            End = end
        };

        laneViews[lane] = view;
    }

    public LaneView GetLaneView(Lane lane)
    {
        return laneViews[lane];
    }

    public void SpawnVehicles(List<Agent> agents)
    {
        foreach (var agent in agents)
        {
            if (agent is VehicleAgent vehicle)
            {
                GameObject go = Instantiate(vehiclePrefab);
                VehicleView view = go.AddComponent<VehicleView>();
                view.Agent = vehicle;
                view.WorldView = this;
            }
        }
    }
}