using UnityEngine;

// Visual feedback only: the existing pressure plate still owns its collider
// and the Button still decides whether the gate opens.
[ExecuteAlways, DefaultExecutionOrder(1250)]
public sealed class JerryButtonFeedback : MonoBehaviour
{
    public Button source;
    public JerryPressurePlate plate;
    public SpriteRenderer indicator;
    public Transform[] movingDetails = new Transform[0];
    public SpriteRenderer[] statusLights = new SpriteRenderer[0];
    public SpriteRenderer activationGlow;
    [Min(0f)] public float extraVisualTravel = .035f;

    private Vector3[] restPositions;
    private bool initialized;
    private bool previouslyActive;
    private float flashRemaining;
    private float glowAlpha;

    private void Awake() => Initialize();
    private void OnEnable() => Refresh(0f);
    private void LateUpdate() => Refresh(Time.deltaTime);

    private void Initialize()
    {
        if (initialized || source == null || plate == null || indicator == null)
            return;
        restPositions = new Vector3[movingDetails.Length];
        for (int i = 0; i < movingDetails.Length; i++)
            if (movingDetails[i] != null)
                restPositions[i] = movingDetails[i].localPosition;
        initialized = true;
    }

    public void Refresh(float deltaTime)
    {
        Initialize();
        if (!initialized)
            return;
        bool visible = plate.colorSource.enabled && plate.sensor.enabled;
        bool active = visible && source.IsPressed;
        float weight = Mathf.Clamp01(plate.PressAmount);
        if (active && !previouslyActive)
            flashRemaining = .45f;
        else
            flashRemaining = Mathf.Max(0f, flashRemaining - Mathf.Max(0f, deltaTime));
        previouslyActive = active;

        float flash = flashRemaining / .45f;
        float brightness = active ? .48f + .20f * flash : Mathf.Lerp(.08f, .28f, weight);
        Color baseColor = source.useColorRules
            ? JerryGateButtonLight.SignalColor(source)
            : plate.colorSource.color;
        indicator.color = Color.Lerp(baseColor, Color.white, brightness);
        if (plate.cap != null)
            plate.cap.color = Color.Lerp(baseColor, Color.white, active ? .38f : .08f);
        indicator.enabled = visible;

        float physicalTravel = plate.sensor.size.y * Mathf.Abs(plate.transform.lossyScale.y) * plate.travelFraction;
        for (int i = 0; i < movingDetails.Length; i++)
        {
            var detail = movingDetails[i];
            if (detail != null)
                detail.localPosition = restPositions[i] + detail.parent.InverseTransformVector(
                    Vector3.down * ((physicalTravel + extraVisualTravel) * weight));
        }

        Color status = active ? new Color(1f, .93f, .68f, 1f)
            : weight > .10f ? new Color(.77f, .56f, .27f, 1f) : new Color(.17f, .23f, .25f, 1f);
        foreach (var light in statusLights)
            if (light != null)
            {
                light.enabled = visible;
                light.color = status;
            }
        if (activationGlow != null)
        {
            float target = active ? .22f + .16f * flash : .05f * weight;
            glowAlpha = visible ? Mathf.MoveTowards(glowAlpha, target, Mathf.Max(0f, deltaTime) * 3f) : 0f;
            var color = active ? new Color(1f, .86f, .50f, glowAlpha)
                : new Color(.90f, .65f, .30f, glowAlpha);
            activationGlow.color = color;
            activationGlow.enabled = visible && glowAlpha > .001f;
            activationGlow.transform.position = indicator.bounds.center;
        }
    }

    private void OnDisable()
    {
        previouslyActive = false;
        flashRemaining = glowAlpha = 0f;
        if (activationGlow != null)
            activationGlow.enabled = false;
        foreach (var light in statusLights)
            if (light != null)
                light.enabled = false;
        if (initialized)
            for (int i = 0; i < movingDetails.Length; i++)
                if (movingDetails[i] != null)
                    movingDetails[i].localPosition = restPositions[i];
    }
}
