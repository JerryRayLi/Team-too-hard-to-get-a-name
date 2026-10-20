using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerRespawn : MonoBehaviour
{
    public static int DeathCount { get; private set; }

    [Header("Optional Respawn Effects")]
    public ParticleSystem deathParticles;
    public ParticleSystem respawnParticles;

    private PlayerController playerController;
    private Rigidbody2D playerBody;
    private RigidbodyConstraints2D originalConstraints;
    private SpriteRenderer[] playerRenderers;
    private bool isRespawning;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerBody = GetComponent<Rigidbody2D>();
        playerRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (playerBody != null)
        {
            originalConstraints = playerBody.constraints;
        }

        if (deathParticles == null)
        {
            deathParticles = FindChildParticleSystem("DeathParticle");
        }

        if (respawnParticles == null)
        {
            respawnParticles = FindChildParticleSystem("RespawnParticle");
        }
    }

    private void Start()
    {
        if (respawnParticles != null)
        {
            StartCoroutine(PlayRespawnEffect());
        }
    }

    private void Update()
    {
        if (!isRespawning &&
            Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        if (isRespawning)
        {
            return;
        }

        DeathCount++;
        PlaySessionStats.RecordDeath();
        StartCoroutine(PlayDeathEffectAndReload());
    }

    private IEnumerator PlayDeathEffectAndReload()
    {
        isRespawning = true;
        SetPlayerControlEnabled(false);
        SetPlayerVisible(false);

        if (deathParticles == null)
        {
            ReloadCurrentScene();
            yield break;
        }

        deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        deathParticles.Play(true);
        yield return new WaitForSeconds(GetParticleEffectDuration(deathParticles));

        ReloadCurrentScene();
    }

    private IEnumerator PlayRespawnEffect()
    {
        isRespawning = true;
        SetPlayerControlEnabled(false);
        SetPlayerVisible(false);

        respawnParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        respawnParticles.Play(true);
        yield return new WaitForSeconds(GetParticleEffectDuration(respawnParticles));

        SetPlayerVisible(true);
        SetPlayerControlEnabled(true);
        isRespawning = false;
    }

    private void SetPlayerControlEnabled(bool isEnabled)
    {
        if (playerController != null)
        {
            playerController.enabled = isEnabled;
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.constraints = isEnabled
                ? originalConstraints
                : RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void SetPlayerVisible(bool isVisible)
    {
        foreach (SpriteRenderer playerRenderer in playerRenderers)
        {
            playerRenderer.enabled = isVisible;
        }
    }

    private ParticleSystem FindChildParticleSystem(string childName)
    {
        foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particleSystem.name == childName)
            {
                return particleSystem;
            }
        }

        return null;
    }

    private static float GetParticleEffectDuration(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        return Mathf.Max(
            0.05f,
            main.startDelay.constantMax + main.startLifetime.constantMax
        );
    }

    private static void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }
}
