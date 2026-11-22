using UnityEngine;

// The cable only observes the existing button. It never opens gates or changes
// the color manager, collision layers, or any gameplay object.
[DefaultExecutionOrder(1200)]
public sealed class JerrySignalCable : MonoBehaviour
{
    public Button source;
    public SpriteRenderer[] conductors = new SpriteRenderer[0];
    public SpriteRenderer[] terminalLights = new SpriteRenderer[0];
    public Color idleColor = new Color(.70f, .68f, .57f, .34f);
    public Color poweredColor = new Color(.85f, .79f, .54f, .66f);
    public Color idleLight = new Color(.65f, .64f, .54f, .55f);
    public Color poweredLight = new Color(.89f, .86f, .64f, .95f);

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        bool powered = source != null && source.IsPressed;
        foreach (var piece in conductors)
            if (piece != null)
                piece.color = powered ? poweredColor : idleColor;
        foreach (var light in terminalLights)
            if (light != null)
                light.color = powered ? poweredLight : idleLight;
    }
}
