using UnityEngine;

[RequireComponent(typeof(LevelExit))]
[RequireComponent(typeof(BoxCollider2D))]
public class FleeingExit : MonoBehaviour
{
    public Transform player;
    public ColorLayerManager colorManager;
    public BoxCollider2D blueBarrier;

    public float noticeDistance = 3f;
    public float fleeSpeed = 7.5f;
    public float escapeX = 10f;

    private BoxCollider2D exitTrigger;
    private bool fleeing;
    private bool caught;
    private bool escaped;

    private void Awake()
    {
        exitTrigger = GetComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;

        // 门被挡住之后，才允许进入。
        exitTrigger.enabled = false;
    }

    private void Update()
    {
        if (caught || escaped || player == null ||
            colorManager == null || blueBarrier == null)
        {
            return;
        }

        float distance = Vector2.Distance(
            player.position,
            transform.position
        );

        if (distance <= noticeDistance)
        {
            fleeing = true;
        }

        if (!fleeing)
        {
            return;
        }

        float nextX = transform.position.x
            + fleeSpeed * Time.deltaTime;

        bool barrierActive =
            colorManager.IsBlueActive &&
            blueBarrier.gameObject.activeInHierarchy &&
            blueBarrier.enabled;

        if (barrierActive)
        {
            float halfWidth = exitTrigger.size.x
                * Mathf.Abs(transform.lossyScale.x) * 0.5f;

            float centerOffset = exitTrigger.offset.x
                * transform.lossyScale.x;

            float stopX = blueBarrier.bounds.min.x
                - halfWidth - centerOffset - 0.05f;

            if (transform.position.x <= stopX && nextX >= stopX)
            {
                SetX(stopX);
                caught = true;
                exitTrigger.enabled = true;
                return;
            }
        }

        SetX(nextX);

        // 门逃走了，重新开始这一关。
        if (nextX >= escapeX)
        {
            escaped = true;

            PlayerRespawn respawn =
                player.GetComponent<PlayerRespawn>();

            if (respawn != null)
            {
                respawn.Respawn();
            }
        }
    }

    private void SetX(float x)
    {
        Vector3 position = transform.position;
        position.x = x;
        transform.position = position;
    }
}