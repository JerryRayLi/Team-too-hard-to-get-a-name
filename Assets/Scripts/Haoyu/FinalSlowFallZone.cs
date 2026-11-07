using UnityEngine;
// Inside this trigger the player falls slowly; jumps keep their normal height.
[RequireComponent(typeof(Collider2D))]
public sealed class FinalSlowFallZone : MonoBehaviour
{
    [Range(.01f, 1f)] public float gravityMultiplier = .15f;
    [Min(.1f)] public float maxFallSpeed = 1.5f;
    void OnTriggerEnter2D(Collider2D c){Mark(c);}
    void OnTriggerStay2D(Collider2D c){Mark(c);}
    void Mark(Collider2D c){var p=c.GetComponent<FinalLevelPlayerController>();if(p)p.MarkSlowFall(gravityMultiplier,maxFallSpeed);}
}
