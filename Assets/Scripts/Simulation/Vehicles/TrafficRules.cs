/// <summary>
/// The traffic rules. Law-abiding drivers (every taxi and most ambient cars) follow all of them perfectly;
/// rebels ignore all of them.
///
///   Traffic lights   Stop at red. At yellow, stop if you can do it comfortably, otherwise keep going.
///   Speed limits     50 km/h on avenues (streets with two lanes side by side), 30 km/h on streets with one lane
///                    and inside roundabouts. Slow down before you enter a slower street.
///   Right of way     Where no traffic light decides: cars already in a roundabout go before cars entering it;
///                    cars going straight along a street go before cars turning into it; between equals, whoever
///                    has waited longest goes first.
///   Crossings        Never stop inside a crossing (Vehicle only drives into one when it can drive all the way through).
/// </summary>
public static class TrafficRules
{
    /// <summary>A law-abiding driver gives way to a car with more right of way that will reach the junction within this time.</summary>
    public const float SecondsToLookForTrafficWithRightOfWay = 3f;

    public static bool MustStopForLight(TrafficLight light, float brakingDistance, float distanceToStopLine)
    {
        if (light == null || light.Color == LightColor.Green) return false;
        if (light.Color == LightColor.Red) return true;

        // Yellow: stop if we can do it comfortably, otherwise it's safer to keep going.
        return brakingDistance <= distanceToStopLine;
    }

    /// <summary>
    /// How much right of way a car has when it drives into a junction through 'entrance':
    /// 2 = it is already driving around the roundabout, 1 = it goes straight along the street, 0 = it turns into it.
    /// </summary>
    public static int RightOfWay(CellLink entrance)
    {
        RoadCell junction = entrance.To;
        if (junction.IsInRoundabout)
            return entrance.From.IsInRoundabout ? 2 : 0;

        bool goingStraight = entrance.Heading == junction.TrafficDirection;
        return goingStraight ? 1 : 0;
    }
}
