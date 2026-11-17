using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplyJerryPlayerVisualToOtherLevels
{
    private const string JerryPrefabPath =
        "Assets/Prefabs/level_jerry/Player_Jerry_Geometric.prefab";
    private const string SharedVisualPrefabPath =
        "Assets/Prefabs/PlayerGeometricVisual.prefab";

    [MenuItem("Tools/Players/Apply Jerry Visual To Other Levels")]
    public static void ApplyAll()
    {
        GameObject visualPrefab = CreateOrUpdateVisualPrefab();
        string[] scenePaths = AssetDatabase
            .FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsTargetLevel)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        int changedScenes = 0;
        int changedPlayers = 0;

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single
            );

            int scenePlayerCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in
                         root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject player = candidate.gameObject;
                    bool identifiesPlayer =
                        player.CompareTag("Player") || player.name == "Player";

                    if (!identifiesPlayer ||
                        player.GetComponent<Rigidbody2D>() == null ||
                        player.GetComponent<Collider2D>() == null ||
                        player.GetComponent<SpriteRenderer>() == null ||
                        player.GetComponent<SharedGeometricPlayerVisual>() != null)
                    {
                        continue;
                    }

                    ApplyToPlayer(player, visualPrefab, scene);
                    scenePlayerCount++;
                    changedPlayers++;
                }
            }

            if (scenePlayerCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                changedScenes++;
                Debug.Log(
                    $"Applied geometric player visual to " +
                    $"{scenePlayerCount} player(s) in {scenePath}."
                );
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"Finished applying the shared geometric player visual: " +
            $"{changedPlayers} player(s) across {changedScenes} scene(s)."
        );
    }

    private static bool IsTargetLevel(string path)
    {
        if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Level_jerry/") ||
            path.EndsWith("/MainMenu.unity") ||
            path.EndsWith("/WinScene.unity") ||
            path.EndsWith("/SampleScene.unity"))
        {
            return false;
        }

        return true;
    }

    private static GameObject CreateOrUpdateVisualPrefab()
    {
        GameObject sourceRoot =
            PrefabUtility.LoadPrefabContents(JerryPrefabPath);

        try
        {
            Transform sourceVisual =
                sourceRoot.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Visual");

            if (sourceVisual == null)
            {
                throw new InvalidOperationException(
                    "Jerry geometric prefab does not contain Visual."
                );
            }

            GameObject visualCopy =
                UnityEngine.Object.Instantiate(sourceVisual.gameObject);
            visualCopy.name = "GeometricVisual";
            visualCopy.transform.SetParent(null, false);
            visualCopy.transform.localPosition = Vector3.zero;
            visualCopy.transform.localRotation = Quaternion.identity;
            visualCopy.transform.localScale = Vector3.one;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                visualCopy,
                SharedVisualPrefabPath
            );
            UnityEngine.Object.DestroyImmediate(visualCopy);

            if (savedPrefab == null)
            {
                throw new InvalidOperationException(
                    "Could not save shared geometric visual prefab."
                );
            }

            return savedPrefab;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(sourceRoot);
        }
    }

    private static void ApplyToPlayer(
        GameObject player,
        GameObject visualPrefab,
        Scene scene
    )
    {
        GameObject visualInstance = (GameObject)
            PrefabUtility.InstantiatePrefab(visualPrefab, scene);

        visualInstance.name = "GeometricVisual";
        visualInstance.transform.SetParent(player.transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        SharedGeometricPlayerVisual adapter =
            player.AddComponent<SharedGeometricPlayerVisual>();

        adapter.sourceRenderer = player.GetComponent<SpriteRenderer>();
        adapter.visual = visualInstance.transform;
        adapter.leftLeg = FindChild(
            visualInstance.transform,
            "Left Leg Pivot"
        );
        adapter.rightLeg = FindChild(
            visualInstance.transform,
            "Right Leg Pivot"
        );
        adapter.leftArm = FindChild(
            visualInstance.transform,
            "Left Arm Pivot"
        );
        adapter.rightArm = FindChild(
            visualInstance.transform,
            "Right Arm Pivot"
        );

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(adapter);
    }

    private static Transform FindChild(
        Transform root,
        string childName
    )
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == childName);
    }
}
