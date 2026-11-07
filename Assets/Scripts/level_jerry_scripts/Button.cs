using System.Collections.Generic;
using UnityEngine;

public enum RequiredButtonBackground
{
    Orange,
    Blue
}

public class Button : MonoBehaviour
{
    public Door door;
    public bool useColorRules;
    public RequiredButtonBackground requiredBackground = RequiredButtonBackground.Orange;
    public ColorLayerManager colorManager;
    public Color redColor = new Color(138f / 255f, 80f / 255f, 74f / 255f);
    public Color blueColor = new Color(63f / 255f, 101f / 255f, 112f / 255f);

    [Header("Button Outline")]
    public Color outlineColor = Color.black;
    [Min(0f)] public float outlineThickness = 0.04f;

    [Min(0f)] public float contactTolerance = 0.02f;

    [Header("Optional Corpse Color Hint")]
    public bool showCorpseColorHint;
    [Tooltip("Show a contextual reminder only while a corpse is blocked by the red state.")]
    public bool delayedColorSwitchHint;
    [Min(0f)] public float colorHintDelay = 3f;
    public string corpseColorHintText =
        "A corpse can only trigger a switch in the right color!";
    public Vector2 hintWorldOffset = new Vector2(0f, -1.7f);
    [Tooltip("Set to 0 to keep the hint visible until the level ends.")]
    [Min(0f)] public float hintDuration;

    private readonly List<Collider2D> nearbyColliders = new List<Collider2D>();
    private Collider2D buttonCollider;
    private SpriteRenderer buttonRenderer;
    private readonly List<SpriteRenderer> outlineRenderers = new List<SpriteRenderer>();
    private ContactFilter2D contactFilter;
    private bool isPressed;
    private bool corpseHintTriggered;
    private float corpseHintEndsAt;
    private bool corpseIsTouching;
    private float blockedByColorTime;
    private bool colorSwitchUnderstood;

    private void Awake()
    {
        buttonCollider = GetComponent<Collider2D>();
        buttonRenderer = GetComponent<SpriteRenderer>();
        if (useColorRules && colorManager == null)
            colorManager = FindFirstObjectByType<ColorLayerManager>();
        if (useColorRules && buttonRenderer != null)
            buttonRenderer.color = requiredBackground == RequiredButtonBackground.Blue
                ? blueColor : redColor;
        CreateOutline();

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        contactFilter.useTriggers = false;
    }

    private void CreateOutline()
    {
        if (buttonRenderer == null || buttonRenderer.sprite == null ||
            outlineThickness <= 0f)
            return;

        Bounds spriteBounds = buttonRenderer.sprite.bounds;
        float localThicknessX = outlineThickness /
            Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float localThicknessY = outlineThickness /
            Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        int originalOrder = buttonRenderer.sortingOrder;

        // Leave the bottom edge open so the switch reads as a floor button
        // instead of a fully boxed obstacle.
        CreateOutlinePart("Button Outline Left",
            new Vector2(spriteBounds.min.x - localThicknessX * 0.5f,
                spriteBounds.center.y),
            new Vector2(localThicknessX, spriteBounds.size.y), originalOrder + 1);
        CreateOutlinePart("Button Outline Right",
            new Vector2(spriteBounds.max.x + localThicknessX * 0.5f,
                spriteBounds.center.y),
            new Vector2(localThicknessX, spriteBounds.size.y), originalOrder + 1);
        CreateOutlinePart("Button Outline Top",
            new Vector2(spriteBounds.center.x,
                spriteBounds.max.y + localThicknessY * 0.5f),
            new Vector2(spriteBounds.size.x + localThicknessX * 2f,
                localThicknessY), originalOrder + 1);

        buttonRenderer.sortingOrder = originalOrder + 2;
    }

    private void CreateOutlinePart(string objectName, Vector2 targetCenter,
        Vector2 targetSize, int sortingOrder)
    {
        Bounds spriteBounds = buttonRenderer.sprite.bounds;
        Vector2 partScale = new Vector2(
            targetSize.x / Mathf.Max(0.0001f, spriteBounds.size.x),
            targetSize.y / Mathf.Max(0.0001f, spriteBounds.size.y));

        GameObject outlineObject = new GameObject(objectName);
        outlineObject.layer = gameObject.layer;
        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = new Vector3(partScale.x, partScale.y, 1f);
        outlineObject.transform.localPosition = new Vector3(
            targetCenter.x - spriteBounds.center.x * partScale.x,
            targetCenter.y - spriteBounds.center.y * partScale.y,
            0f);

        SpriteRenderer partRenderer = outlineObject.AddComponent<SpriteRenderer>();
        partRenderer.sprite = buttonRenderer.sprite;
        partRenderer.sharedMaterial = buttonRenderer.sharedMaterial;
        partRenderer.color = outlineColor;
        partRenderer.sortingLayerID = buttonRenderer.sortingLayerID;
        partRenderer.sortingOrder = sortingOrder;
        partRenderer.maskInteraction = buttonRenderer.maskInteraction;
        partRenderer.enabled = buttonRenderer.enabled;
        outlineRenderers.Add(partRenderer);
    }

