using UnityEngine;
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class FinalCollapsePlatform : MonoBehaviour
{
    [Min(0)] public float approachDistance=.3f;
    [Min(0)] public float collapseDelay=.06f;
    public bool Triggered { get; private set; }
    public bool Collapsed { get; private set; }
    float collapseAt;
    SpriteRenderer visual;
    Collider2D surface;
    Collider2D playerShape;
    void Awake(){visual=GetComponent<SpriteRenderer>();surface=GetComponent<Collider2D>();}
    void FixedUpdate()
    {
        var manager=FinalLevelManager.Instance;
        if(!manager || !manager.AcceptContact)return;
        if(!playerShape)playerShape=manager.player.GetComponent<Collider2D>();
        if(!Triggered)
        {
            var p=playerShape.bounds;var b=surface.bounds;
            float vx=manager.player.Body.linearVelocity.x;
            bool approachingLeft=vx>.01f && p.max.x>=b.min.x-approachDistance && p.min.x<b.max.x;
            bool approachingRight=vx<-.01f && p.min.x<=b.max.x+approachDistance && p.max.x>b.min.x;
            bool overlappingTop=p.max.x>b.min.x && p.min.x<b.max.x;
            // A jump above the surface does not arm the concealed ground trap.
            bool feetNearSurface=p.min.y>=b.max.y-.15f && p.min.y<=b.max.y+.2f;
            if(feetNearSurface && (approachingLeft || approachingRight || overlappingTop))
            {Triggered=true;collapseAt=Time.time+collapseDelay;Debug.Log("FINAL_EVENT collapse_armed feetX="+p.max.x+" edgeX="+b.min.x);}
        }
        if(Triggered && !Collapsed && Time.time>=collapseAt)
        {Collapsed=true;visual.enabled=false;surface.enabled=false;Debug.Log("FINAL_EVENT collapsed");}
    }
    public void ResetState(){Triggered=false;Collapsed=false;collapseAt=float.PositiveInfinity;if(visual)visual.enabled=true;if(surface)surface.enabled=true;}
}

