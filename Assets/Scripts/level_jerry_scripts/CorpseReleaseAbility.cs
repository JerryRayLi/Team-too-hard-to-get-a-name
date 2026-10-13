using UnityEngine;
using UnityEngine.InputSystem;

public class CorpseReleaseAbility : MonoBehaviour
{
    public float releaseRange = 2f;

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ReleaseNearestCorpse();
        }
    }

    private void ReleaseNearestCorpse()
    {
        Corpse[] corpses = FindObjectsByType<Corpse>(
            FindObjectsSortMode.None
        );

        Corpse nearest = null;
        float nearestDistance = releaseRange;

        foreach (Corpse corpse in corpses)
        {
            float distance = Vector2.Distance(
                transform.position,
                corpse.transform.position
            );

            if (distance < nearestDistance)
            {
                nearest = corpse;
                nearestDistance = distance;
            }
        }

        if (nearest != null)
        {
            nearest.Release();
        }
    }
}