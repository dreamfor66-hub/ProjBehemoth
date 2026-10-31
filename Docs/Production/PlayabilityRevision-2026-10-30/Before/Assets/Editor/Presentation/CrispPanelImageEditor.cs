using DungeonSlash;
using UnityEditor;

[CustomEditor(typeof(CrispPanelImage)),CanEditMultipleObjects]
internal sealed class CrispPanelImageEditor : UnityEditor.UI.ImageEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("borderThickness"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("borderColor"));
        serializedObject.ApplyModifiedProperties();
    }
}
