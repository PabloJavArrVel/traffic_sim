public enum LightColor
{
    Green,
    Yellow,
    Red
}

/// <summary>
/// A traffic light at the end of one street cell (an S cell in the Excel map).
/// Cars wait at the end of that cell while the light is red. Its TrafficLightController changes the color.
/// </summary>
public class TrafficLight
{
    public TrafficLight(RoadCell cell)
    {
        Cell = cell;
    }

    public RoadCell Cell { get; }
    public Direction TrafficDirection => Cell.TrafficDirection;
    public LightColor Color { get; set; } = LightColor.Red;
}
