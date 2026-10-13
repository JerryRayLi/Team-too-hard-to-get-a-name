using UnityEngine;

public class SpikeTrapTrigger : MonoBehaviour
{
    public enum TriggerAction
    {
        Extend,
        Retract,
        Toggle,
        FlyUpward,
        MoveHorizontally
    }

    [Tooltip("One trigger zone can control one or several spike traps.")]
    public SpikeTrap[] spikeTraps = new SpikeTrap[0];

    public TriggerAction action = TriggerAction.Extend;
    [Tooltip("Turn this off if the same trigger zone should activate again after the player leaves and re-enters it.")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || (triggerOnlyOnce && hasTriggered))
        {
            return;
        }

        hasTriggered = true;

        foreach (SpikeTrap spikeTrap in spikeTraps)
        {
            if (spikeTrap == null)
            {
                continue;
            }

            switch (action)
            {
                case TriggerAction.Extend:
                    spikeTrap.Extend();
                    break;
                case TriggerAction.Retract:
                    spikeTrap.Retract();
                    break;
                case TriggerAction.Toggle:
                    spikeTrap.Toggle();
                    break;
                case TriggerAction.FlyUpward:
                    spikeTrap.FlyUpward();
                    break;
                case TriggerAction.MoveHorizontally:
                    spikeTrap.MoveHorizontally();
                    break;
            }
        }
    }
}
