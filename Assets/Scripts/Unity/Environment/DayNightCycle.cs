using UnityEngine;

/// <summary>Turns the sun around to make day and night. Put it on the Directional Light.</summary>
public class DayNightCycle : MonoBehaviour
{
    [Tooltip("Degrees the sun turns per second.")]
    public float rotationSpeed = 10f;

    void Update()
    {
        transform.Rotate(-rotationSpeed * Time.deltaTime, 0f, 0f);
    }
}
