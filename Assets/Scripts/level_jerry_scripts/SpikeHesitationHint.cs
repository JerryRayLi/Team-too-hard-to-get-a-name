using System.Collections.Generic;
using UnityEngine;

public sealed class SpikeHesitationHint : MonoBehaviour
{
    public PlayerRespawnJ player;
    [Min(0f)] public float hesitationDelay = 8f;
    [Min(0f)] public float heightAboveSpikes = 1.1f;
    public string hintText = "Try jumping onto the spikes.";

    private readonly List<Collider2D> spikeColliders = new List<Collider2D>();
    private float waitingTime;
    private bool dismissed;
    private bool approachFromLeft;
    private int observedDeaths;
    private GUIStyle hintStyle;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerRespawnJ>();

        foreach (Collider2D candidate in GetComponentsInChildren<Collider2D>(true))
        {
            if (candidate.GetComponentInParent<HazardJ>() != null)
                spikeColliders.Add(candidate);
        }

        if (player != null && TryGetSpikesBounds(out Bounds bounds))
            approachFromLeft = player.transform.position.x < bounds.center.x;
    }

    private void Update()
    {
        if (dismissed || player == null || Time.timeScale <= 0f)
            return;

        if (PlayerRespawnJ.DeathCount != observedDeaths)
        {
            observedDeaths = PlayerRespawnJ.DeathCount;
            // A fall alone does not mean the player has discovered corpse
            // platforms. A corpse left on the spikes does.
            if (FindFirstObjectByType<Corpse>() != null)
            {
                dismissed = true;
                return;
            }
        }

        if (player.IsRespawning || !TryGetSpikesBounds(out Bounds bounds))
            return;

        float playerX = player.transform.position.x;
        bool hasTriedCrossing = approachFromLeft
            ? playerX >= bounds.min.x + 0.1f
            : playerX <= bounds.max.x - 0.1f;
        if (hasTriedCrossing)
        {
            dismissed = true;
            return;
        }

        // Wandering back and forth before the obstacle still counts as
        // hesitation; this is intentionally not a keyboard-idle timer.
        waitingTime += Time.deltaTime;
    }

    private bool TryGetSpikesBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Collider2D spike in spikeColliders)
        {
            if (spike == null || !spike.enabled || !spike.gameObject.activeInHierarchy)
                continue;
            if (found)
                bounds.Encapsulate(spike.bounds);
            else
            {
                bounds = spike.bounds;
                found = true;
            }
        }
        return found;
    }

    private void OnGUI()
    {
        if (dismissed || waitingTime < hesitationDelay || player == null ||
            player.IsRespawning || Time.timeScale <= 0f || Camera.main == null ||
            !TryGetSpikesBounds(out Bounds bounds))
            return;

        Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(
            bounds.center.x, bounds.max.y + heightAboveSpikes, bounds.center.z));
        if (screen.z <= 0f)
            return;

        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            hintStyle.normal.textColor = new Color(1f, 0.93f, 0.73f);
        }

        hintStyle.fontSize = GameHUD.ScaleFont(24);
        GUIContent caption = new GUIContent(hintText);
        float availableWidth = Mathf.Max(100f, Screen.width - 64f);
        float textWidth = hintStyle.CalcSize(caption).x;
        if (textWidth > availableWidth)
            hintStyle.fontSize = Mathf.Max(10, Mathf.FloorToInt(hintStyle.fontSize * availableWidth / textWidth));

        float width = Mathf.Min(Screen.width - 32f, hintStyle.CalcSize(caption).x + 32f);
        float height = Mathf.Max(40f, 42f * GameHUD.UiScale);
        Rect rectangle = new Rect(
            Mathf.Clamp(screen.x - width / 2f, 16f, Screen.width - width - 16f),
            Mathf.Clamp(Screen.height - screen.y - height / 2f, 16f, Screen.height - height - 16f),
            width, height);
        float fade = Mathf.Clamp01((waitingTime - hesitationDelay) / 0.3f);
        Color previous = GUI.color;
        GUI.color = new Color(0.04f, 0.07f, 0.09f, 0.82f * fade);
        GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, fade);
        GUI.Label(rectangle, caption, hintStyle);
        GUI.color = previous;
    }
}
