using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Door03GreyboxBuilder
{
    private const string ScenePath =
        "Assets/Scenes/Running_Door/Level_Running_Door_3.unity";
    private const string LayoutName = "DOOR03_Greybox";

    private static readonly string[] OldRoots =
    {
        "StartGround", "EndGround", "TrapPlatfoem", "BlueBarrier",
        "HideBox", "ExitDoor", "DoorCaptureZone"
    };

    private static readonly string[] PlatformNames =
    {
        "LowerStart", "LowerMiddle", "LowerRight",
        "BlueStep1", "BlueStep2", "BlueStep3",
        "UpperLanding", "UpperCapture"
    };

    [MenuItem("Tools/Layered Lies/Build DOOR03 Greybox")]
    public static void Run()
    {
        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        if (roots.Any(root => root.name == LayoutName || PlatformNames.Contains(root.name)))
            throw new InvalidOperationException("DOOR03 already contains greybox objects.");

        GameObject Root(string name) => roots.Single(root => root.name == name);
        GameObject[] previousLayout = OldRoots.Select(Root).ToArray();
        SpriteRenderer source = Root("StartGround").GetComponent<SpriteRenderer>();
        ColorLayerManager colors = Root("ColorLayerManager").GetComponent<ColorLayerManager>();
        Transform player = Root("Player").transform;
        Camera camera = Root("Main Camera").GetComponent<Camera>();
        Transform fallZone = Root("FallZone").transform;
        BoxCollider2D fallCollider = fallZone.GetComponent<BoxCollider2D>();
        if (!source || !source.sprite || !colors || !camera || !fallCollider)
            throw new InvalidOperationException("Missing required DOOR03 components.");

        var layout = new GameObject(LayoutName);
        Color lower = new Color32(158, 161, 169, 255);
        Color blue = new Color32(103, 176, 244, 255);
        Color upper = new Color32(213, 218, 230, 255);

        GameObject Platform(string name, float x, float y, float w, float h, Color tint)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            obj.transform.SetParent(layout.transform, false);
            obj.transform.localPosition = new Vector3(x, y, 0f);
            Bounds spriteBounds = source.sprite.bounds;
            obj.transform.localScale = new Vector3(w / spriteBounds.size.x, h / spriteBounds.size.y, 1f);

            SpriteRenderer sprite = obj.GetComponent<SpriteRenderer>();
            sprite.sprite = source.sprite;
            sprite.sharedMaterial = source.sharedMaterial;
            sprite.color = tint;

            BoxCollider2D box = obj.GetComponent<BoxCollider2D>();
            box.size = spriteBounds.size;
            box.offset = spriteBounds.center;
            box.isTrigger = false;
            return obj;
        }

        Platform("LowerStart", -10f, -3.25f, 8f, 0.5f, lower);
        Platform("LowerMiddle", -1.5f, -3.25f, 6f, 0.5f, lower);
        Platform("LowerRight", 8.5f, -3.25f, 11f, 0.5f, lower);
        GameObject[] steps =
        {
            Platform("BlueStep1", -1.8f, -1.55f, 1.8f, 0.3f, blue),
            Platform("BlueStep2", 0.7f, 0f, 1.8f, 0.3f, blue),
            Platform("BlueStep3", 3.2f, 1.55f, 1.8f, 0.3f, blue)
        };
        Platform("UpperLanding", 5.9f, 3f, 3.8f, 0.4f, upper);
        Platform("UpperCapture", 11f, 3f, 5f, 0.4f, upper);

        foreach (GameObject oldRoot in previousLayout)
        {
            oldRoot.SetActive(false);
            Record(oldRoot);
        }

        // In this project the redObjects array is visible on a blue background.
        colors.redObjects = steps;
        colors.blueObjects = Array.Empty<GameObject>();
        Record(colors);

        player.position = new Vector3(-12f, -2.35f, 0f);
        Record(player);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 9.5f;
        Record(camera.transform);
        Record(camera);

        fallZone.position = new Vector3(0f, -6.5f, 0f);
        fallZone.localScale = new Vector3(34f, 1.25f, 1f);
        fallCollider.size = Vector2.one;
        fallCollider.offset = Vector2.zero;
        fallCollider.isTrigger = true;
        Record(fallZone);
        Record(fallCollider);

        if (layout.transform.childCount != 8 || steps.Any(step => !step.activeSelf))
            throw new InvalidOperationException("Unexpected greybox layout.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save DOOR03.");

        Scene saved = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var savedRoots = saved.GetRootGameObjects();
        Transform savedLayout = savedRoots.Single(root => root.name == LayoutName).transform;
        var savedColors = savedRoots.Single(root => root.name == "ColorLayerManager")
            .GetComponent<ColorLayerManager>();
        if (savedLayout.childCount != 8 ||
            savedColors.redObjects.Length != 3 ||
            savedColors.redObjects.Any(step => !step || step.transform.parent != savedLayout) ||
            savedRoots.Any(root => OldRoots.Contains(root.name) && root.activeSelf))
            throw new InvalidOperationException("Saved scene validation failed.");

        Debug.Log("DOOR03_GREYBOX_VERIFIED: 8 platforms; 3 blue-layer steps; original door setup retained inactive.");
    }

    private static void Record(UnityEngine.Object target)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        EditorUtility.SetDirty(target);
    }
}
