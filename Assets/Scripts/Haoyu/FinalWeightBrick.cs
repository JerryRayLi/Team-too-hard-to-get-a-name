using UnityEngine;
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class FinalWeightBrick : MonoBehaviour
{
    public Color triggeredColor = new Color(.95f,.55f,.08f,1);
    public bool Triggered { get; private set; }
    SpriteRenderer visual;
    Collider2D surface;
    Color original;
    void Awake() { visual=GetComponent<SpriteRenderer>(); surface=GetComponent<Collider2D>(); original=visual.color; }
    void OnCollisionEnter2D(Collision2D c) { Contact(c); }
    void OnCollisionStay2D(Collision2D c) { Contact(c); }
    void Contact(Collision2D c)
    {
        if (Triggered || !FinalLevelManager.Instance || !FinalLevelManager.Instance.AcceptContact) return;
        var p=c.gameObject.GetComponent<FinalLevelPlayerController>();
        if (!p) return;
        foreach(var contact in c.contacts)
            if (contact.normal.y < -.65f && contact.point.y >= surface.bounds.max.y-.08f && p.GetComponent<Collider2D>().bounds.min.y >= surface.bounds.max.y-.1f)
            {
                Triggered=true; visual.color=triggeredColor; p.ApplyWeightedState(); FinalLevelManager.Instance.Weighted();
                Debug.Log($"FINAL_EVENT weighted x={p.transform.position.x:F3} speed={p.CurrentSpeed:F3} gravity={p.Body.gravityScale:F3}");
                return;
            }
    }
    public void ResetState() { Triggered=false; if(visual)visual.color=original; }
}

