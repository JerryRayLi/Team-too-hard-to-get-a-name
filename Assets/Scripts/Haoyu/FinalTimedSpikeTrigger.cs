using UnityEngine;
// Raises its spikes a short time after the player first lands inside the zone; the floor shakes in between.
[RequireComponent(typeof(Collider2D))]
public sealed class FinalTimedSpikeTrigger : MonoBehaviour
{
    public HaoyuTianyu.SpikeTrap[] spikeTraps = new HaoyuTianyu.SpikeTrap[0];
    public Transform warningShake;
    [Min(0)] public float delaySeconds = .3f;
    public float shakeAmount = .05f;
    float firesAt = float.PositiveInfinity;
    bool fired;
    Vector3 shakeRest;
    Collider2D zone;
    void Awake(){zone=GetComponent<Collider2D>();if(warningShake)shakeRest=warningShake.localPosition;}
    void FixedUpdate()
    {
        var m=FinalLevelManager.Instance;
        if(fired || !m || !m.AcceptContact)return;
        var p=m.player;
        if(float.IsPositiveInfinity(firesAt) && p.Grounded && zone.bounds.Intersects(p.GetComponent<Collider2D>().bounds))
        {
            firesAt=Time.time+delaySeconds;
            Debug.Log($"FINAL_EVENT timed_spikes_armed x={p.transform.position.x:F3}");
        }
        if(float.IsPositiveInfinity(firesAt))return;
        if(Time.time>=firesAt)
        {
            fired=true;StopShake();
            foreach(var trap in spikeTraps) if(trap) trap.Extend();
            Debug.Log("FINAL_EVENT timed_spikes");
        }
        else if(warningShake)
            warningShake.localPosition=shakeRest+new Vector3(Mathf.Sin(Time.time*70f)*shakeAmount,0,0);
    }
    void StopShake(){if(warningShake)warningShake.localPosition=shakeRest;}
    public void ResetState(){firesAt=float.PositiveInfinity;fired=false;StopShake();}
}
