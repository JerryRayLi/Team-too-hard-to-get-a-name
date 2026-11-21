using UnityEngine;

// Decorations observe gameplay state; they never modify collisions or mechanics.
[DefaultExecutionOrder(1000)]
public sealed class JerryLabArt : MonoBehaviour
{
    public SpriteRenderer sourceRenderer;
    public SpriteRenderer[] pieces = new SpriteRenderer[0];
    public SpriteRenderer[] indicators = new SpriteRenderer[0];
    public bool useSourceColor;
    public JerryCheckpoint checkpoint;
    public Color unsavedColor = new Color(.82f, .71f, .43f, 1f);
    public Color savedColor = new Color(.43f, .85f, .68f, 1f);

    private void LateUpdate() => RefreshState();

    public void RefreshState()
    {
        bool visible = sourceRenderer == null ||
            (sourceRenderer.enabled && sourceRenderer.gameObject.activeInHierarchy);
        foreach (SpriteRenderer piece in pieces)
            if (piece != null)
                piece.enabled = visible;

        if (checkpoint == null && (!useSourceColor || sourceRenderer == null))
            return;
        Color color = checkpoint != null
            ? (checkpoint.IsSaved ? savedColor : unsavedColor)
            : sourceRenderer.color;
        foreach (SpriteRenderer indicator in indicators)
            if (indicator != null)
                indicator.color = color;
    }
}
