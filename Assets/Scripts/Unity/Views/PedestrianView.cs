using UnityEngine;

/// <summary>
/// Shows a pedestrian standing on the sidewalk next to their pickup cell, facing the street.
/// They disappear when they get into the taxi (the WorldView removes the GameObject when they finish).
/// </summary>
public class PedestrianView : MonoBehaviour
{
    /// <summary>How far from the middle of the lane the pedestrian stands, in cells (0.5 = the edge of the road).</summary>
    const float DistanceFromLaneInCells = 0.62f;

    public Pedestrian Pedestrian { get; private set; }

    public void Show(Pedestrian pedestrian, SimulationManager simulation)
    {
        Pedestrian = pedestrian;

        RoadCell cell = pedestrian.PickupCell;
        Direction sidewalkSide = cell.TrafficDirection.TurnRight();
        MapPoint standingPoint = cell.Center + sidewalkSide.ToMapVector() * (simulation.Placement.MetersPerCell * DistanceFromLaneInCells);

        MapPlacement placement = simulation.Placement;
        Vector3 towardsStreet = placement.ToWorldDirection(sidewalkSide.Opposite());
        transform.SetPositionAndRotation(placement.ToWorld(standingPoint), Quaternion.LookRotation(towardsStreet, Vector3.up));
    }

    void Update()
    {
        if (Pedestrian == null) return;

        bool onTheSidewalk = Pedestrian.State == PedestrianState.WaitingForTaxi
                             || Pedestrian.State == PedestrianState.TaxiOnTheWay;
        if (!onTheSidewalk) gameObject.SetActive(false);
    }
}
