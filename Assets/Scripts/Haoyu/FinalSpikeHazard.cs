using UnityEngine;
public sealed class FinalSpikeHazard : MonoBehaviour
{
    public string deathReason = "spike";
    public SpriteRenderer[] revealTogether;
    SpriteRenderer[] visuals;
    bool[] initiallyVisible;
    void Awake()
    {
        visuals = revealTogether != null && revealTogether.Length > 0
            ? revealTogether : GetComponents<SpriteRenderer>();
        initiallyVisible = new bool[visuals.Length];
        for (int i = 0; i < visuals.Length; i++) initiallyVisible[i] = visuals[i] && visuals[i].enabled;
    }
    void OnTriggerEnter2D(Collider2D c){Hit(c);}
    void OnTriggerStay2D(Collider2D c){Hit(c);}
    void Hit(Collider2D c)
    {
        var manager = FinalLevelManager.Instance;
        if (!manager || !manager.AcceptContact || !c.GetComponent<FinalLevelPlayerController>()) return;
        foreach (var visual in visuals) if (visual) visual.enabled = true;
        manager.Die(deathReason);
    }
    public void ResetState()
    {
        for (int i = 0; i < visuals.Length; i++) if (visuals[i]) visuals[i].enabled = initiallyVisible[i];
    }
}

