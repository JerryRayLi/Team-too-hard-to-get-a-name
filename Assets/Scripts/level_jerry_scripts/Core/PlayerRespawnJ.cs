using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerRespawnJ : MonoBehaviour
{
    public static int DeathCount { get; private set; }

    public Transform respawnPoint;

    [Header("Optional Respawn Effects")]
    public ParticleSystem deathParticles;
    public ParticleSystem respawnParticles;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private CorpseReleaseAbility corpseReleaseAbility;
    private SpriteRenderer[] playerRenderers;
    private bool[] rendererVisibility;
    private RigidbodyConstraints2D originalConstraints;
    private bool controllerWasEnabled;
    private bool abilityWasEnabled;
    private Vector3 initialPosition;
    private bool isRespawning;

    public bool IsRespawning => isRespawning;
    public bool IsDying { get; private set; }
    public float RespawnDuration => GetParticleEffectDuration(respawnParticles);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        corpseReleaseAbility = GetComponent<CorpseReleaseAbility>();
        playerRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        rendererVisibility = new bool[playerRenderers.Length];
        initialPosition = transform.position;

        if (deathParticles == null)
            deathParticles = FindChildParticleSystem("DeathParticle");
        if (respawnParticles == null)
            respawnParticles = FindChildParticleSystem("RespawnParticle");

        // Jerry's existing scenes have no particle children. Keep the shared
        // player prefab and other levels untouched by loading a local copy.
        if (deathParticles == null || respawnParticles == null)
        {
            GameObject effectsPrefab = Resources.Load<GameObject>("JerryRespawnEffects");
            if (effectsPrefab != null)
            {
                GameObject effects = Instantiate(effectsPrefab, transform, false);
                foreach (ParticleSystem effect in effects.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (effect.name == "DeathParticle" && deathParticles == null)
                        deathParticles = effect;
                    else if (effect.name == "RespawnParticle" && respawnParticles == null)
                        respawnParticles = effect;
                }
            }
        }

        StopEffect(deathParticles);
        StopEffect(respawnParticles);
    }

    private void Start()
    {
        DeathCount = 0;
        if (!isRespawning && respawnParticles != null)
        {
            PlayRespawnEffect();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            RestartLevel();
    }

    public void Respawn()
    {
        if (isRespawning)
            return;

        BeginRespawn();
        IsDying = true;
        DeathCount++;
        StartCoroutine(PlayDeathEffectAndRespawn());
    }

    private IEnumerator PlayDeathEffectAndRespawn()
    {
        if (deathParticles != null)
        {
            StopEffect(deathParticles);
            deathParticles.Play(true);
            yield return new WaitForSeconds(GetParticleEffectDuration(deathParticles));
            StopEffect(deathParticles);
        }

        // Preserve Jerry's checkpoint respawn and the corpses left in the level.
        Vector3 spawnPosition = respawnPoint != null ? respawnPoint.position : initialPosition;
        transform.position = spawnPosition;
        if (rb != null)
            rb.position = spawnPosition;
        IsDying = false;
        PlayRespawnEffect();
    }

    private void PlayRespawnEffect()
    {
        // Respawning at a checkpoint is immediate. The particles are visual
        // only and must not suspend gravity or swallow the first movement.
        RestorePlayer();
        if (respawnParticles != null)
        {
            StopEffect(respawnParticles);
            respawnParticles.Play(true);
        }
    }

    // Also supports an external reset after the caller moves the player.
    public void ResetAndPlayRespawn()
    {
        StopAllCoroutines();
        StopEffect(deathParticles);
        StopEffect(respawnParticles);
        RestorePlayer();
        PlayRespawnEffect();
    }

    private void BeginRespawn()
    {
        StopEffect(respawnParticles);
        isRespawning = true;
        if (playerController != null)
        {
            controllerWasEnabled = playerController.enabled;
            playerController.enabled = false;
        }
        if (corpseReleaseAbility != null)
        {
            abilityWasEnabled = corpseReleaseAbility.enabled;
            corpseReleaseAbility.enabled = false;
        }
        if (rb != null)
        {
            originalConstraints = rb.constraints;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null)
                continue;
            rendererVisibility[i] = playerRenderers[i].enabled;
            playerRenderers[i].enabled = false;
        }
    }

    private void RestorePlayer()
    {
        if (!isRespawning)
            return;

        if (playerController != null)
            playerController.enabled = controllerWasEnabled;
        if (corpseReleaseAbility != null)
            corpseReleaseAbility.enabled = abilityWasEnabled;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = originalConstraints;
            rb.WakeUp();
        }
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
                playerRenderers[i].enabled = rendererVisibility[i];
        }
        IsDying = false;
        isRespawning = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopEffect(deathParticles);
        StopEffect(respawnParticles);
        RestorePlayer();
    }

    private ParticleSystem FindChildParticleSystem(string childName)
    {
        foreach (ParticleSystem effect in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (effect.name == childName)
                return effect;
        }
        return null;
    }

    private static void StopEffect(ParticleSystem effect)
    {
        if (effect != null)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static float GetParticleEffectDuration(ParticleSystem effect)
    {
        if (effect == null)
            return 0f;

        float duration = 0.05f;
        foreach (ParticleSystem child in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = child.main;
            duration = Mathf.Max(duration,
                (main.startDelay.constantMax + main.duration + main.startLifetime.constantMax)
                / Mathf.Max(0.01f, main.simulationSpeed));
        }
        return duration;
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }
}
