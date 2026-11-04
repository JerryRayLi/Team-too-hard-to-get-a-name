using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class JerryCheckpoint : MonoBehaviour
{
    public JerryLevelProgress progress;
    [Min(1)] public int checkpointOrder = 1;
    [Header("Label Placement")]
    public Collider2D hintUpperPlatform;
    public Collider2D hintLowerPlatform;
    public Vector2 hintOffset;
    public bool IsSaved => progress != null && progress.CheckpointOrder >= checkpointOrder;

    private void OnTriggerEnter2D(Collider2D other) => TrySave(other);
    private void OnTriggerStay2D(Collider2D other) => TrySave(other);

    private void TrySave(Collider2D other)
    {
        PlayerRespawnJ player = other.GetComponentInParent<PlayerRespawnJ>();
        if (player != null && !player.IsRespawning && progress != null)
            progress.Save(this, player);
    }

    private void OnGUI()
    {
        if (Camera.main == null || Time.timeScale <= 0f)
            return;
        Vector3 labelPosition = transform.position + Vector3.up * 1.1f;
        if (hintUpperPlatform != null && hintLowerPlatform != null)
        {
            Bounds upper = hintUpperPlatform.bounds;
            Bounds lower = hintLowerPlatform.bounds;
            // Use the space between platform edges rather than their centers.
            labelPosition = new Vector3((upper.center.x + lower.center.x) * 0.5f,
                (upper.min.y + lower.max.y) * 0.5f, transform.position.z);
        }
        Vector3 screen = Camera.main.WorldToScreenPoint(labelPosition + (Vector3)hintOffset);
        if (screen.z <= 0f)
            return;
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = GameHUD.ScaleFont(22),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = IsSaved ? new Color(0.5f, 1f, 0.65f) : new Color(1f, 0.88f, 0.5f);
        string caption = IsSaved ? "CHECKPOINT SAVED" : "CHECKPOINT";
        Vector2 size = style.CalcSize(new GUIContent(caption)) + new Vector2(16f, 8f);
        GUI.Label(new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y * 0.5f,
            size.x, size.y), caption, style);
    }
}
