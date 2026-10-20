using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlaySessionStats
{
    public sealed class LevelStats
    {
        public int? BestDeaths { get; internal set; }
        public float? BestDurationSeconds { get; internal set; }
        public bool WasSkipped { get; internal set; }
        public int Visits { get; internal set; }

        public string DeathsDisplay => BestDeaths.HasValue
            ? BestDeaths.Value.ToString()
            : "N/A";

        public string DurationDisplay => BestDurationSeconds.HasValue
            ? $"{BestDurationSeconds.Value:F2}s"
            : "N/A";
    }

    private static readonly HashSet<string> PlayableScenes = new()
    {
        "Level01",
        "Level02",
        "Level03",
        "Level04",
        "Level_TIanyu1",
        "level_j1",
        "level_j2",
        "Level_Haoyu",
        "Level_Running_Door_1",
        "Level_Yulin_v1.1"
    };

    private static readonly Dictionary<string, LevelStats> StatsByLevel = new();

    private static string currentLevel;
    private static double currentLevelStartedAt;
    private static int currentLevelDeaths;
    private static bool currentResultRecorded;

    public static int TotalDeaths { get; private set; }
    public static int CurrentLevelDeaths => currentLevelDeaths;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public static void RecordDeath()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!PlayableScenes.Contains(sceneName) || sceneName != currentLevel)
        {
            return;
        }

        TotalDeaths++;
        currentLevelDeaths++;
    }

    public static void MarkCurrentLevelSkipped()
    {
        if (string.IsNullOrEmpty(currentLevel) || currentResultRecorded)
        {
            return;
        }

        LevelStats stats = GetOrCreate(currentLevel);
        if (!stats.BestDeaths.HasValue)
        {
            stats.WasSkipped = true;
        }

        FirebaseAnalyticsUploader.RecordLevelSkipped(currentLevel, stats.Visits);
        currentResultRecorded = true;
        Debug.Log($"LEVEL_STATS level={currentLevel} deaths=N/A duration=N/A result=skipped");
    }

    public static void MarkCurrentLevelCompleted()
    {
        if (string.IsNullOrEmpty(currentLevel) || currentResultRecorded)
        {
            return;
        }

        float duration = GetCurrentLevelElapsedSeconds();
        LevelStats stats = GetOrCreate(currentLevel);

        bool isBetter =
            !stats.BestDeaths.HasValue ||
            currentLevelDeaths < stats.BestDeaths.Value ||
            (currentLevelDeaths == stats.BestDeaths.Value &&
             duration < stats.BestDurationSeconds.Value);

        if (isBetter)
        {
            stats.BestDeaths = currentLevelDeaths;
            stats.BestDurationSeconds = duration;
            stats.WasSkipped = false;
        }

        FirebaseAnalyticsUploader.RecordLevelCompleted(
            currentLevel,
            currentLevelDeaths,
            duration,
            stats.Visits,
            isBetter
        );
        currentResultRecorded = true;
        Debug.Log(
            $"LEVEL_STATS level={currentLevel} deaths={currentLevelDeaths} " +
            $"duration={duration:F2}s result=completed best={isBetter}"
        );
    }

    public static int GetCurrentLevelDeaths()
    {
        return currentLevelDeaths;
    }

    public static float GetCurrentLevelElapsedSeconds()
    {
        return string.IsNullOrEmpty(currentLevel)
            ? 0f
            : (float)(Time.realtimeSinceStartupAsDouble - currentLevelStartedAt);
    }

    public static IReadOnlyDictionary<string, LevelStats> GetAllLevelStats()
    {
        return StatsByLevel;
    }

    public static void RestartCurrentLevelAttempt()
    {
        if (string.IsNullOrEmpty(currentLevel))
        {
            return;
        }

        currentLevelStartedAt = Time.realtimeSinceStartupAsDouble;
        currentLevelDeaths = 0;
        currentResultRecorded = false;
        GetOrCreate(currentLevel).Visits++;
    }

    public static void ResetSession()
    {
        TotalDeaths = 0;
        StatsByLevel.Clear();
        currentLevel = null;
        currentLevelStartedAt = 0d;
        currentLevelDeaths = 0;
        currentResultRecorded = false;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == currentLevel)
        {
            // A death reloads the same scene. Keep this run's timer and deaths.
            return;
        }

        if (!PlayableScenes.Contains(scene.name))
        {
            currentLevel = null;
            currentLevelDeaths = 0;
            currentResultRecorded = false;
            return;
        }

        currentLevel = scene.name;
        currentLevelStartedAt = Time.realtimeSinceStartupAsDouble;
        currentLevelDeaths = 0;
        currentResultRecorded = false;
        GetOrCreate(scene.name).Visits++;
    }

    private static LevelStats GetOrCreate(string sceneName)
    {
        if (!StatsByLevel.TryGetValue(sceneName, out LevelStats stats))
        {
            stats = new LevelStats();
            StatsByLevel.Add(sceneName, stats);
        }

        return stats;
    }
}
