using UnityEngine;

public class VehicleView : MonoBehaviour
{
    public VehicleAgent Agent;
    public WorldView WorldView;

    // For timing test — log once when vehicle crosses each node boundary
    float startTime = -1f;
    int   lastEdgeFrom = -1;

    void Update()
    {
        // Start timer when first vehicle begins moving
        if (startTime < 0f && Agent.Speed > 0.01f)
        {
            startTime = Time.time;
            lastEdgeFrom = Agent.CurrentLane.Edge.from.id;
            Debug.Log($"[Timing] Vehicle departed node {lastEdgeFrom} at t=0");
        }

        // Detect edge transition
        int currentFrom = Agent.CurrentLane.Edge.from.id;
        if (startTime >= 0f && currentFrom != lastEdgeFrom)
        {
            float elapsed = Time.time - startTime;
            Debug.Log($"[Timing] Vehicle reached node {Agent.CurrentLane.Edge.from.id} at t={elapsed:F2}s (expected 5.4s per edge)");
            lastEdgeFrom = currentFrom;
        }

        // Position
        var laneView = WorldView.GetLaneView(Agent.CurrentLane);
        float t = Agent.Position / Agent.CurrentLane.Edge.Length;

        Vector3 newPos = Vector3.Lerp(laneView.Start, laneView.End, t);
        transform.position = newPos;

        // Orientation
        Vector3 dir = laneView.End - laneView.Start;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}