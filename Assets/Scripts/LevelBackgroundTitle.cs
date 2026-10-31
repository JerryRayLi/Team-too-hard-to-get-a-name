using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelBackgroundTitle
{
    private const string CanvasName = "Level Background Title Canvas";

    private static readonly Dictionary<string, string> Titles = new()
    {
        { "Level01", "LEVEL01" },
        { "Level02", "LEVEL02" },
        { "Level03", "LEVEL03" },
        { "Level04", "LEVEL04" },
        { "Level_TIanyu1", "SPIKES" },
        { "level_j1", "CORPSE01" },
        { "level_j2", "CORPSE02" },
        { "level_j3", "CORPSE03" },
        { "Level_Haoyu", "SLOW" },
        { "Level_Running_Door_1", "DOOR" },
        { "Level_Yulin_v1.1", "REVERSE" }
    };

    public static string GetDisplayTitle(string sceneName)
    {
        return Titles.TryGetValue(sceneName, out string title)
            ? title
            : sceneName;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateForScene(scene);
    }

    private static void CreateForScene(Scene scene)
    {
        if (!Titles.TryGetValue(scene.name, out string title) ||
            GameObject.Find(CanvasName) != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning($"LevelBackgroundTitle could not find the Main Camera in {scene.name}.");
            return;
        }

        GameObject canvasObject = new GameObject(CanvasName);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCamera;
        canvas.planeDistance = 50f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject(title);
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Text text = textObject.AddComponent<Text>();
        text.text = title;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 280;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0.1f);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }
}
