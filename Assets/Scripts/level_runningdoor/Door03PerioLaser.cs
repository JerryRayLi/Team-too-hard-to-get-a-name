using UnityEngine;

public class Door03PeriodicLaser : MonoBehaviour
{
    public SpriteRenderer beam;
    public BoxCollider2D damageCollider;

    [Min(0.1f)] public float offTime = 2.4f;
    [Min(0.1f)] public float warningTime = 0.6f;
    [Min(0.1f)] public float activeTime = 1.2f;

    private enum Phase { Off, Warning, Active }
    private Phase phase;
    private float timer;

    private void Awake()
    {
        if (damageCollider != null)
        {
            damageCollider.isTrigger = true;
            damageCollider.enabled = false;
        }
        if (beam != null)
            beam.enabled = false;
    }

    private void Start()
    {
        if (beam == null || damageCollider == null)
        {
            Debug.LogError("Laser: assign Beam and Damage Collider.", this);
            enabled = false;
            return;
        }
        Enter(Phase.Off);
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f)
            return;

        timer += Time.deltaTime;
        if (phase == Phase.Off && timer >= offTime)
            Enter(Phase.Warning);
        else if (phase == Phase.Warning && timer >= warningTime)
            Enter(Phase.Active);
        else if (phase == Phase.Active && timer >= activeTime)
            Enter(Phase.Off);

        if (phase == Phase.Warning)
        {
            float alpha = Mathf.Lerp(0.2f, 0.65f,
                Mathf.PingPong(timer * 6f, 1f));
            beam.color = new Color(1f, 0.85f, 0.1f, alpha);
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        timer = 0f;
        damageCollider.enabled = next == Phase.Active;
        beam.enabled = next != Phase.Off;
        if (next == Phase.Active)
            beam.color = new Color(1f, 0.1f, 0.15f, 1f);
    }

    private void OnDisable()
    {
        if (damageCollider != null)
            damageCollider.enabled = false;
        if (beam != null)
            beam.enabled = false;
        phase = Phase.Off;
        timer = 0f;
    }
}
