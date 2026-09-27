using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The on-screen controls of the follow camera. Put it on the Main Camera, next to CameraFollowController.
///
///   Bottom:    [Filter]  [◀]  Car 1 / 35  [▶]  [Free]
///   Top-left:  status of the followed taxi (only when following a taxi)
/// </summary>
[RequireComponent(typeof(CameraFollowController))]
public class FollowCamHUD : MonoBehaviour
{
    [Header("Colors")]
    public Color hudBackground = new Color(0.06f, 0.06f, 0.08f, 0.90f);
    public Color buttonColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    public Color accentColor = new Color(0.35f, 0.90f, 0.65f, 1f);
    public Color textColor = new Color(0.92f, 0.92f, 0.95f, 1f);
    public Color taxiIdleColor = new Color(0.35f, 0.90f, 0.65f, 1f);
    public Color taxiEnRouteColor = new Color(1f, 0.80f, 0.20f, 1f);
    public Color taxiRidingColor = new Color(0.40f, 0.70f, 1f, 1f);

    [Header("Layout")]
    public float bottomPadding = 32f;
    public float pillHeight = 52f;
    public float statusPadding = 20f;

    CameraFollowController followCamera;
    TMP_Text filterLabel;
    TMP_Text targetLabel;
    TMP_Text freeCameraLabel;
    GameObject taxiPanel;
    TMP_Text taxiStatusLabel;

    void Awake()
    {
        followCamera = GetComponent<CameraFollowController>();
        Canvas canvas = HudBuilder.CreateScreenCanvas(transform, "Follow camera HUD", sortingOrder: 100);
        BuildTaxiPanel(canvas.transform);
        BuildControls(canvas.transform);
        followCamera.screenFade = HudBuilder.CreateScreenFade(canvas.transform);   // created last so it covers everything
    }

    void BuildControls(Transform canvas)
    {
        GameObject pill = HudBuilder.CreateHorizontalPanel(canvas, "Camera controls", hudBackground,
            corner: new Vector2(0.5f, 0f), offsetFromCorner: new Vector2(0f, bottomPadding), height: pillHeight);

        HudBuilder.CreateButton(pill.transform, "Filter", "All", 90f, pillHeight, buttonColor, accentColor, out filterLabel)
            .onClick.AddListener(followCamera.CycleFilter);
        // Plain < and > because the default font has no arrow symbols.
        HudBuilder.CreateButton(pill.transform, "Previous", "<", 44f, pillHeight, Color.clear, textColor, out _)
            .onClick.AddListener(followCamera.SelectPrevious);
        targetLabel = HudBuilder.CreateLabel(pill.transform, "Target", "", textColor, 14f,
            FontStyles.Normal, TextAlignmentOptions.Center, width: 190f);
        HudBuilder.CreateButton(pill.transform, "Next", ">", 44f, pillHeight, Color.clear, textColor, out _)
            .onClick.AddListener(followCamera.SelectNext);
        HudBuilder.CreateButton(pill.transform, "Free camera", "Free", 70f, pillHeight, buttonColor, textColor, out freeCameraLabel)
            .onClick.AddListener(followCamera.ToggleFreeCamera);
    }

    void BuildTaxiPanel(Transform canvas)
    {
        taxiPanel = HudBuilder.CreateVerticalPanel(canvas, "Taxi status", hudBackground,
            corner: new Vector2(0f, 1f), offsetFromCorner: new Vector2(statusPadding, -statusPadding), width: 320f);
        HudBuilder.CreateLabel(taxiPanel.transform, "Title", "TAXI", textColor, 10f, FontStyles.Bold);
        taxiStatusLabel = HudBuilder.CreateLabel(taxiPanel.transform, "Status", "", taxiIdleColor, 14f, FontStyles.Bold);
        taxiPanel.SetActive(false);
    }

    void Update()
    {
        filterLabel.text = followCamera.FilterLabel;
        targetLabel.text = followCamera.IsFreeCamera ? "Free camera" : followCamera.SelectedLabel;
        freeCameraLabel.text = followCamera.IsFreeCamera ? "Follow" : "Free";

        Taxi taxi = followCamera.SelectedTaxi;
        taxiPanel.SetActive(taxi != null);
        if (taxi == null) return;

        taxiStatusLabel.text = TaxiDescriptions.Status(taxi);
        taxiStatusLabel.color = ColorFor(taxi.State);
    }

    Color ColorFor(TaxiState state)
    {
        switch (state)
        {
            case TaxiState.Cruising: return taxiIdleColor;
            case TaxiState.GoingToPickup:
            case TaxiState.Boarding: return taxiEnRouteColor;
            default: return taxiRidingColor;
        }
    }
}
