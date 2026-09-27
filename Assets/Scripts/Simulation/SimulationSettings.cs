/// <summary>
/// All the numbers that tune the simulation, in one place.
/// Units: meters, seconds and km/h (speeds are converted to m/s for the formulas).
/// </summary>
public class SimulationSettings
{
    // ---- Map ----------------------------------------------------------
    /// <summary>One cell of the Excel map is 1.2 Unity units; the city models are built at 1 unit = 4.5 m.</summary>
    public float MetersPerCell = 5.4f;

    // ---- Cars ---------------------------------------------------------
    public float CarLength = 4.5f;
    public float SpeedLimitKmh = 30f;
    public float TurnSpeedKmh = 15f;

    /// <summary>
    /// Space a car leaves in front of it when it stops. Car + gap (4.9 m) must fit in one cell (5.4 m) with room
    /// to spare, so a stopped car never keeps its rear in the cell behind it (which could be a junction).
    /// </summary>
    public float StoppedGap = 0.4f;

    /// <summary>Extra meters a lane change "costs" when planning routes, so cars only change lanes when it helps.</summary>
    public float LaneChangeExtraCost = 5.4f;

    /// <summary>Extra meters a cell with a stopped car "costs" when planning routes, so cars drive around jams.</summary>
    public float StoppedCarExtraCost = 100f;

    /// <summary>A car stuck this long (not at a red light) looks for another way around.</summary>
    public float SecondsStuckBeforeLookingForAnotherWay = 45f;

    // ---- Traffic lights -----------------------------------------------
    public float GreenLightSeconds = 15f;
    public float YellowLightSeconds = 3f;

    /// <summary>All lights of an intersection stay red this long between turns, so it can empty.</summary>
    public float AllRedSeconds = 2f;

    // ---- Taxis --------------------------------------------------------
    public float SecondsToGetInOrOut = 3f;

    // ---- Handy conversions ----------------------------------------------
    public float SpeedLimit => SpeedLimitKmh / 3.6f;
    public float TurnSpeed => TurnSpeedKmh / 3.6f;
}
