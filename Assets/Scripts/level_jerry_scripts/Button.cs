using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    public Door door;
    public bool useColorRules;
    public ColorLayerManager colorManager;
    public Color redColor = new Color(138f / 255f, 80f / 255f, 74f / 255f);
    public Color blueColor = new Color(63f / 255f, 101f / 255f, 112f / 255f);

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

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        contactFilter.useTriggers = false;
    }

    private void LateUpdate()
    {
        // Read after ColorLayerManager.Update so Shift also re-evaluates objects
        // already resting on the button, without requiring another collision.
        bool isBlue = colorManager != null && colorManager.IsBlueActive;
        if (useColorRules && buttonRenderer != null)
            buttonRenderer.color = isBlue ? blueColor : redColor;

        SetPressed(HasValidContact(isBlue));
        UpdateColorSwitchHint(isBlue);
    }

    private bool HasValidContact(bool isBlue)
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

                if (!useColorRules || isBlue)
                    return true;
            }
            else if (useColorRules && !isBlue)
            {
                PlayerRespawnJ player = other.GetComponentInParent<PlayerRespawnJ>();
                if (player != null && !player.IsRespawning &&
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

    private void UpdateColorSwitchHint(bool isBlue)
    {
        if (!delayedColorSwitchHint)
            return;

        if (corpseIsTouching && isBlue && isPressed)
            colorSwitchUnderstood = true;

        bool needsColorSwitch = showCorpseColorHint && useColorRules &&
            colorManager != null && corpseIsTouching && !isBlue && !isPressed &&
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

        float width = delayedColorSwitchHint ? Mathf.Min(640f, Screen.width - 32f) : 720f;
        float height = delayedColorSwitchHint ? 54f : 82f;
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
            fontSize = delayedColorSwitchHint ? Mathf.Clamp(Screen.height / 32, 18, 32) : 32,
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
