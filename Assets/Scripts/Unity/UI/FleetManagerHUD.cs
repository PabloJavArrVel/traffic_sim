using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// A panel in the top-right corner with the state of every taxi, how many pedestrians are waiting, riding or were
/// served, and live traffic metrics (the same ones experiments save). Put it on the same GameObject as the SimulationManager.
/// </summary>
[RequireComponent(typeof(SimulationManager))]
public class FleetManagerHUD : MonoBehaviour
{
    [Header("Colors")]
    public Color panelBackground = new Color(0.06f, 0.06f, 0.08f, 0.88f);
    public Color headerColor = new Color(0.92f, 0.92f, 0.95f, 1f);
    public Color idleColor = new Color(0.35f, 0.90f, 0.65f, 1f);
    public Color enRouteColor = new Color(1f, 0.80f, 0.20f, 1f);
    public Color carryingColor = new Color(0.40f, 0.70f, 1f, 1f);
    public Color pendingColor = new Color(0.85f, 0.55f, 0.25f, 1f);

    [Header("Layout")]
    public float rightPadding = 20f;
    public float topPadding = 20f;
    public float panelWidth = 280f;

    [Header("Update")]
    [Tooltip("Seconds between refreshes of the panel.")]
    public float refreshInterval = 0.25f;

    SimulationManager simulation;
    TMP_Text taxiText;
    TMP_Text pedestrianText;
    TMP_Text metricsText;
    float secondsSinceRefresh;

    void Start()
    {
        simulation = GetComponent<SimulationManager>();

        Canvas canvas = HudBuilder.CreateScreenCanvas(transform, "Fleet HUD", sortingOrder: 99);
        GameObject panel = HudBuilder.CreateVerticalPanel(canvas.transform, "Fleet panel", panelBackground,
            corner: new Vector2(1f, 1f), offsetFromCorner: new Vector2(-rightPadding, -topPadding), width: panelWidth);

        HudBuilder.CreateLabel(panel.transform, "Title", "FLEET MANAGER", headerColor, 11f, FontStyles.Bold);
        taxiText = HudBuilder.CreateLabel(panel.transform, "Taxis", "...", headerColor, 11f);
        HudBuilder.CreateLabel(panel.transform, "Separator", "──────────────────────", new Color(1f, 1f, 1f, 0.15f), 9f);
        pedestrianText = HudBuilder.CreateLabel(panel.transform, "Pedestrians", "...", pendingColor, 11f);
        HudBuilder.CreateLabel(panel.transform, "Separator", "──────────────────────", new Color(1f, 1f, 1f, 0.15f), 9f);
        metricsText = HudBuilder.CreateLabel(panel.transform, "Metrics", "...", headerColor, 11f);
    }

    void Update()
    {
        secondsSinceRefresh += Time.deltaTime;
        if (secondsSinceRefresh < refreshInterval || simulation.World == null) return;

        secondsSinceRefresh = 0f;
        taxiText.text = DescribeTaxis(simulation.World);
        pedestrianText.text = DescribePedestrians(simulation.World);
        metricsText.text = DescribeMetrics(simulation);
    }

    string DescribeTaxis(World world)
    {
        var text = new StringBuilder();
        text.AppendLine($"Taxis: {world.FleetManager.Taxis.Count}");

        int number = 1;
        foreach (Taxi taxi in world.FleetManager.Taxis)
        {
            string color = HudBuilder.ColorTag(ColorFor(taxi.State));
            text.AppendLine($"<color={color}>T{number}  {TaxiDescriptions.Status(taxi)}</color>");
            number++;
        }
        return text.ToString().TrimEnd();
    }

    string DescribePedestrians(World world)
    {
        int waiting = 0, taxiOnTheWay = 0, riding = 0;
        foreach (Pedestrian pedestrian in world.Pedestrians)
        {
            if (pedestrian.State == PedestrianState.WaitingForTaxi) waiting++;
            else if (pedestrian.State == PedestrianState.TaxiOnTheWay) taxiOnTheWay++;
            else if (pedestrian.State == PedestrianState.Riding) riding++;
        }

        SimulationStatistics statistics = world.Statistics;
        var text = new StringBuilder();
        text.AppendLine($"<color={HudBuilder.ColorTag(headerColor)}>Pasajeros activos: {waiting + taxiOnTheWay + riding}</color>");
        text.AppendLine($"<color={HudBuilder.ColorTag(pendingColor)}>  Esperando taxi: {waiting}</color>");
        text.AppendLine($"<color={HudBuilder.ColorTag(enRouteColor)}>  Taxi asignado:  {taxiOnTheWay}</color>");
        text.AppendLine($"<color={HudBuilder.ColorTag(carryingColor)}>  En viaje:       {riding}</color>");
        text.AppendLine($"<color={HudBuilder.ColorTag(headerColor)}>Viajes completados: {statistics.RidesCompleted} · se cansaron: {statistics.RidesGivenUp}</color>");
        return text.ToString().TrimEnd();
    }

    /// <summary>The last measured interval of traffic, and the rides so far.</summary>
    static string DescribeMetrics(SimulationManager simulation)
    {
        var text = new StringBuilder();
        text.AppendLine($"MÉTRICAS · escenario {simulation.Scenario.Name}");

        IReadOnlyList<MetricsSample> samples = simulation.World.Metrics.Samples;
        if (samples.Count == 0) return text.Append("  midiendo...").ToString();

        MetricsSample last = samples[samples.Count - 1];
        text.AppendLine($"  Velocidad media: {last.AverageSpeedKmh:F1} km/h");
        text.AppendLine($"  Detenidos: {last.ShareStopped:P0} (semáforo {last.ShareStoppedAtRedLight:P0}, " +
                        $"fila {last.ShareStoppedBehindCar:P0}, cruce {last.ShareStoppedAtJunction:P0})");
        text.AppendLine($"  Taxis ocupados: {last.ShareOfTaxisBusy:P0}");

        float pickupSeconds = 0f;
        int completed = 0;
        foreach (RideRecord ride in simulation.World.Metrics.Rides)
        {
            if (ride.GaveUp) continue;
            pickupSeconds += ride.SecondsUntilPickup;
            completed++;
        }
        if (completed > 0) text.AppendLine($"  Espera media por taxi: {pickupSeconds / completed:F0} s");

        int redLightsRun = 0;
        foreach (MetricsSample sample in samples) redLightsRun += sample.RedLightsRun;
        text.AppendLine($"  Choques: {simulation.World.Metrics.Collisions.Count} · rojos pasados: {redLightsRun}");
        return text.ToString().TrimEnd();
    }

    Color ColorFor(TaxiState state)
    {
        switch (state)
        {
            case TaxiState.Cruising: return idleColor;
            case TaxiState.GoingToPickup:
            case TaxiState.Boarding: return enRouteColor;
            default: return carryingColor;
        }
    }
}
