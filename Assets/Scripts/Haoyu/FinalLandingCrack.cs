using UnityEngine;
// Holds a player who walks onto it; breaks when the player lands on it from a jump or fall.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class FinalLandingCrack : MonoBehaviour
{
    [Min(0)] public float breakSpeed = 3f;
    [Min(0)] public float breakDelay = .05f;
    public SpriteRenderer[] crackLines = new SpriteRenderer[0];
    public bool Broken { get; private set; }
    float breakAt = float.PositiveInfinity;
    SpriteRenderer visual;
    Collider2D surface;
    void Awake(){visual=GetComponent<SpriteRenderer>();surface=GetComponent<Collider2D>();}
    void OnCollisionEnter2D(Collision2D c)
    {
        var m=FinalLevelManager.Instance;
        if(Broken || !float.IsPositiveInfinity(breakAt) || !m || !m.AcceptContact)return;
        var p=c.gameObject.GetComponent<FinalLevelPlayerController>();
        if(!p || p.GetComponent<Collider2D>().bounds.min.y < surface.bounds.max.y-.1f)return;
        if(Mathf.Abs(c.relativeVelocity.y) < breakSpeed)return;
        breakAt=Time.time+breakDelay;
        Debug.Log($"FINAL_EVENT crack_hit x={p.transform.position.x:F3} impact={Mathf.Abs(c.relativeVelocity.y):F2}");
    }
    void FixedUpdate()
    {
        if(Broken || Time.time<breakAt)return;
        Broken=true;visual.enabled=false;surface.enabled=false;
        foreach(var line in crackLines) if(line) line.enabled=false;
        Debug.Log("FINAL_EVENT crack_broken");
    }
    public void ResetState()
    {
        Broken=false;breakAt=float.PositiveInfinity;
        if(visual)visual.enabled=true;if(surface)surface.enabled=true;
        foreach(var line in crackLines) if(line) line.enabled=true;
    }
}
