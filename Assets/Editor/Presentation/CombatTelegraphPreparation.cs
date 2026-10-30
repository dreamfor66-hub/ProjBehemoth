using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Saved editor-visible skins. Reopening preserves subsequent designer edits.
[InitializeOnLoad]
internal static class CombatTelegraphPreparation
{
    static bool done;
    static CombatTelegraphPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(done||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var path="Assets/Prefabs/Combat/MonsterView.prefab";
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var ink=AssetDatabase.LoadAssetAtPath<InkUiStyle>(InkUiPreparation.StylePath);
        if(asset==null||ink==null)return;
        if(System.Array.Exists(asset.GetComponentsInChildren<InkUiSurface>(true),s=>s.role==InkSurfaceRole.Countdown&&s.source.material!=null&&s.source.material.name=="CombatInkSource")){done=true;return;}
        var prefab=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var serialized=new SerializedObject(prefab.GetComponent<MonsterView>());
            Skin((Image)serialized.FindProperty("attackNameBackdrop").objectReferenceValue,InkSurfaceRole.Brush);
            Skin((Image)serialized.FindProperty("chargeTimeBackdrop").objectReferenceValue,InkSurfaceRole.Countdown);
            PrefabUtility.SaveAsPrefabAsset(prefab,path);done=true;
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
    }
    static void Skin(Image source,InkSurfaceRole role)
    {
        if(source==null)return;
        const string hiddenPath="Assets/Art/Materials/CombatInkSource.mat";
        var hidden=AssetDatabase.LoadAssetAtPath<Material>(hiddenPath);
        if(hidden==null){hidden=new Material(Shader.Find("UI/Default"));hidden.name="CombatInkSource";hidden.color=Color.clear;AssetDatabase.CreateAsset(hidden,hiddenPath);}
        source.material=hidden;source.raycastTarget=false;
        foreach(var outline in source.GetComponents<Shadow>())Object.DestroyImmediate(outline);
        var skin=source.GetComponentInChildren<InkUiSurface>(true);if(skin==null)return;
        var first=skin.role!=role;
        skin.role=role;skin.SetAllDirty();
        if(first&&role==InkSurfaceRole.Countdown)
            source.rectTransform.sizeDelta=new Vector2(source.rectTransform.sizeDelta.x*.78f,source.rectTransform.sizeDelta.y);
    }
}
