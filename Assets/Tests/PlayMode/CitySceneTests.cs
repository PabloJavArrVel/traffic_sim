using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Opens the real City scene and lets it run, like pressing Play. Any error or exception logged while it runs
/// makes the test fail, so this also checks that the scene is wired correctly.
/// </summary>
public class CitySceneTests
{
    [UnityTest]
    public IEnumerator TheCityRunsWithCarsTaxisPedestriansAndLights()
    {
        SceneManager.LoadScene("City");
        yield return null;

        var simulation = Object.FindFirstObjectByType<SimulationManager>();
        Assert.That(simulation, Is.Not.Null, "the scene has a SimulationManager");

        Time.timeScale = 4f;   // 20 seconds of city life in a few real seconds
        yield return new WaitForSeconds(20f);
        Time.timeScale = 1f;

        World world = simulation.World;
        Assert.That(world, Is.Not.Null, "the city map loaded");
        Assert.That(world.Vehicles.Count, Is.EqualTo(simulation.ambientCarCount + simulation.taxiCount));
        Assert.That(world.FleetManager.Taxis.Count, Is.EqualTo(simulation.taxiCount));
        Assert.That(world.Pedestrians.Count, Is.GreaterThan(0));

        int carsThatMoved = 0;
        foreach (Vehicle vehicle in world.Vehicles)
            if (vehicle.DistanceDriven > 10f) carsThatMoved++;
        Assert.That(carsThatMoved, Is.GreaterThan(world.Vehicles.Count / 2), "most cars are driving");

        Assert.That(Object.FindObjectsByType<VehicleView>(FindObjectsSortMode.None).Length, Is.EqualTo(world.Vehicles.Count));
        Assert.That(Object.FindObjectsByType<TrafficLightView>(FindObjectsSortMode.None).Length, Is.EqualTo(world.Network.TrafficLights.Count));
    }
}
