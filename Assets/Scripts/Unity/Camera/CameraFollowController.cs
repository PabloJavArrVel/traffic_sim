using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A camera that follows a car, a taxi or a pedestrian, or flies freely. Put it on the Main Camera;
/// FollowCamHUD (on the same GameObject) draws the buttons and labels.
///
/// Follow mode:   Left/Right arrows or A/D - previous/next target
///                Tab - what to follow (all, taxis, cars, pedestrians)
///                F   - free camera
/// Free camera:   W/A/S/D/Q/E - move, right mouse button + drag - look around, Shift - faster, F - back to follow mode
/// </summary>
public class CameraFollowController : MonoBehaviour
{
    public enum TargetFilter
    {
        All,
        Taxis,
        Cars,
        Pedestrians
    }

    [Header("Following")]
    public float followDistance = 8f;
    public float followHeight = 3f;
    public float followSmoothing = 6f;

    [Header("Free camera")]
    public float freeCamSpeed = 10f;
    public float freeCamFastMult = 3f;
    public float freeCamSensitivity = 3f;

    [Header("Switching targets")]
    [Tooltip("Seconds of the fade to black when switching targets.")]
    public float fadeDuration = 0.4f;

    /// <summary>Optional full-screen black image (created by FollowCamHUD) used to fade when switching targets.</summary>
    [HideInInspector] public CanvasGroup screenFade;

    class Target
    {
        public Vehicle Vehicle;
        public Pedestrian Pedestrian;
        public Transform Transform;
    }

    readonly List<Target> allTargets = new List<Target>();
    readonly List<Target> visibleTargets = new List<Target>();   // the targets that pass the current filter
    int selectedIndex;
    bool snapToTarget;
    bool switchingTarget;
    float freeCamYaw;
    float freeCamPitch;

    public TargetFilter Filter { get; private set; } = TargetFilter.All;
    public bool IsFreeCamera { get; private set; }

    /// <summary>The taxi being followed, or null if the camera is following something else.</summary>
    public Taxi SelectedTaxi => !IsFreeCamera && Selected != null ? Selected.Vehicle as Taxi : null;

    Target Selected => visibleTargets.Count > 0 ? visibleTargets[selectedIndex] : null;

    public string FilterLabel
    {
        get
        {
            switch (Filter)
            {
                case TargetFilter.Taxis: return "Taxis";
                case TargetFilter.Cars: return "Cars";
                case TargetFilter.Pedestrians: return "Peds";
                default: return "All";
            }
        }
    }

    public string SelectedLabel
    {
        get
        {
            Target selected = Selected;
            if (selected == null) return Filter == TargetFilter.Pedestrians ? "No peds" : "No vehicles";

            string position = $"{selectedIndex + 1} / {visibleTargets.Count}";
            if (selected.Pedestrian != null) return $"Ped {position}  {TaxiDescriptions.Status(selected.Pedestrian)}";
            if (selected.Vehicle is Taxi) return $"Taxi  {position}";
            return selected.Vehicle.FollowsTrafficRules ? $"Car  {position}" : $"Rebel car  {position}";
        }
    }

    // ------------------------------------------------------------------
    // Targets (called by SimulationManager)
    // ------------------------------------------------------------------

    public void Follow(Vehicle vehicle, Transform shown)
    {
        AddTarget(new Target { Vehicle = vehicle, Transform = shown });
    }

    public void Follow(Pedestrian pedestrian, Transform shown)
    {
        AddTarget(new Target { Pedestrian = pedestrian, Transform = shown });
    }

    public void Forget(Pedestrian pedestrian)
    {
        allTargets.RemoveAll(target => target.Pedestrian == pedestrian);
        RefreshVisibleTargets();
    }

    void AddTarget(Target target)
    {
        bool firstTarget = Selected == null;
        allTargets.Add(target);
        RefreshVisibleTargets();
        if (firstTarget) snapToTarget = true;
    }

    /// <summary>Rebuilds the list of targets that pass the filter, keeping the selected one selected.</summary>
    void RefreshVisibleTargets()
    {
        Target previouslySelected = Selected;
        visibleTargets.Clear();
        foreach (Target target in allTargets)
            if (PassesFilter(target)) visibleTargets.Add(target);

        int index = visibleTargets.IndexOf(previouslySelected);
        selectedIndex = index >= 0 ? index : 0;
    }

