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
    }

    private bool HasValidContact(bool isBlue)
    {
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
        if (!showCorpseColorHint || corpseHintTriggered)
            return;

        corpseHintTriggered = true;
        corpseHintEndsAt = hintDuration > 0f
            ? Time.unscaledTime + hintDuration
            : float.PositiveInfinity;
    }

    private void OnGUI()
    {
        if (!showCorpseColorHint || !corpseHintTriggered ||
            Time.unscaledTime > corpseHintEndsAt || Camera.main == null)
            return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(
            transform.position + (Vector3)hintWorldOffset
        );
        if (screenPoint.z <= 0f)
            return;

        const float width = 720f;
        const float height = 82f;
        Rect hintRect = new Rect(
            screenPoint.x - width * 0.5f,
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
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
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
