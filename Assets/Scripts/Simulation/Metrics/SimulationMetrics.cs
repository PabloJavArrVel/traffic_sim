using System;
using System.Collections.Generic;

/// <summary>
/// Measures the simulation while it runs, for experiments. Every step it adds up what the cars are doing;
/// every 'SampleSeconds' it turns those sums into one MetricsSample (averages over that interval).
/// Rides and collisions are recorded one by one when they happen.
/// </summary>
public class SimulationMetrics
{
    /// <summary>A car slower than this (m/s) counts as stopped.</summary>
    const float StoppedSpeed = 0.5f;

    /// <summary>A stopped car counts as queuing at an intersection when one of its next cells has a light of it.</summary>
    const int CellsToLookForALight = 4;

    public float SampleSeconds = 10f;

    readonly List<MetricsSample> samples = new List<MetricsSample>();
    readonly List<RideRecord> rides = new List<RideRecord>();
    readonly List<CollisionRecord> collisions = new List<CollisionRecord>();
    Dictionary<TrafficLight, int> intersectionOfLight;

    // Sums for the interval being measured.
    int steps;
    int vehicleSteps;
    float speedSum;
    int stopped, stoppedAtRedLight, stoppedBehindCar, stoppedAtJunction;
    int taxiSteps, busyTaxiSteps;
    int speedingSteps;
    int redLightsRun, collisionsInInterval;
    float metersDriven;
    float[] queuedCarSteps;

    public IReadOnlyList<MetricsSample> Samples => samples;
    public IReadOnlyList<RideRecord> Rides => rides;
    public IReadOnlyList<CollisionRecord> Collisions => collisions;

    public void RecordStep(World world)
    {
        if (intersectionOfLight == null) NumberIntersections(world.Network);

        steps++;
        foreach (Vehicle vehicle in world.Vehicles)
        {
            vehicleSteps++;
            speedSum += vehicle.Speed;
            metersDriven += vehicle.Speed * world.DeltaTime;

            if (vehicle is Taxi taxi)
            {
                taxiSteps++;
                if (taxi.State != TaxiState.Cruising) busyTaxiSteps++;
            }

            // A little margin so rounding never counts as speeding.
            if (vehicle.Speed > vehicle.CellAtFront.SpeedLimit * 1.02f) speedingSteps++;

            if (vehicle.Speed >= StoppedSpeed) continue;
            stopped++;
            switch (vehicle.WaitingFor)
            {
                case ObstacleReason.RedLight: stoppedAtRedLight++; break;
                case ObstacleReason.CarAhead: stoppedBehindCar++; break;
                case ObstacleReason.BusyJunction:
                case ObstacleReason.BusyLane: stoppedAtJunction++; break;
            }

            int intersection = IntersectionAhead(vehicle);
            if (intersection >= 0) queuedCarSteps[intersection]++;
        }

        // Count steps rather than compare times: adding 0.05 s thousands of times drifts a little.
        int stepsPerSample = Math.Max(1, (int)Math.Round(SampleSeconds / world.DeltaTime));
        if (steps >= stepsPerSample)
            FinishSample(world);
    }

    public void RecordRedLightRun() => redLightsRun++;

    public void RecordCollision(float time, RoadCell cell, Vehicle first, Vehicle second)
    {
        collisionsInInterval++;
        collisions.Add(new CollisionRecord
        {
            Time = time,
            Cell = cell.Position.ExcelAddress,
            FirstVehicle = first.Id,
            SecondVehicle = second.Id,
            FirstKind = KindOf(first),
            SecondKind = KindOf(second),
            RebelInvolved = !first.FollowsTrafficRules || !second.FollowsTrafficRules,
            TaxiInvolved = first is Taxi || second is Taxi,
            FasterCarKmh = Math.Max(first.Speed, second.Speed) * 3.6f
        });
    }

    static string KindOf(Vehicle vehicle)
    {
        if (vehicle is Taxi) return "Taxi";
        return vehicle.FollowsTrafficRules ? "Car" : "Rebel";
    }

    public void RecordRide(Pedestrian pedestrian)
    {
        rides.Add(new RideRecord
        {
            PedestrianId = pedestrian.Id,
            RequestedAt = pedestrian.RequestedAt,
            SecondsUntilTaxiAssigned = pedestrian.TaxiAssignedAt - pedestrian.RequestedAt,
            SecondsUntilPickup = pedestrian.PickedUpAt - pedestrian.RequestedAt,
            TripSeconds = pedestrian.FinishedAt - pedestrian.PickedUpAt,
            GaveUp = pedestrian.State == PedestrianState.GaveUp
        });
    }

