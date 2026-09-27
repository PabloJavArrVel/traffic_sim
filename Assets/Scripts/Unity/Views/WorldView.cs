using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates and removes the GameObjects that show the simulation: cars, taxis, pedestrians and traffic lights.
/// Each GameObject gets a view component (VehicleView, PedestrianView, TrafficLightView) that keeps it in sync.
/// </summary>
public class WorldView : MonoBehaviour
{
    const float TrafficLightHeight = 0.05f;

    [Header("Prefabs")]
    public GameObject ambientCarPrefab;
    public GameObject taxiPrefab;
    public GameObject pedestrianPrefab;

    [Tooltip("Needs a TrafficLightView component with its three lamps assigned.")]
    public GameObject trafficLightPrefab;

    readonly Dictionary<Pedestrian, GameObject> pedestrianObjects = new Dictionary<Pedestrian, GameObject>();
    SimulationManager simulation;

    public void Initialize(SimulationManager simulation)
    {
        this.simulation = simulation;
    }

    public GameObject ShowVehicle(Vehicle vehicle)
    {
        GameObject prefab = vehicle is Taxi ? taxiPrefab : ambientCarPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[WorldView] No prefab assigned for {vehicle.GetType().Name}.");
            return null;
        }

        GameObject shown = Instantiate(prefab, transform);
        shown.name = $"{vehicle.GetType().Name} {vehicle.Id}";
        shown.AddComponent<VehicleView>().Show(vehicle, simulation);
        return shown;
    }

    public GameObject ShowPedestrian(Pedestrian pedestrian)
    {
        if (pedestrianPrefab == null)
        {
            Debug.LogWarning("[WorldView] No pedestrian prefab assigned.");
            return null;
        }

        GameObject shown = Instantiate(pedestrianPrefab, transform);
        shown.name = $"Pedestrian {pedestrian.Id}";
        shown.AddComponent<PedestrianView>().Show(pedestrian, simulation);
        pedestrianObjects[pedestrian] = shown;
        return shown;
    }

    public void RemovePedestrian(Pedestrian pedestrian)
    {
        if (!pedestrianObjects.TryGetValue(pedestrian, out GameObject shown)) return;
        pedestrianObjects.Remove(pedestrian);
        Destroy(shown);
    }

    public void ShowTrafficLights(RoadNetwork network)
    {
        if (trafficLightPrefab == null)
        {
            Debug.LogWarning("[WorldView] No traffic light prefab assigned: lights work but are invisible.");
            return;
        }

        foreach (TrafficLight trafficLight in network.TrafficLights)
        {
            GameObject shown = Instantiate(trafficLightPrefab, transform);
            shown.name = $"Traffic light {trafficLight.Cell.Position}";
            PlaceTrafficLight(shown.transform, trafficLight, network);

            var view = shown.GetComponent<TrafficLightView>();
            if (view != null) view.Show(trafficLight);
            else Debug.LogWarning("[WorldView] The traffic light prefab has no TrafficLightView, so it won't change color.");
        }
    }

    /// <summary>
    /// A light stands on the sidewalk at the stop line (the end of its cell). The left lane of a two-lane street
    /// gets its light on the left sidewalk, every other lane on the right one.
    /// </summary>
    void PlaceTrafficLight(Transform lightTransform, TrafficLight trafficLight, RoadNetwork network)
    {
        RoadCell cell = trafficLight.Cell;
        Direction forward = cell.TrafficDirection;
        float halfCell = network.MetersPerCell / 2f;

        RoadCell cellOnTheRight = network.CellAt(cell.Position.Step(forward.TurnRight()));
        bool anotherLaneOnTheRight = cellOnTheRight != null && cellOnTheRight.TrafficDirection == forward;
        Direction sidewalkSide = anotherLaneOnTheRight ? forward.TurnLeft() : forward.TurnRight();

        MapPoint stopLine = cell.Center + forward.ToMapVector() * halfCell;
        MapPoint onSidewalk = stopLine + sidewalkSide.ToMapVector() * (halfCell * 1.1f);

        MapPlacement placement = simulation.Placement;
        lightTransform.position = placement.ToWorld(onSidewalk, TrafficLightHeight);
        lightTransform.rotation = Quaternion.LookRotation(placement.ToWorldDirection(forward), Vector3.up);
    }
}
