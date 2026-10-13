using UnityEngine;
[RequireComponent(typeof(Collider2D),typeof(SpriteRenderer))]
public sealed class FinalChaser : MonoBehaviour
{
    public float speed=3.2f, activationX=2f;
    public bool Active { get; private set; }
    Vector3 startPosition;
    Collider2D shape;
    SpriteRenderer visual;
    void Awake(){startPosition=transform.position;shape=GetComponent<Collider2D>();visual=GetComponent<SpriteRenderer>();ResetState();}
    void FixedUpdate()
    {
        var m=FinalLevelManager.Instance;
        if(!m || !m.CanMove)return;
        if(!Active && m.player.transform.position.x>=activationX){Active=true;SetVisible(true);Debug.Log("FINAL_EVENT chase_started");}
        if(Active)transform.position+=Vector3.right*(speed*Time.fixedDeltaTime);
    }
    void OnTriggerEnter2D(Collider2D c){Hit(c);}
    void OnTriggerStay2D(Collider2D c){Hit(c);}
    void Hit(Collider2D c){if(Active && c.GetComponent<FinalLevelPlayerController>())FinalLevelManager.Instance.Die("chaser");}
    void SetVisible(bool value){shape.enabled=value;visual.enabled=value;}
    public void ResetState(){Active=false;transform.position=startPosition;if(shape && visual)SetVisible(false);}
}