    void FinishSample(World world)
    {
        int waitingPedestrians = 0;
        foreach (Pedestrian pedestrian in world.Pedestrians)
            if (pedestrian.State == PedestrianState.WaitingForTaxi) waitingPedestrians++;

        var queues = new float[queuedCarSteps.Length];
        for (int i = 0; i < queues.Length; i++) queues[i] = queuedCarSteps[i] / Math.Max(1, steps);

        samples.Add(new MetricsSample
        {
            Time = world.Time,
            AverageSpeedKmh = Average(speedSum, vehicleSteps) * 3.6f,
            ShareStopped = Average(stopped, vehicleSteps),
            ShareStoppedAtRedLight = Average(stoppedAtRedLight, vehicleSteps),
            ShareStoppedBehindCar = Average(stoppedBehindCar, vehicleSteps),
            ShareStoppedAtJunction = Average(stoppedAtJunction, vehicleSteps),
            KilometersDriven = metersDriven / 1000f,
            ShareOfTaxisBusy = Average(busyTaxiSteps, taxiSteps),
            PedestriansWaiting = waitingPedestrians,
            AverageQueuePerIntersection = queues,
            ShareSpeeding = Average(speedingSteps, vehicleSteps),
            RedLightsRun = redLightsRun,
            Collisions = collisionsInInterval
        });

        steps = vehicleSteps = stopped = stoppedAtRedLight = stoppedBehindCar = stoppedAtJunction = 0;
        taxiSteps = busyTaxiSteps = speedingSteps = redLightsRun = collisionsInInterval = 0;
        speedSum = metersDriven = 0f;
        Array.Clear(queuedCarSteps, 0, queuedCarSteps.Length);
    }

    static float Average(float sum, int count) => count > 0 ? sum / count : 0f;

    /// <summary>Intersections are numbered like in CityMapValidator.Describe: in the order of the network's controllers.</summary>
    void NumberIntersections(RoadNetwork network)
    {
        intersectionOfLight = new Dictionary<TrafficLight, int>();
        for (int i = 0; i < network.TrafficLightControllers.Count; i++)
            foreach (TrafficLight light in network.TrafficLightControllers[i].Lights)
                intersectionOfLight[light] = i;
        queuedCarSteps = new float[network.TrafficLightControllers.Count];
    }

    int IntersectionAhead(Vehicle vehicle)
    {
        VehiclePath path = vehicle.Path;
        int last = Math.Min(path.LastIndex, path.FrontIndex + CellsToLookForALight);
        for (int i = path.FrontIndex; i <= last; i++)
        {
            TrafficLight light = path.Cells[i].TrafficLight;
            if (light != null) return intersectionOfLight[light];
        }
        return -1;
    }
}

/// <summary>Averages over one interval of the simulation (see SimulationMetrics.SampleSeconds).</summary>
public class MetricsSample
{
    public float Time;                      // end of the interval, seconds since the start
    public float AverageSpeedKmh;           // average speed of all vehicles
    public float ShareStopped;              // 0..1 of vehicles stopped, for any reason
    public float ShareStoppedAtRedLight;
    public float ShareStoppedBehindCar;
    public float ShareStoppedAtJunction;    // waiting for a junction or a lane change
    public float KilometersDriven;          // by all vehicles together in this interval
    public float ShareOfTaxisBusy;          // 0..1 of taxis with a passenger assigned or on board
    public int PedestriansWaiting;          // waiting for a taxi to be assigned, at the end of the interval
    public float[] AverageQueuePerIntersection;   // stopped cars near each intersection
    public float ShareSpeeding;             // 0..1 of vehicles going faster than the limit of their street
    public int RedLightsRun;                // stop lines crossed on red in this interval
    public int Collisions;                  // crashes in this interval
}

/// <summary>One crash between two vehicles.</summary>
public class CollisionRecord
{
    public float Time;
    public string Cell;           // Excel address of the cell where it happened
    public int FirstVehicle;
    public int SecondVehicle;
    public string FirstKind;      // "Taxi", "Car" (law-abiding) or "Rebel"
    public string SecondKind;
    public bool RebelInvolved;
    public bool TaxiInvolved;
    public float FasterCarKmh;    // speed of the faster of the two cars
}

/// <summary>One finished ride request.</summary>
public class RideRecord
{
    public int PedestrianId;
    public float RequestedAt;
    public float SecondsUntilTaxiAssigned;  // NaN if no taxi was ever assigned
    public float SecondsUntilPickup;        // NaN if the pedestrian gave up
    public float TripSeconds;               // NaN if the pedestrian gave up
    public bool GaveUp;
}
