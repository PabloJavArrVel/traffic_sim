using UnityEngine;

/// <summary>
/// Keeps a car's GameObject where the simulation says the car is.
///
/// - The car model's pivot is its center, so we place it half a car length behind the front bumper.
/// - Corners and lane changes are drawn as smooth curves (see PathGeometry).
/// - The simulation moves in steps of 0.05 s; between two steps we blend the two poses so movement looks smooth.
/// - Rebels (drivers who ignore the traffic rules) are tinted red; crashed cars are darkened until they are towed.
/// </summary>
public class VehicleView : MonoBehaviour
{
    static readonly Color RebelTint = new Color(1f, 0.35f, 0.35f);
    static readonly Color CrashedTint = new Color(0.25f, 0.25f, 0.25f);

    Vehicle vehicle;
    SimulationManager simulation;
    Renderer[] renderers;
    MaterialPropertyBlock tint;

    Vector3 previousPosition;
    Quaternion previousRotation;
    Vector3 currentPosition;
    Quaternion currentRotation;
    float simulationTimeOfCurrentPose = -1f;
    int timesTowedWhenLastDrawn;
    bool wasCrashed;

    public Vehicle Vehicle => vehicle;

    public void Show(Vehicle vehicle, SimulationManager simulation)
    {
        this.vehicle = vehicle;
        this.simulation = simulation;
        renderers = GetComponentsInChildren<Renderer>();
        tint = new MaterialPropertyBlock();

        SnapToSimulation();
        ShowTint();
    }

    void LateUpdate()
    {
        if (vehicle == null) return;

        // Towed after a crash: jump to the new place instead of gliding across the city.
        if (vehicle.TimesTowed != timesTowedWhenLastDrawn) SnapToSimulation();

        bool simulationMovedOn = simulation.World.Time != simulationTimeOfCurrentPose;
        if (simulationMovedOn)
        {
            previousPosition = currentPosition;
            previousRotation = currentRotation;
            ReadPoseFromSimulation();
        }

        float blend = Mathf.Clamp01(simulation.StepProgress);
        transform.SetPositionAndRotation(
            Vector3.Lerp(previousPosition, currentPosition, blend),
            Quaternion.Slerp(previousRotation, currentRotation, blend));

        if (vehicle.IsCrashed != wasCrashed) ShowTint();
    }

    void SnapToSimulation()
    {
        ReadPoseFromSimulation();
        previousPosition = currentPosition;
        previousRotation = currentRotation;
        transform.SetPositionAndRotation(currentPosition, currentRotation);
        timesTowedWhenLastDrawn = vehicle.TimesTowed;
    }

    void ReadPoseFromSimulation()
    {
        MapPlacement placement = simulation.Placement;
        PathPoint middleOfCar = vehicle.Path.PointBehindFront(vehicle.Length / 2f);
        MapPose pose = PathGeometry.PoseAt(vehicle.Path, middleOfCar, cornerRadius: placement.MetersPerCell / 2f);

        currentPosition = placement.ToWorld(pose.Position);
        currentRotation = Quaternion.LookRotation(placement.ToWorldDirection(pose.Forward), Vector3.up);
        simulationTimeOfCurrentPose = simulation.World.Time;
    }

    /// <summary>Tints the car's materials without changing the shared material assets.</summary>
    void ShowTint()
    {
        wasCrashed = vehicle.IsCrashed;
        Color color = Color.white;
        if (!vehicle.FollowsTrafficRules) color = RebelTint;
        if (vehicle.IsCrashed) color = CrashedTint;

        foreach (Renderer part in renderers)
        {
            part.GetPropertyBlock(tint);
            tint.SetColor("_BaseColor", color);   // URP materials
            tint.SetColor("_Color", color);       // built-in materials
            part.SetPropertyBlock(tint);
        }
    }
}