    private void LateUpdate()
    {
        bool outlineEnabled = buttonRenderer != null && buttonRenderer.enabled;
        foreach (SpriteRenderer outlineRenderer in outlineRenderers)
        {
            if (outlineRenderer != null)
                outlineRenderer.enabled = outlineEnabled;
        }

        // Read after ColorLayerManager.Update so Shift also re-evaluates objects
        // already resting on the button, without requiring another collision.
        bool backgroundMatches = IsRequiredBackgroundActive();
        SetPressed(HasValidContact(backgroundMatches));
        UpdateColorSwitchHint(backgroundMatches);
    }

    private bool IsRequiredBackgroundActive()
    {
        if (!useColorRules)
            return true;
        if (colorManager == null)
            return false;

        return requiredBackground == RequiredButtonBackground.Blue
            ? colorManager.IsBlueBackground
            : colorManager.IsOrangeBackground;
    }

    private bool HasValidContact(bool backgroundMatches)
    {
        corpseIsTouching = false;
        if (buttonCollider == null || !buttonCollider.enabled)
            return false;
        if (useColorRules && colorManager == null)
            return false;

        Bounds buttonBounds = buttonCollider.bounds;
        // Static corpses do not generate collision callbacks against a static
        // button. Query touching shapes directly, including resting corpses.
        Physics2D.OverlapBox(buttonBounds.center,
            (Vector2)buttonBounds.size + Vector2.one * (contactTolerance * 2f),
            0f, contactFilter, nearbyColliders);

        foreach (Collider2D other in nearbyColliders)
        {
            if (other == buttonCollider)
                continue;

            ColliderDistance2D separation = buttonCollider.Distance(other);
            if (!separation.isValid || separation.distance > contactTolerance)
                continue;

            Corpse corpse = other.GetComponentInParent<Corpse>();
            if (corpse != null)
            {
                corpseIsTouching = true;
                ShowCorpseColorHint();

                if (backgroundMatches)
                    return true;
            }
            else
            {
                PlayerRespawnJ player = other.GetComponentInParent<PlayerRespawnJ>();
                if (backgroundMatches && player != null && !player.IsRespawning &&
                    other.bounds.center.y > buttonBounds.max.y &&
                    other.bounds.min.y >= buttonBounds.max.y - contactTolerance)
                    return true;
            }
        }

        return false;
    }

    private void ShowCorpseColorHint()
    {
        if (!showCorpseColorHint || delayedColorSwitchHint || corpseHintTriggered)
            return;

        corpseHintTriggered = true;
        corpseHintEndsAt = hintDuration > 0f
            ? Time.unscaledTime + hintDuration
            : float.PositiveInfinity;
    }

    private void UpdateColorSwitchHint(bool backgroundMatches)
    {
        if (!delayedColorSwitchHint)
            return;

        if (corpseIsTouching && backgroundMatches && isPressed)
            colorSwitchUnderstood = true;

        bool needsColorSwitch = showCorpseColorHint && useColorRules &&
            colorManager != null && corpseIsTouching && !backgroundMatches && !isPressed &&
            !colorSwitchUnderstood;
        if (!needsColorSwitch)
        {
            blockedByColorTime = 0f;
            corpseHintTriggered = false;
            return;
        }

        if (Time.timeScale <= 0f)
            return;

        blockedByColorTime += Time.deltaTime;
        if (!corpseHintTriggered && blockedByColorTime >= colorHintDelay)
        {
            corpseHintTriggered = true;
            corpseHintEndsAt = hintDuration > 0f
                ? Time.unscaledTime + hintDuration
                : float.PositiveInfinity;
        }
    }

    private void OnGUI()
    {
        if (!showCorpseColorHint || !corpseHintTriggered ||
            Time.unscaledTime > corpseHintEndsAt || Camera.main == null ||
            (delayedColorSwitchHint && Time.timeScale <= 0f))
            return;

        Vector3 hintPosition = transform.position + (Vector3)hintWorldOffset;
        if (delayedColorSwitchHint && buttonCollider != null)
            hintPosition = new Vector3(buttonCollider.bounds.center.x + hintWorldOffset.x,
                buttonCollider.bounds.max.y + hintWorldOffset.y, transform.position.z);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(hintPosition);
        if (screenPoint.z <= 0f)
            return;

        float width = Mathf.Min((delayedColorSwitchHint ? 560f : 640f) * GameHUD.UiScale, Screen.width - 32f);
        float height = (delayedColorSwitchHint ? 42f : 64f) * GameHUD.UiScale;
        Rect hintRect = new Rect(
            delayedColorSwitchHint
                ? Mathf.Clamp(screenPoint.x - width * 0.5f, 16f, Screen.width - width - 16f)
                : screenPoint.x - width * 0.5f,
            Screen.height - screenPoint.y - height * 0.5f,
            width,
            height
        );

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(hintRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = GameHUD.ScaleFont(22),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = !delayedColorSwitchHint
        };
        if (delayedColorSwitchHint)
        {
            float captionWidth = hintStyle.CalcSize(new GUIContent(corpseColorHintText)).x;
            if (captionWidth > width - 24f)
                hintStyle.fontSize = Mathf.Max(10, Mathf.FloorToInt(hintStyle.fontSize * (width - 24f) / captionWidth));
        }
        hintStyle.normal.textColor = Color.white;
        GUI.Label(hintRect, corpseColorHintText, hintStyle);
    }

    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed)
            return;

        isPressed = pressed;
        if (door == null)
            return;
        if (pressed)
            door.Open();
        else
            door.Close();
    }

    private void OnDisable()
    {
        SetPressed(false);
    }
}
