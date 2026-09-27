using UnityEngine;

/// <summary>
/// Shows the color of one traffic light by switching its lamps on and off.
/// Lives on the traffic light prefab; the three lamps are child objects assigned in the inspector.
/// </summary>
public class TrafficLightView : MonoBehaviour
{
    [Header("Lamps (child objects of the prefab)")]
    public GameObject greenLamp;
    public GameObject yellowLamp;
    public GameObject redLamp;

    TrafficLight trafficLight;
    LightColor shownColor;

    public void Show(TrafficLight trafficLight)
    {
        this.trafficLight = trafficLight;
        ShowColor(trafficLight.Color);
    }

    void Update()
    {
        if (trafficLight != null && trafficLight.Color != shownColor)
            ShowColor(trafficLight.Color);
    }

    void ShowColor(LightColor color)
    {
        shownColor = color;
        SetLamp(greenLamp, color == LightColor.Green);
        SetLamp(yellowLamp, color == LightColor.Yellow);
        SetLamp(redLamp, color == LightColor.Red);
    }

    static void SetLamp(GameObject lamp, bool on)
    {
        if (lamp != null) lamp.SetActive(on);
    }
}
