using UnityEngine;

// 在门移动、颜色切换完成后处理墙体。
[DefaultExecutionOrder(200)]
public class DoorWallTrap : MonoBehaviour
{
    public BoxCollider2D doorCollider;
    public BoxCollider2D barrier;

    public float warningTime = 2f;
    public float disappearTime = 2f;
    public float flashInterval = 0.12f;

    private enum State
    {
        Ready,
        Warning,
        Hidden
    }

    private State state;
    private float timer;

    private SpriteRenderer[] wallSprites;
    private bool[] originalVisibility;
    private bool originalColliderEnabled;
    private bool initialized;

    private void Start()
    {
        if (barrier == null || doorCollider == null)
        {
            Debug.LogError(
                "DoorWallTrap: 请设置 Door Collider 和 Barrier。",
                this);

            enabled = false;
            return;
        }

        wallSprites =
            barrier.GetComponentsInChildren<SpriteRenderer>(true);

        originalVisibility = new bool[wallSprites.Length];

        for (int i = 0; i < wallSprites.Length; i++)
            originalVisibility[i] = wallSprites[i].enabled;

        originalColliderEnabled = barrier.enabled;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized || Time.deltaTime <= 0f)
            return;

        if (state == State.Ready)
        {
            if (DoorIsBlocked())
            {
                state = State.Warning;
                timer = 0f;
            }

            return;
        }

        timer += Time.deltaTime;

        if (state == State.Warning)
        {
            // 玩家切色让墙消失，也会取消这次倒计时。
            if (!barrier.gameObject.activeInHierarchy ||
                !barrier.enabled)
            {
                RestoreWall();
                state = State.Ready;
                return;
            }

            float interval = Mathf.Max(0.02f, flashInterval);

            bool visible =
                Mathf.FloorToInt(timer / interval) % 2 == 0;

            SetWallVisible(visible);

            if (timer >= warningTime)
            {
                state = State.Hidden;
                timer = 0f;

                barrier.enabled = false;
                SetWallVisible(false);
            }
        }
        else if (state == State.Hidden)
        {
            // 即使玩家切色，机关生效期间也保持无碰撞。
            barrier.enabled = false;
            SetWallVisible(false);

            if (timer >= disappearTime)
            {
                RestoreWall();
                state = State.Ready;
            }
        }
    }

    private bool DoorIsBlocked()
    {
        if (!doorCollider.enabled ||
            !barrier.enabled ||
            barrier.isTrigger ||
            !barrier.gameObject.activeInHierarchy)
        {
            return false;
        }

        Bounds door = doorCollider.bounds;
        Bounds wall = barrier.bounds;

        float gap = wall.min.x - door.max.x;

        bool overlapsHeight =
            door.max.y > wall.min.y &&
            door.min.y < wall.max.y;

        // 对应 WatcherDoor 留出的 0.02 单位间距。
        return overlapsHeight && gap >= -0.01f && gap <= 0.06f;
    }

    private void SetWallVisible(bool visible)
    {
        for (int i = 0; i < wallSprites.Length; i++)
        {
            if (wallSprites[i] != null)
            {
                wallSprites[i].enabled =
                    visible && originalVisibility[i];
            }
        }
    }

    private void RestoreWall()
    {
        if (!initialized)
            return;

        if (barrier != null)
            barrier.enabled = originalColliderEnabled;

        SetWallVisible(true);
    }

    private void OnDisable()
    {
        RestoreWall();
    }
}