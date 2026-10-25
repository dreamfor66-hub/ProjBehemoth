using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Saved assets ship ready to inspect. This only assigns styles to newly authored, unstyled TMP labels.
[InitializeOnLoad]
internal static class TypographyPreparation
{
    internal const string ProfilePath="Assets/Data/Presentation/Typography.asset";
    [Serializable] sealed class RoleEntry { public string owner,field; public TextRole role; }
    [Serializable] sealed class RoleTable { public RoleEntry[] entries; }
    static Dictionary<string,TextRole> roles;
    static TypographyPreparation()=>EditorApplication.delayCall+=Prepare;

    internal static void Prepare()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall+=Prepare;return; }
        var profile=AssetDatabase.LoadAssetAtPath<TypographyProfile>(ProfilePath);
        if(profile==null||profile.dosStory==null)return;
        roles=new();
        foreach(var entry in JsonUtility.FromJson<RoleTable>(File.ReadAllText("Assets/Editor/Presentation/TypographyRoles.json")).entries)
            roles[entry.owner+"."+entry.field]=entry.role;
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var missing=false;
            foreach(var label in asset.GetComponentsInChildren<TextMeshProUGUI>(true))
                if(label.GetComponent<TypographyRoleBinding>()==null){missing=true;break;}
            if(!missing)continue;
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try { Assign(prefab,profile);PrefabUtility.SaveAsPrefabAsset(prefab,path); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var scene=SceneManager.GetSceneByPath(path);
            var wasLoaded=scene.isLoaded;
            if(wasLoaded&&scene.isDirty)continue; // Never save over a designer's unfinished scene edits.
            if(!wasLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var changed=false;
                foreach(var root in scene.GetRootGameObjects())changed|=Assign(root,profile);
                if(changed)EditorSceneManager.SaveScene(scene);
            }
            finally { if(!wasLoaded)EditorSceneManager.CloseScene(scene,true); }
        }
        AssetDatabase.SaveAssets();
    }

    static bool Assign(GameObject root,TypographyProfile profile)
    {
        var mapped=new Dictionary<TMP_Text,TextRole>();
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if(component==null||component is TMP_Text||component is TypographyRoleBinding)continue;
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.Next(true))
            {
                if(iterator.propertyType!=SerializedPropertyType.ObjectReference||iterator.objectReferenceValue is not TMP_Text label)continue;
                var field=Regex.Replace(iterator.propertyPath,@"\.Array\.data\[\d+\]","");
                var rootField=field.Split('.')[0];
                var leafField=field.Substring(field.LastIndexOf('.')+1);
                if(roles.TryGetValue(component.GetType().Name+"."+field,out var role)
                    ||roles.TryGetValue(component.GetType().Name+"."+rootField,out role)
                    ||roles.TryGetValue("*."+leafField,out role))mapped[label]=role;
            }
        }
        var changed=false;
        foreach(var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if(label.GetComponent<TypographyRoleBinding>()!=null)continue;
            var binding=label.gameObject.AddComponent<TypographyRoleBinding>();
            binding.profile=profile;binding.designSize=label.fontSize;
            binding.role=mapped.TryGetValue(label,out var role)?role:label.GetComponentInParent<Button>()!=null?TextRole.Button:label.fontSize>=64?TextRole.Title:TextRole.Base;
            binding.Apply();EditorUtility.SetDirty(binding);EditorUtility.SetDirty(label);changed=true;
        }
        return changed;
    }
}
