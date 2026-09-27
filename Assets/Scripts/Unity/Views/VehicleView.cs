using UnityEngine;

/// <summary>
/// Keeps a car's GameObject where the simulation says the car is.
///
/// - The car model's pivot is its center, so we place it half a car length behind the front bumper.
/// - Corners and lane changes are drawn as smooth curves (see PathGeometry).
/// - The simulation moves in steps of 0.05 s; between two steps we blend the two poses so movement looks smooth.
/// </summary>
public class VehicleView : MonoBehaviour
{
    Vehicle vehicle;
    SimulationManager simulation;

    Vector3 previousPosition;
    Quaternion previousRotation;
    Vector3 currentPosition;
    Quaternion currentRotation;
    float simulationTimeOfCurrentPose = -1f;

    public Vehicle Vehicle => vehicle;

    public void Show(Vehicle vehicle, SimulationManager simulation)
    {
        this.vehicle = vehicle;
        this.simulation = simulation;

        ReadPoseFromSimulation();
        previousPosition = currentPosition;
        previousRotation = currentRotation;
        transform.SetPositionAndRotation(currentPosition, currentRotation);
    }

    void LateUpdate()
    {
        if (vehicle == null) return;

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
}
