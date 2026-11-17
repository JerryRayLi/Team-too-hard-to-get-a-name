using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Door03FallingSpike : MonoBehaviour
{
    public Transform player;
    public Transform fallingSpike;
    public SpriteRenderer warningMarker;

    [Min(0.1f)] public float warningTime = 0.9f;
    [Min(0.1f)] public float fallSpeed = 10f;
    public float landingY = 3.7f;
    [Min(0.1f)] public float stayTime = 0.5f;

    private enum Phase { Ready, Warning, Falling, Landed, Finished }
    private Phase phase;
    private float timer;
    private BoxCollider2D triggerArea;
    private Collider2D spikeCollider;
    private Collider2D playerCollider;

    private void Start()
    {
        triggerArea = GetComponent<BoxCollider2D>();
        triggerArea.isTrigger = true;

        if (player == null || fallingSpike == null ||
            warningMarker == null)
        {
            Debug.LogError("Falling spike: assign all three references.", this);
            enabled = false;
            return;
        }

        spikeCollider = fallingSpike.GetComponent<Collider2D>();
        playerCollider = player.GetComponentInChildren<Collider2D>();
        if (spikeCollider == null)
        {
            Debug.LogError("Falling spike needs a Collider2D.", this);
            enabled = false;
            return;
        }

        spikeCollider.isTrigger = true;
        spikeCollider.enabled = false;
        warningMarker.enabled = false;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || phase == Phase.Finished)
            return;

        if (phase == Phase.Ready)
        {
            Vector2 target = playerCollider != null
                ? (Vector2)playerCollider.bounds.center
                : (Vector2)player.position;

            if (triggerArea.enabled && triggerArea.OverlapPoint(target))
            {
                phase = Phase.Warning;
                timer = 0f;
                warningMarker.enabled = true;
            }
            return;
        }

        timer += Time.deltaTime;
        if (phase == Phase.Warning)
        {
            float alpha = Mathf.Lerp(0.3f, 1f,
                Mathf.PingPong(timer * 7f, 1f));
            warningMarker.color = new Color(1f, 0.15f, 0.1f, alpha);
            if (timer >= warningTime)
            {
                phase = Phase.Falling;
                spikeCollider.enabled = true;
            }
        }
        else if (phase == Phase.Falling)
        {
            Vector3 position = fallingSpike.position;
            position.y = Mathf.MoveTowards(position.y,
                landingY, fallSpeed * Time.deltaTime);
            fallingSpike.position = position;
            if (Mathf.Approximately(position.y, landingY))
            {
                phase = Phase.Landed;
                timer = 0f;
                warningMarker.enabled = false;
            }
        }
        else if (phase == Phase.Landed && timer >= stayTime)
        {
            spikeCollider.enabled = false;
            fallingSpike.gameObject.SetActive(false);
            phase = Phase.Finished;
        }
    }
}
