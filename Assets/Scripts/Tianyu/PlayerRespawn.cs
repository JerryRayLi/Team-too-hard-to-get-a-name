// Adapted from xlin3364-code/game_levelup@33df47b; original and adaptation notes in Review-Tianyu-Rebuild/Tianyu-33df47b.
namespace HaoyuTianyu
{
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

    private FinalLevelPlayerController playerController;
    private Rigidbody2D playerBody;
    private RigidbodyConstraints2D originalConstraints;
    private SpriteRenderer[] playerRenderers;
    private bool isRespawning;
    public bool IsRespawning => isRespawning;
    public bool IsDying { get; private set; }
    public float RespawnDuration => respawnParticles ? GetParticleEffectDuration(respawnParticles) : 0;
    public void ResetAndPlayRespawn()
    {
        StopAllCoroutines(); IsDying = false; isRespawning = false;
        if (deathParticles) deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (respawnParticles) respawnParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetPlayerVisible(true); SetPlayerControlEnabled(true);
        if (respawnParticles) StartCoroutine(PlayRespawnEffect());
    }

    private void Awake()
    {
        playerController = GetComponent<FinalLevelPlayerController>();
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

    public void Respawn()
    {
        if (isRespawning)
        {
            return;
        }

        DeathCount++;
        StartCoroutine(PlayDeathEffectAndReload());
    }

    private IEnumerator PlayDeathEffectAndReload()
    {
        isRespawning = true;
        IsDying = true;
        SetPlayerControlEnabled(false);
        SetPlayerVisible(false);

        if (deathParticles == null)
        {
            ReloadCurrentScene();
            yield break;
        }

        deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        deathParticles.Play(true);
        Debug.Log("TIANYU_EVENT death_effect");
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
        Debug.Log("TIANYU_EVENT respawn_effect");
        yield return new WaitForSeconds(GetParticleEffectDuration(respawnParticles));

        SetPlayerVisible(true);
        SetPlayerControlEnabled(true);
        isRespawning = false;
        Debug.Log("TIANYU_EVENT control_restored");
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

    private void ReloadCurrentScene()
    {
        FinalLevelManager.Instance.ResetRun();
    }

    public static void ResetDeathCount()
    {
        DeathCount = 0;
    }
}

}