    bool PassesFilter(Target target)
    {
        switch (Filter)
        {
            case TargetFilter.Taxis: return target.Vehicle is Taxi;
            case TargetFilter.Cars: return target.Vehicle is AmbientCar;
            case TargetFilter.Pedestrians: return target.Pedestrian != null;
            default: return target.Vehicle != null;
        }
    }

    // ------------------------------------------------------------------
    // Actions (keys and HUD buttons)
    // ------------------------------------------------------------------

    public void SelectNext() => SwitchTarget(+1);
    public void SelectPrevious() => SwitchTarget(-1);

    public void CycleFilter()
    {
        if (switchingTarget) return;
        StartCoroutine(SwitchWhileScreenIsBlack(() =>
        {
            Filter = Filter == TargetFilter.Pedestrians ? TargetFilter.All : Filter + 1;
            RefreshVisibleTargets();
            selectedIndex = 0;
        }));
    }

    public void ToggleFreeCamera()
    {
        IsFreeCamera = !IsFreeCamera;
        if (IsFreeCamera)
        {
            freeCamYaw = transform.eulerAngles.y;
            freeCamPitch = transform.eulerAngles.x;
        }
        else
        {
            snapToTarget = true;
        }
    }

    void SwitchTarget(int step)
    {
        int count = visibleTargets.Count;
        if (switchingTarget || count <= 1) return;
        int newIndex = (selectedIndex + step + count) % count;
        StartCoroutine(SwitchWhileScreenIsBlack(() => selectedIndex = newIndex));
    }

    /// <summary>Fades to black, changes the target while nothing is visible, then fades back in.</summary>
    IEnumerator SwitchWhileScreenIsBlack(System.Action changeTarget)
    {
        switchingTarget = true;
        yield return Fade(0f, 1f);
        changeTarget();
        snapToTarget = true;
        yield return new WaitForSeconds(0.05f);
        yield return Fade(1f, 0f);
        switchingTarget = false;
    }

    IEnumerator Fade(float fromAlpha, float toAlpha)
    {
        if (screenFade == null) yield break;

        screenFade.blocksRaycasts = true;
        for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.deltaTime)
        {
            screenFade.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration);
            yield return null;
        }
        screenFade.alpha = toAlpha;
        screenFade.blocksRaycasts = toAlpha > 0f;
    }

    // ------------------------------------------------------------------
    // Every frame
    // ------------------------------------------------------------------

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) ToggleFreeCamera();

        if (IsFreeCamera)
        {
            MoveFreeCamera();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SelectPrevious();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SelectNext();
        if (Input.GetKeyDown(KeyCode.Tab)) CycleFilter();
    }

    void LateUpdate()
    {
        if (IsFreeCamera || Selected == null || Selected.Transform == null) return;

        Transform target = Selected.Transform;
        Vector3 wantedPosition = target.position - target.forward * followDistance + Vector3.up * followHeight;
        Quaternion wantedRotation = Quaternion.LookRotation(target.position + Vector3.up * 0.5f - wantedPosition, Vector3.up);

        if (snapToTarget)
        {
            transform.SetPositionAndRotation(wantedPosition, wantedRotation);
            snapToTarget = false;
            return;
        }

        float blend = followSmoothing * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, wantedPosition, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, blend);
    }

    void MoveFreeCamera()
    {
        if (Input.GetMouseButton(1))
        {
            freeCamYaw += Input.GetAxis("Mouse X") * freeCamSensitivity;
            freeCamPitch -= Input.GetAxis("Mouse Y") * freeCamSensitivity;
            freeCamPitch = Mathf.Clamp(freeCamPitch, -80f, 80f);
            transform.rotation = Quaternion.Euler(freeCamPitch, freeCamYaw, 0f);
        }

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        float speed = freeCamSpeed * (Input.GetKey(KeyCode.LeftShift) ? freeCamFastMult : 1f);
        transform.position += move * speed * Time.deltaTime;
    }
}
