using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class ReverseStatusVisual : MonoBehaviour
{
    [Header("Player Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color reversedColor =
        new Color(0.7294118f, 0.3333333f, 0.8274511f, 1f);

    [Header("Dizzy Effect")]
    [SerializeField] private Vector3 effectOffset = new Vector3(0f, 0.78f, 0f);
    [SerializeField, Min(0.05f)] private float effectRadius = 0.42f;
    [SerializeField, Min(0.02f)] private float particleSize = 0.12f;

    private PlayerController playerController;
    private ParticleSystem dizzyEffect;
    private Color normalColor;
    private bool visualReversed;
    private bool effectVisible;
    private bool initialized;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
        }

        if (bodyRenderer == null)
        {
            Debug.LogError(
                "ReverseStatusVisual requires a player SpriteRenderer.",
                this
            );
            enabled = false;
            return;
        }

        normalColor = bodyRenderer.color;
        dizzyEffect = CreateDizzyEffect();
        initialized = true;
        ApplyVisualState(false, true);
    }

    private void LateUpdate()
    {
        if (!initialized || playerController == null)
        {
            return;
        }

        // HiddenPlatform already owns the control reversal. Reading its result
        // here keeps the feedback synchronized without adding collision logic.
        bool isReversed = playerController.moveSpeed < 0f;
        ApplyVisualState(isReversed, false);

        bool shouldShowEffect = isReversed && bodyRenderer.enabled;
        SetEffectVisible(shouldShowEffect);
    }

    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }

        bodyRenderer.color = normalColor;
        visualReversed = false;
        SetEffectVisible(false);
    }

    private void ApplyVisualState(bool isReversed, bool force)
    {
        if (!force && visualReversed == isReversed)
        {
            return;
        }

        visualReversed = isReversed;
        bodyRenderer.color = isReversed ? reversedColor : normalColor;
    }

    private void SetEffectVisible(bool shouldBeVisible)
    {
        if (dizzyEffect == null || effectVisible == shouldBeVisible)
        {
            return;
        }

        effectVisible = shouldBeVisible;

        if (shouldBeVisible)
        {
            dizzyEffect.Play(true);
        }
        else
        {
            dizzyEffect.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private ParticleSystem CreateDizzyEffect()
    {
        GameObject effectObject = new GameObject("Reverse Dizzy Effect");
        effectObject.layer = gameObject.layer;
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = effectOffset;

        ParticleSystem effect = effectObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = effect.main;
        main.loop = true;
        main.duration = 1f;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(
            particleSize * 0.75f,
            particleSize * 1.25f
        );
        main.startColor = reversedColor;
        main.maxParticles = 12;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = effect.emission;
        emission.enabled = true;
        emission.rateOverTime = 7f;

        ParticleSystem.ShapeModule shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = effectRadius;
        shape.radiusThickness = 1f;
        shape.arc = 360f;

        ParticleSystem.VelocityOverLifetimeModule velocity =
            effect.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(2.4f);

        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            effect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color =
            new ParticleSystem.MinMaxGradient(fadeGradient);

        ParticleSystemRenderer effectRenderer =
            effectObject.GetComponent<ParticleSystemRenderer>();
        effectRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        effectRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        effectRenderer.sortingOrder = bodyRenderer.sortingOrder + 10;

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
        return effect;
    }
}
