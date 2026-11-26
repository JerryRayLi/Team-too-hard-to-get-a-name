using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CorpseReleaseAbility : MonoBehaviour
{
    public float releaseRange = 2f;
    private Corpse[] observed = new Corpse[0];
    private readonly List<JerryCorpseSelection> selections = new List<JerryCorpseSelection>();
    private float nextScan;

    private void Update()
    {
        RefreshSelection();
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ReleaseCorpsesInRange();
    }

    private bool CanUse()
    {
        var player = GetComponent<PlayerRespawnJ>();
        return isActiveAndEnabled && Time.timeScale > 0f && releaseRange > 0f &&
            (player == null || !player.IsRespawning);
    }
    private bool IsReleaseTarget(Corpse corpse)
    {
        if (corpse == null || corpse.gameObject.scene != gameObject.scene || !corpse.isActiveAndEnabled)
            return false;
        var body = corpse.GetComponent<Rigidbody2D>();
        return body != null && body.bodyType == RigidbodyType2D.Static &&
            Vector2.Distance(transform.position,corpse.transform.position) < releaseRange;
    }
    public void RefreshSelection(bool forceScan = false)
    {
        bool usable = CanUse();
        if (usable && (forceScan || Time.unscaledTime >= nextScan))
        {
            observed = FindObjectsByType<Corpse>(FindObjectsSortMode.None);
            nextScan = Time.unscaledTime + .15f;
        }
        if (usable)
            foreach (var corpse in observed)
            {
                if (!IsReleaseTarget(corpse)) continue;
                var selection = corpse.GetComponent<JerryCorpseSelection>();
                if (selection == null) selection = corpse.gameObject.AddComponent<JerryCorpseSelection>();
                if (!selections.Contains(selection)) selections.Add(selection);
            }
        for (int i = selections.Count-1; i >= 0; i--)
        {
            var selection = selections[i];
            if (selection == null) { selections.RemoveAt(i); continue; }
            selection.SetSelected(usable && IsReleaseTarget(selection.GetComponent<Corpse>()));
        }
    }
    private void OnDisable()
    {
        foreach (var selection in selections) if (selection != null) selection.SetSelected(false);
    }

    public int ReleaseCorpsesInRange()
    {
        if (!CanUse()) return 0;

        int released = 0;
        foreach (var corpse in FindObjectsByType<Corpse>(FindObjectsSortMode.None))
        {
            if (!IsReleaseTarget(corpse)) continue;
            corpse.Release();
            var flash = corpse.GetComponent<JerryCorpseReleaseFlash>();
            if (flash == null) flash = corpse.gameObject.AddComponent<JerryCorpseReleaseFlash>();
            flash.Play();
            released++;
        }
        GetComponent<CorpseReleaseRangeIndicator>()?.PulseRelease(released);
        RefreshSelection();
        return released;
    }
}
