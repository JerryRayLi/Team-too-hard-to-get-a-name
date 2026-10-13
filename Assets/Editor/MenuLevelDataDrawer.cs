
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MenuLevelData))]
public class MenuLevelDataDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 3 + 12f;
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty sceneAsset =
            property.FindPropertyRelative("sceneAsset");

        SerializedProperty sceneName =
            property.FindPropertyRelative("sceneName");

        SerializedProperty displayName =
            property.FindPropertyRelative("displayName");

        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        Rect assetRect = new Rect(
            position.x,
            position.y,
            position.width,
            lineHeight
        );

        Rect nameRect = new Rect(
            position.x,
            position.y + lineHeight + 4,
            position.width,
            lineHeight
        );

        Rect displayRect = new Rect(
            position.x,
            position.y + (lineHeight + 4) * 2,
            position.width,
            lineHeight
        );

        EditorGUI.BeginChangeCheck();

        EditorGUI.PropertyField(
            assetRect,
            sceneAsset,
            new GUIContent("Scene Asset")
        );

        if (EditorGUI.EndChangeCheck())
        {
            SceneAsset selectedScene =
                sceneAsset.objectReferenceValue as SceneAsset;

            if (selectedScene != null)
            {
                // Only update when a new scene is selected.
                sceneName.stringValue = selectedScene.name;
            }
        }

        // Manually editable, never overwritten unless
        // the Scene Asset is changed again.
        EditorGUI.PropertyField(
            nameRect,
            sceneName,
            new GUIContent("Scene Name")
        );

        EditorGUI.PropertyField(
            displayRect,
            displayName,
            new GUIContent("Display Name")
        );

        EditorGUI.EndProperty();
    }
}
#endif
