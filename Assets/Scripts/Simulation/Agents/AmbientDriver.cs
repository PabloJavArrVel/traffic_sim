using System;

public class AmbientDriver : VehicleAgent
{
    Profile profile;

    // --- Perceived state ---
    VehicleAgent vehicleAhead;
    float        gap;
    bool         redLightAhead;

    bool  mustMerge;
    public bool MustMerge = false;
    float mergeUrgency;
    bool  isMergeTarget;

    float desiredSpeed;
    float acceleration = 5f;
    float laneChangeCooldown = 0f;

    public AmbientDriver()
    {
        profile = Profile.RandomProfile();
    }

    public override void Perceive(World world)
    {
        PerceiveVehicleAhead();
        PerceiveLanesAhead();
        PerceiveAdjacentMergers();
        PerceiveTrafficLight();
    }

    public override void Deliberate(World world)
    {
        laneChangeCooldown -= world.DeltaTime;
        ConsiderLaneChange();
        ChooseSpeed(world);
    }

    public override void Act(World world)
    {
        Move(world);
    }

    void PerceiveVehicleAhead()
    {
        vehicleAhead = CurrentLane.GetVehicleAhead(this);

        if (vehicleAhead != null && vehicleAhead.CurrentLane != CurrentLane)
            vehicleAhead = null;

        gap = vehicleAhead != null
            ? Math.Max(0f, vehicleAhead.Position - Position - vehicleAhead.Length - Length)
            : float.MaxValue;
    }

    void PerceiveLanesAhead()
    {
        mustMerge    = false;
        mergeUrgency = 0f;

        var toNode = CurrentLane.Edge.to;
        if (toNode.Outgoing.Count == 0) return;

        var nextEdge = toNode.Outgoing[0];

        if (LaneNumber >= nextEdge.Lanes.Count)
        {
            mustMerge = true;

            float distToEnd  = CurrentLane.Edge.Length - Position;
            float noticeZone = CurrentLane.Edge.Length * 0.5f;
            mergeUrgency     = 1f - Math.Min(1f, distToEnd / noticeZone);
        }

        MustMerge = mustMerge;
    }

    void PerceiveAdjacentMergers()
    {
        isMergeTarget = false;

        if (profile.Kindness < 0.3f) return;

        var edge = CurrentLane.Edge;

        int[] adjacent = { LaneNumber - 1, LaneNumber + 1 };

        foreach (int n in adjacent)
        {
            if (n < 0 || n >= edge.Lanes.Count) continue;

            foreach (var v in edge.Lanes[n].Vehicles)
            {
                if (!(v is AmbientDriver other)) continue;
                if (!other.mustMerge) continue;

                float dist = Math.Abs(v.Position - Position);
                if (dist < Length * 3f)
                {
                    isMergeTarget = true;
                    return;
                }
            }
        }
    }

    void PerceiveTrafficLight()
    {
        var nextNode = CurrentLane.Edge.to;
        redLightAhead = false;

        if (nextNode.Light != null)
        {
            float distanceToEnd = CurrentLane.Edge.Length - Position;

            if (nextNode.Light.CurrentState == TrafficLight.State.Red &&
                distanceToEnd < 20f)
            {
                redLightAhead = true;
            }
        }
    }

    void ConsiderLaneChange()
    {
        var edge  = CurrentLane.Edge;
        int lanes = edge.Lanes.Count;
        if (lanes < 2) return;

        bool shouldAttempt = false;

        if (mustMerge)
        {
            float threshold = 1f - profile.Kindness * 0.7f;
            shouldAttempt   = mergeUrgency >= threshold;
        }
        else
        {
            if (laneChangeCooldown > 0f) return;
            if (gap > profile.MinFollowingDistance * 2f) return;

            float roll = (float)rng.NextDouble();
            shouldAttempt = roll < profile.LaneChangeAggression;
        }

        if (!shouldAttempt) return;

        int[] candidates = mustMerge
            ? new[] { LaneNumber - 1 }
            : new[] { LaneNumber - 1, LaneNumber + 1 };

        foreach (int target in candidates)
        {
            if (target < 0 || target >= lanes) continue;

            Lane targetLane = edge.Lanes[target];
            if (!IsLaneSafeToEnter(targetLane, mustMerge ? mergeUrgency : 0f)) continue;

            CurrentLane.RemoveVehicle(this);

            LaneNumber  = target;
            LaneIndex   = targetLane.Vehicles.Count;
            CurrentLane = targetLane;
            targetLane.Vehicles.Add(this);

            laneChangeCooldown = mustMerge
                ? 1f
                : 3f * (1f - profile.LaneChangeAggression) + 1f;

            break;
        }
    }

    bool IsLaneSafeToEnter(Lane target, float urgency = 0f)
    {
        float margin = profile.MinFollowingDistance * (1f - urgency * 0.7f);

        foreach (var v in target.Vehicles)
        {
            float relativePos = v.Position - Position;

            if (relativePos > 0)
            {
                // vehicle ahead in target lane — need room to not hit it
                if (relativePos < Length + v.Length + margin)
                    return false;
            }
            else
            {
                // vehicle behind in target lane — need room so they don't hit us
                // add buffer based on how much faster they are
                float closingSpeed   = Math.Max(0f, v.Speed - Speed);
                float reactionMargin = margin + closingSpeed * 1.5f;
                if (Math.Abs(relativePos) < Length + v.Length + reactionMargin)
                    return false;
            }
        }

        return true;
    }

    void ChooseSpeed(World world)
    {
        desiredSpeed = CurrentLane.Edge.SpeedLimit * (1000f / 3600f) * profile.DesiredSpeedFactor;

        if (gap < profile.MinFollowingDistance)
        {
            float followFactor = Math.Max(0f, gap / profile.MinFollowingDistance);
            desiredSpeed = Math.Min(desiredSpeed, vehicleAhead.Speed * followFactor);
        }

        if (isMergeTarget)
            desiredSpeed *= 1f - (profile.Kindness * 0.25f);

        if (redLightAhead)
        {
            float distanceToEnd = CurrentLane.Edge.Length - Position;
            float brakingSpeed  = (float)Math.Sqrt(2f * 4f * Math.Max(0f, distanceToEnd));
            desiredSpeed        = Math.Min(desiredSpeed, brakingSpeed);
        }

        if (mustMerge && mergeUrgency > 0.8f)
            desiredSpeed *= 1f - (mergeUrgency - 0.8f) * 2f;

        Speed = MoveTowards(Speed, desiredSpeed, acceleration * world.DeltaTime);
    }

    float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta) return target;
        return current + Math.Sign(target - current) * maxDelta;
    }
}