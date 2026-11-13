using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
public class Door03LiftRoute : MonoBehaviour
{
    public Transform player;
    public Transform liftPlate;
    public ColorLayerManager colorManager;
    public BoxCollider2D captureArea;
    public SpriteRenderer capturePad;

    [SerializeField, Min(0f)] private float noticeDistance = 3.5f;
    [SerializeField, Min(0.1f)] private float runSpeed = 6f;
    [SerializeField, Min(0.1f)] private float liftSpeed = 3.8f;
    [SerializeField] private float gapStartX = 1.2f;
    [SerializeField] private float gapEndX = 3.3f;
    [SerializeField] private float liftX = 12.5f;
    [SerializeField] private float upperDoorY = 4.2f;

    [Header("Upper route")]
    [SerializeField, Min(0.1f)] private float upperSpeed = 4f;
    [SerializeField] private float firstTurnX = 9f;
    [SerializeField] private float upperLeftLimit = 4.6f;
    [SerializeField] private float upperRightLimit = 13f;
    [SerializeField] private float upperGapStartX = 7.5f;
    [SerializeField] private float upperGapEndX = 8.8f;

    private enum Phase { Waiting, Running, Lifting, LeavingLift, Herding, Captured }
    private Phase phase;
    private float lowerDoorY;
    private Vector3 lowerPlatePosition;
    private BoxCollider2D exitTrigger;
    private DoorCameraVision vision;
    private float upperDirection = -1f;

    private void Start()
    {
        lowerDoorY = transform.position.y;
        if (liftPlate != null)
            lowerPlatePosition = liftPlate.position;

        exitTrigger = GetComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;
        exitTrigger.enabled = false;
        vision = GetComponentInChildren<DoorCameraVision>();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || phase == Phase.Captured)
            return;

        // The pad becomes available once the door has left the elevator.
        bool trapActive = phase == Phase.Herding &&
            colorManager != null && colorManager.IsOrangeBackground;
        if (capturePad != null)
            capturePad.color = trapActive ? Color.yellow : Color.gray;

        if (phase == Phase.Waiting)
        {
            if (player == null)
                return;

            Vector3 difference = transform.position - player.position;
            if (difference.x <= 0f || difference.x > noticeDistance ||
                Mathf.Abs(difference.y) > 1.4f)
                return;

            phase = Phase.Running;
        }

        Vector3 position = transform.position;

        if (phase == Phase.Running)
        {
            position.x = Mathf.MoveTowards(
                position.x, liftX, runSpeed * Time.deltaTime);

            float jump = Mathf.InverseLerp(gapStartX, gapEndX, position.x);
            position.y = lowerDoorY + Mathf.Sin(jump * Mathf.PI) * 0.9f;
            if (position.x == liftX)
                phase = Phase.Lifting;
        }
        else if (phase == Phase.Lifting)
        {
            position.y = Mathf.MoveTowards(
                position.y, upperDoorY, liftSpeed * Time.deltaTime);

            if (liftPlate != null)
                liftPlate.position = lowerPlatePosition +
                    Vector3.up * (position.y - lowerDoorY);

            if (position.y == upperDoorY)
                phase = Phase.LeavingLift;
        }
        else if (phase == Phase.LeavingLift)
        {
            position.x = Mathf.MoveTowards(
                position.x, firstTurnX, upperSpeed * Time.deltaTime);

            if (position.x == firstTurnX)
                phase = Phase.Herding;
        }
        else if (phase == Phase.Herding)
        {
            if (player != null &&
                player.position.y >= upperDoorY - 0.75f &&
                player.position.y <= upperDoorY + 2.6f)
            {
                float awayFromPlayer = position.x - player.position.x;
                if (Mathf.Abs(awayFromPlayer) > 0.2f)
                    upperDirection = Mathf.Sign(awayFromPlayer);
            }
            else
            {
                // A player still on the lower floor cannot steer the upper door.
                upperDirection = -1f;
            }

            Vector3 previous = position;
            position.x = Mathf.Clamp(
                position.x + upperDirection * upperSpeed * Time.deltaTime,
                upperLeftLimit, upperRightLimit);
            float jump = Mathf.InverseLerp(
                upperGapStartX, upperGapEndX, position.x);
            position.y = upperDoorY + Mathf.Sin(jump * Mathf.PI) * 0.45f;

            if (trapActive && captureArea != null && captureArea.isActiveAndEnabled)
            {
                Bounds zone = captureArea.bounds;
                // Include the traveled segment so a low frame rate cannot skip the pad.
                float sampleX = Mathf.Clamp(zone.center.x,
                    Mathf.Min(previous.x, position.x),
                    Mathf.Max(previous.x, position.x));
                Vector2 sample = new Vector2(sampleX, position.y);
                if (captureArea.OverlapPoint(sample))
                {
                    position.x = sampleX;
                    transform.position = position;
                    Capture();
                    return;
                }
            }
        }

        transform.position = position;
    }

    private void Capture()
    {
        phase = Phase.Captured;
        exitTrigger.enabled = true;
        if (capturePad != null)
            capturePad.color = Color.green;
        if (vision != null)
        {
            vision.enabled = false;
            if (vision.cameraEye != null)
                vision.cameraEye.color = Color.green;
        }
    }
}
