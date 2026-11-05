using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DeathTauntManager : MonoBehaviour
{
    // Temporary layout preview. Set to false after meme positions are approved.
    private const bool PreviewMemesAlways = false;

    private readonly struct TauntSchedule
    {
        public readonly int First;
        public readonly int Second;
        public readonly int Strong;
        public readonly int RepeatInterval;

        public TauntSchedule(int first, int second, int strong, int repeatInterval)
        {
            First = first;
            Second = second;
            Strong = strong;
            RepeatInterval = repeatInterval;
        }
    }

    private static readonly string[] MildTaunts =
    {
        "Oh no. Looks like you still haven't learned.",
        "That looked intentional. Was it?",
        "Have you considered surviving?",
        "Excellent. Now try the part where you stay alive.",
        "The respawn point missed you.",
        "You almost learned something that time.",
        "The good news: respawning still works.",
        "The laws of physics remain undefeated.",
        "That jump had confidence, ambition, and no chance.",
        "Don't worry. The trap believed in you for a moment."
    };

    private static readonly string[] MediumTaunts =
    {
        "You aren't still waiting for a hint, are you?",
        "The level remembers. Apparently, you don't.",
        "Bold strategy. Same result.",
        "The trap didn't move. You just forgot it existed.",
        "Another flawless return to the beginning.",
        "You found the trap again. Impressive consistency.",
        "The level is starting to recognize you.",
        "Same plan again? Surely it will work this time.",
        "The trap sends its regards.",
        "Maybe the next death will reveal the answer."
    };

    private static readonly string[] StrongTaunts =
    {
        "Grow up, kid. Remember those traps!",
        "Your confidence is doing most of the work here.",
        "At least your death count is making progress.",
        "That obstacle has defeated you emotionally now.",
        "The level appreciates your generous donation of lives.",
        "Your keyboard has requested a new player.",
        "Somewhere, a checkpoint is losing hope.",
        "You have mastered the art of arriving nowhere.",
        "The level designer would like to thank you personally.",
        "Was that a strategy or a cry for help?"
    };

    private static readonly HashSet<string> PlayableScenes = new()
    {
        "Level01", "Level02", "Level03", "Level04", "Level_TIanyu1",
        "level_j1", "level_j2", "level_j3", "Level_Haoyu", "Level_Haoyu_2",
        "Level_Running_Door_1", "Level_Yulin_v1.1"
    };

    private static readonly HashSet<string> InitiationScenes = new()
    {
        "Level01", "Level02", "Level03", "Level04"
    };

    private static readonly Dictionary<string, TauntSchedule> Schedules = new()
    {
        { "Level01", new TauntSchedule(3, 5, 8, 4) },
        { "Level02", new TauntSchedule(3, 5, 8, 4) },
        { "Level03", new TauntSchedule(3, 6, 10, 4) },
        { "Level04", new TauntSchedule(4, 7, 11, 4) },
        { "Level_Running_Door_1", new TauntSchedule(4, 7, 11, 4) },
        { "level_j1", new TauntSchedule(4, 8, 12, 5) },
        { "level_j2", new TauntSchedule(6, 11, 16, 5) },
        { "level_j3", new TauntSchedule(6, 11, 16, 5) },
        { "Level_Haoyu", new TauntSchedule(5, 9, 14, 5) },
        { "Level_Haoyu_2", new TauntSchedule(5, 9, 14, 5) },
        { "Level_Yulin_v1.1", new TauntSchedule(5, 10, 15, 5) },
        { "Level_TIanyu1", new TauntSchedule(8, 14, 20, 6) }
    };

    private static DeathTauntManager instance;

    private readonly HashSet<int> triggeredThresholds = new();
    private string trackedScene;
    private AudioSource audioSource;
    private AudioClip laughClip;
    private Texture2D confusedMeme;
    private Texture2D judgingMeme;
    private SpriteRenderer confusedMemeRenderer;
    private SpriteRenderer judgingMemeRenderer;
    private string currentFormalTaunt;
    private int lastTauntDeathCount = -1;
    private int lastLaughDeathCount = -1;
    private readonly HashSet<int> usedMildTaunts = new();
    private readonly HashSet<int> usedMediumTaunts = new();
    private readonly HashSet<int> usedStrongTaunts = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(DeathTauntManager));
        instance = managerObject.AddComponent<DeathTauntManager>();
        DontDestroyOnLoad(managerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.22f;
        laughClip = Resources.Load<AudioClip>("TauntLaugh");
        if (laughClip == null)
        {
            laughClip = CreateFallbackLaughClip();
        }
        confusedMeme = Resources.Load<Texture2D>("TauntMemeConfused");
        judgingMeme = Resources.Load<Texture2D>("TauntMemeJudging");
    }

    private void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != trackedScene)
        {
            trackedScene = sceneName;
            triggeredThresholds.Clear();
            usedMildTaunts.Clear();
            usedMediumTaunts.Clear();
            usedStrongTaunts.Clear();
            currentFormalTaunt = null;
            lastTauntDeathCount = -1;
            lastLaughDeathCount = -1;
            DestroyBackgroundMemes();
        }

        if (!PlayableScenes.Contains(sceneName))
        {
            return;
        }

        int deaths = PlaySessionStats.GetCurrentLevelDeaths();
        TauntSchedule schedule = GetSchedule(sceneName);
        UpdateBackgroundMemes(deaths, schedule);
        TriggerThresholdIfReached(deaths, schedule.First, schedule.Strong);
        TriggerThresholdIfReached(deaths, schedule.Second, schedule.Strong);
        TriggerThresholdIfReached(deaths, schedule.Strong, schedule.Strong);

        if (!InitiationScenes.Contains(sceneName) && ShouldSelectNewTaunt(deaths, schedule))
        {
            currentFormalTaunt = SelectFormalTaunt(deaths, schedule);
            lastTauntDeathCount = deaths;
        }

        if (deaths > schedule.Strong &&
            (deaths - schedule.Strong) % schedule.RepeatInterval == 0)
        {
            TryPlayLaugh(deaths);
        }
    }

    private void TriggerThresholdIfReached(int deaths, int threshold, int laughThreshold)
    {
        if (deaths < threshold || !triggeredThresholds.Add(threshold))
        {
            return;
        }

        if (InitiationScenes.Contains(trackedScene))
        {
            PlaySessionStats.MarkHintTriggered(threshold);
        }
        else
        {
            FirebaseAnalyticsUploader.RecordTauntTriggered(
                trackedScene,
                deaths,
                threshold,
                triggeredThresholds.Count
            );
        }
        if (threshold == laughThreshold)
        {
            TryPlayLaugh(deaths);
        }
    }

    private void TryPlayLaugh(int deaths)
    {
        if (deaths == lastLaughDeathCount ||
            laughClip == null ||
            audioSource.isPlaying)
        {
            return;
        }

        lastLaughDeathCount = deaths;
        audioSource.PlayOneShot(laughClip);
    }

    private void OnGUI()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!PlayableScenes.Contains(sceneName))
        {
            return;
        }

        int deaths = PlaySessionStats.GetCurrentLevelDeaths();
        TauntSchedule schedule = GetSchedule(sceneName);
        if (deaths < schedule.First)
        {
            return;
        }

        GUIStyle tauntStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        tauntStyle.normal.textColor = new Color(1f, 1f, 1f, 0.88f);

        string message = GetTaunt(sceneName, deaths, schedule);
        GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.77f,
            Screen.width * 0.5f, 72f), GUIContent.none);
        GUI.Label(new Rect(Screen.width * 0.25f, Screen.height * 0.77f,
            Screen.width * 0.5f, 72f), message, tauntStyle);

        if (sceneName == "Level03" && deaths >= schedule.Strong)
        {
            DrawLevel03PlatformArrow(tauntStyle);
        }
    }

    private void UpdateBackgroundMemes(int deaths, TauntSchedule schedule)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        if (confusedMemeRenderer == null && confusedMeme != null)
        {
            confusedMemeRenderer = CreateBackgroundMeme(
                "Confused Taunt Meme",
                confusedMeme
            );
        }
        if (judgingMemeRenderer == null && judgingMeme != null)
        {
            judgingMemeRenderer = CreateBackgroundMeme(
                "Judging Taunt Meme",
                judgingMeme
            );
        }

        if (confusedMemeRenderer != null)
        {
            PositionMemeInViewport(confusedMemeRenderer, camera, 0.14f, 0.68f);
            confusedMemeRenderer.enabled =
                PreviewMemesAlways || deaths >= schedule.Second;
        }
        if (judgingMemeRenderer != null)
        {
            PositionMemeInViewport(judgingMemeRenderer, camera, 0.86f, 0.80f);
            judgingMemeRenderer.enabled =
                PreviewMemesAlways || deaths >= schedule.Strong;
        }
    }

    private static SpriteRenderer CreateBackgroundMeme(
        string objectName,
        Texture2D texture)
    {
        GameObject memeObject = new GameObject(objectName);
        memeObject.transform.rotation = Quaternion.identity;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        SpriteRenderer renderer = memeObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 1f, 1f, 0.18f);
        renderer.sortingOrder = -1000;
        renderer.enabled = false;
        return renderer;
    }

    private static void PositionMemeInViewport(
        SpriteRenderer renderer,
        Camera camera,
        float viewportX,
        float viewportY)
    {
        const float distanceBehindCamera = 20f;
        const float viewportHeight = 0.30f;
        const float viewportMargin = 0.025f;

        Vector3 bottom = camera.ViewportToWorldPoint(
            new Vector3(
                viewportX,
                viewportY - viewportHeight * 0.5f,
                distanceBehindCamera
            )
        );
        Vector3 top = camera.ViewportToWorldPoint(
            new Vector3(
                viewportX,
                viewportY + viewportHeight * 0.5f,
                distanceBehindCamera
            )
        );

        float desiredWorldHeight = Vector3.Distance(bottom, top);
        Vector3 viewportLeft = camera.ViewportToWorldPoint(
            new Vector3(0f, viewportY, distanceBehindCamera)
        );
        Vector3 viewportRight = camera.ViewportToWorldPoint(
            new Vector3(1f, viewportY, distanceBehindCamera)
        );
        float viewportWorldWidth = Vector3.Distance(viewportLeft, viewportRight);
        float heightScale = desiredWorldHeight /
            Mathf.Max(renderer.sprite.bounds.size.y, 0.01f);
        float widthScale = viewportWorldWidth * 0.22f /
            Mathf.Max(renderer.sprite.bounds.size.x, 0.01f);
        float scale = Mathf.Min(heightScale, widthScale);
        float renderedWorldWidth = renderer.sprite.bounds.size.x * scale;
        float renderedWorldHeight = renderer.sprite.bounds.size.y * scale;
        float halfImageViewportWidth = renderedWorldWidth /
            Mathf.Max(viewportWorldWidth, 0.01f) * 0.5f;
        float viewportWorldHeight = desiredWorldHeight / viewportHeight;
        float halfImageViewportHeight = renderedWorldHeight /
            Mathf.Max(viewportWorldHeight, 0.01f) * 0.5f;
        viewportX = Mathf.Clamp(
            viewportX,
            halfImageViewportWidth + viewportMargin,
            1f - halfImageViewportWidth - viewportMargin
        );
        viewportY = Mathf.Clamp(
            viewportY,
            halfImageViewportHeight + viewportMargin,
            1f - halfImageViewportHeight - viewportMargin
        );
        Vector3 center = camera.ViewportToWorldPoint(
            new Vector3(viewportX, viewportY, distanceBehindCamera)
        );

        renderer.transform.position = center;
        renderer.transform.rotation = camera.transform.rotation;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void DestroyBackgroundMemes()
    {
        DestroyBackgroundMeme(ref confusedMemeRenderer);
        DestroyBackgroundMeme(ref judgingMemeRenderer);
    }

    private static void DestroyBackgroundMeme(ref SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        if (renderer.sprite != null)
        {
            Destroy(renderer.sprite);
        }
        Destroy(renderer.gameObject);
        renderer = null;
    }

    private static string GetTaunt(
        string sceneName,
        int deaths,
        TauntSchedule schedule)
    {
        if (sceneName == "Level01")
        {
            if (deaths >= schedule.Strong) return "The spikes have not moved. Remember where they are.";
            if (deaths >= schedule.Second) return "Try watching the trap before your next jump.";
            return "That jump taught you something. Probably.";
        }

        if (sceneName == "Level02")
        {
            if (deaths >= schedule.Strong) return "The background color tells you which platforms are solid.";
            if (deaths >= schedule.Second) return "Switch color before committing to the jump.";
            return "The platforms changed. Did you?";
        }

        if (sceneName == "Level03")
        {
            if (deaths >= schedule.Strong) return "Yes, the moving blue one. Watch its timing.";
            if (deaths >= schedule.Second) return "Maybe try observing before jumping.";
            return "The red platform appreciates your confidence.";
        }

        if (sceneName == "Level04")
        {
            if (deaths >= schedule.Strong) return "Combine color switching with the timing you already learned.";
            if (deaths >= schedule.Second) return "One mechanic at a time: color, movement, then jump.";
            return "Graduation is going wonderfully.";
        }

        return instance != null && !string.IsNullOrEmpty(instance.currentFormalTaunt)
            ? instance.currentFormalTaunt
            : MildTaunts[0];
    }

    private bool ShouldSelectNewTaunt(int deaths, TauntSchedule schedule)
    {
        if (deaths < schedule.First || deaths == lastTauntDeathCount)
        {
            return false;
        }

        return string.IsNullOrEmpty(currentFormalTaunt) ||
            deaths == schedule.Second ||
            deaths == schedule.Strong ||
            (deaths > schedule.Strong &&
             (deaths - schedule.Strong) % schedule.RepeatInterval == 0);
    }

    private string SelectFormalTaunt(int deaths, TauntSchedule schedule)
    {
        if (deaths >= schedule.Strong)
        {
            return SelectUnused(StrongTaunts, usedStrongTaunts);
        }
        if (deaths >= schedule.Second)
        {
            return SelectUnused(MediumTaunts, usedMediumTaunts);
        }
        return SelectUnused(MildTaunts, usedMildTaunts);
    }

    private static TauntSchedule GetSchedule(string sceneName)
    {
        return Schedules.TryGetValue(sceneName, out TauntSchedule schedule)
            ? schedule
            : new TauntSchedule(5, 9, 14, 5);
    }

    private static string SelectUnused(string[] pool, HashSet<int> usedIndices)
    {
        if (usedIndices.Count >= pool.Length)
        {
            usedIndices.Clear();
        }

        int index;
        do
        {
            index = Random.Range(0, pool.Length);
        }
        while (usedIndices.Contains(index));

        usedIndices.Add(index);
        return pool[index];
    }

    private static void DrawLevel03PlatformArrow(GUIStyle baseStyle)
    {
        GameObject platform = GameObject.Find("BluePlatform1");
        Camera camera = Camera.main;
        if (platform == null || camera == null)
        {
            return;
        }

        Vector3 screenPoint = camera.WorldToScreenPoint(platform.transform.position);
        if (screenPoint.z <= 0f)
        {
            return;
        }

        GUIStyle arrowStyle = new GUIStyle(baseStyle) { fontSize = 64 };
        float guiY = Screen.height - screenPoint.y;
        GUI.Label(new Rect(screenPoint.x - 55f, guiY - 115f, 110f, 90f), "▼", arrowStyle);
    }

    private static AudioClip CreateFallbackLaughClip()
    {
        const int sampleRate = 22050;
        const float duration = 0.72f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float pulse = Mathf.Max(0f, Mathf.Sin(time * Mathf.PI * 7f));
            float envelope = Mathf.Clamp01(1f - time / duration);
            float wobble = 185f + 55f * Mathf.Sin(time * 30f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * wobble * time) * pulse * envelope * 0.28f;
        }

        AudioClip clip = AudioClip.Create("ProceduralTauntLaugh", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
