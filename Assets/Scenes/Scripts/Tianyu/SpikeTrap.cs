// Adapted from xlin3364-code/game_levelup@33df47b; original and adaptation notes in Review-Tianyu-Rebuild/Tianyu-33df47b.
namespace HaoyuTianyu
{
using System.Collections;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    public enum HorizontalDirection
    {
        Left,
        Right
    }

    [Header("Movement")]
    [Tooltip("Place the spike at its fully raised position. This offset defines where it hides.")]
    public Vector2 hiddenOffset = new Vector2(0f, -1f);

    [Min(0.01f)]
    public float moveDuration = 0.25f;

    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Starting State")]
    public bool startHidden = true;

    [Header("Optional Automatic Retract")]
    public bool retractAutomatically;

    [Min(0f)]
    public float exposedDuration = 1.5f;

    [Header("Optional Upward Flight")]
    [Min(0f)]
    public float upwardFlightDistance = 4f;

    [Min(0.01f)]
    [Tooltip("Vertical flight speed in Unity units per second.")]
    public float upwardFlightSpeed = 8f;

    public AnimationCurve upwardFlightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public bool returnAfterUpwardFlight;

    [Min(0f)]
    public float upwardFlightReturnDelay = 1f;

    [Header("Optional Horizontal Movement")]
    public HorizontalDirection horizontalDirection = HorizontalDirection.Right;

    [Min(0f)]
    public float horizontalMoveDistance = 3f;

    [Min(0.01f)]
    [Tooltip("Horizontal movement speed in Unity units per second.")]
    public float horizontalMoveSpeed = 3f;

    public AnimationCurve horizontalMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Keep moving between the two horizontal endpoints after being triggered.")]
    public bool moveHorizontallyBackAndForth = true;

    [Min(0f)]
    public float horizontalEndPause = 0.2f;

    private Vector3 extendedLocalPosition;
    private Vector3 hiddenLocalPosition;
    private Coroutine movementRoutine;

    private void Awake()
    {
        extendedLocalPosition = transform.localPosition;
        hiddenLocalPosition = extendedLocalPosition + (Vector3)hiddenOffset;

        transform.localPosition = startHidden ? hiddenLocalPosition : extendedLocalPosition;
    }

    public bool IsExtended => Vector3.Distance(transform.localPosition, extendedLocalPosition) < .01f;
    public bool IsHidden => Vector3.Distance(transform.localPosition, hiddenLocalPosition) < .01f;
    public void ResetState()
    {
        StopAllCoroutines(); movementRoutine = null;
        transform.localPosition = startHidden ? hiddenLocalPosition : extendedLocalPosition;
    }

    public void Extend()
    {
        StartMovement(extendedLocalPosition, retractAutomatically);
    }

    public void Retract()
    {
        StartMovement(hiddenLocalPosition, false);
    }

    public void Toggle()
    {
        float distanceToExtended = Vector3.SqrMagnitude(transform.localPosition - extendedLocalPosition);
        float distanceToHidden = Vector3.SqrMagnitude(transform.localPosition - hiddenLocalPosition);

        if (distanceToExtended < distanceToHidden)
        {
            Retract();
        }
        else
        {
            Extend();
        }
    }

    public void FlyUpward()
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
        }

        movementRoutine = StartCoroutine(FlyUpwardRoutine());
    }

    public void MoveHorizontally()
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
        }

        movementRoutine = StartCoroutine(HorizontalMovementRoutine());
    }

    private void StartMovement(Vector3 targetPosition, bool retractAfterDelay)
    {
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
        }

        movementRoutine = StartCoroutine(MoveTo(targetPosition, retractAfterDelay));
    }

    private IEnumerator MoveTo(Vector3 targetPosition, bool retractAfterDelay)
    {
        Vector3 startPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / moveDuration);
            transform.localPosition = Vector3.LerpUnclamped(
                startPosition,
                targetPosition,
                movementCurve.Evaluate(progress)
            );

            yield return null;
        }

        transform.localPosition = targetPosition;

        if (retractAfterDelay)
        {
            yield return new WaitForSeconds(exposedDuration);
            movementRoutine = StartCoroutine(MoveTo(hiddenLocalPosition, false));
            yield break;
        }

        movementRoutine = null;
    }

    private IEnumerator FlyUpwardRoutine()
    {
        Vector3 flightStartPosition = transform.localPosition;
        Vector3 flightEndPosition = flightStartPosition + Vector3.up * upwardFlightDistance;
        float flightDuration = Mathf.Abs(upwardFlightDistance) /
            Mathf.Max(0.01f, upwardFlightSpeed);

        yield return MoveOverTime(
            flightStartPosition,
            flightEndPosition,
            flightDuration,
            upwardFlightCurve
        );

        if (returnAfterUpwardFlight)
        {
            yield return new WaitForSeconds(upwardFlightReturnDelay);
            yield return MoveOverTime(
                flightEndPosition,
                flightStartPosition,
                flightDuration,
                upwardFlightCurve
            );
        }

        movementRoutine = null;
    }

    private IEnumerator HorizontalMovementRoutine()
    {
        Vector3 startPosition = transform.localPosition;
        float direction = horizontalDirection == HorizontalDirection.Left ? -1f : 1f;
        Vector3 endPosition = startPosition + Vector3.right * direction * horizontalMoveDistance;
        float travelDuration = Mathf.Abs(horizontalMoveDistance) /
            Mathf.Max(0.01f, horizontalMoveSpeed);

        do
        {
            yield return MoveOverTime(
                startPosition,
                endPosition,
                travelDuration,
                horizontalMoveCurve
            );

            if (!moveHorizontallyBackAndForth)
            {
                break;
            }

            if (horizontalEndPause > 0f)
            {
                yield return new WaitForSeconds(horizontalEndPause);
            }

            yield return MoveOverTime(
                endPosition,
                startPosition,
                travelDuration,
                horizontalMoveCurve
            );

            if (horizontalEndPause > 0f)
            {
                yield return new WaitForSeconds(horizontalEndPause);
            }
        }
        while (moveHorizontallyBackAndForth);

        movementRoutine = null;
    }

    private IEnumerator MoveOverTime(
        Vector3 startPosition,
        Vector3 targetPosition,
        float duration,
        AnimationCurve curve)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.LerpUnclamped(
                startPosition,
                targetPosition,
                curve.Evaluate(progress)
            );
            yield return null;
        }

        transform.localPosition = targetPosition;
    }

}

}

