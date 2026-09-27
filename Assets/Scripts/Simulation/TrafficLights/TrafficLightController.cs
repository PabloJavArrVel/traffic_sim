using System.Collections.Generic;

/// <summary>
/// Runs the traffic lights of one intersection. Its east-west lights and its north-south lights take turns:
///
///   east-west green -> east-west yellow -> all red -> north-south green -> north-south yellow -> all red -> ...
///
/// The "all red" moment gives the cars already inside the intersection time to leave it.
/// If an intersection only has lights in one direction, they alternate with a moment when all its lights are red.
/// </summary>
public class TrafficLightController
{
    enum Stage
    {
        Green,
        Yellow,
        AllRed
    }

    readonly List<List<TrafficLight>> phases = new List<List<TrafficLight>>();
    readonly List<TrafficLight> lights;
    readonly SimulationSettings settings;

    int currentPhase;
    Stage stage = Stage.Green;
    float secondsInStage;

    public TrafficLightController(List<TrafficLight> lights, SimulationSettings settings)
    {
        this.lights = lights;
        this.settings = settings;

        var eastWest = new List<TrafficLight>();
        var northSouth = new List<TrafficLight>();
        foreach (TrafficLight light in lights)
        {
            if (light.TrafficDirection.IsEastWest()) eastWest.Add(light);
            else northSouth.Add(light);
        }
        phases.Add(eastWest);
        phases.Add(northSouth);

        ShowStage();
    }

    public IReadOnlyList<TrafficLight> Lights => lights;

    public void Tick(float seconds)
    {
        secondsInStage += seconds;
        if (secondsInStage < SecondsInCurrentStage()) return;

        secondsInStage = 0f;
        if (stage == Stage.Green)
        {
            stage = Stage.Yellow;
        }
        else if (stage == Stage.Yellow)
        {
            stage = Stage.AllRed;
        }
        else
        {
            stage = Stage.Green;
            currentPhase = (currentPhase + 1) % phases.Count;
        }
        ShowStage();
    }

    float SecondsInCurrentStage()
    {
        switch (stage)
        {
            case Stage.Green: return settings.GreenLightSeconds;
            case Stage.Yellow: return settings.YellowLightSeconds;
            default: return settings.AllRedSeconds;
        }
    }

    void ShowStage()
    {
        for (int phase = 0; phase < phases.Count; phase++)
        {
            foreach (TrafficLight light in phases[phase])
                light.Color = phase == currentPhase ? ColorOfCurrentStage() : LightColor.Red;
        }
    }

    LightColor ColorOfCurrentStage()
    {
        switch (stage)
        {
            case Stage.Green: return LightColor.Green;
            case Stage.Yellow: return LightColor.Yellow;
            default: return LightColor.Red;
        }
    }
}
