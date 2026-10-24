using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-time additive migration: content catalog books must also reach the saved playable reward pool.
[InitializeOnLoad]
internal static class SpellbookPoolPreparation
{
    const string ScenePath="Assets/Scenes/MainGameScene.unity";
    static double next;
    static SpellbookPoolPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        if(content==null||content.spellbookPoolRevision>=1||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    internal static void Prepare()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before migrating saved reward references.");
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        var books=content.equipment.Where(item=>item!=null&&item.IsRelic&&item.scope==RelicScope.Shared&&item.grantedSpell!=null&&item.grantedSpell.tier<2).Distinct().ToArray();
        var scene=SceneManager.GetSceneByPath(ScenePath);var wasOpen=scene.IsValid()&&scene.isLoaded;
        if(wasOpen&&scene.isDirty)throw new InvalidOperationException("Keep the open designer scene unchanged; save it before applying this migration.");
        if(!wasOpen)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var added=new System.Collections.Generic.List<string>();
        foreach(var run in scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<RunController>(true)))
        {
            var so=new SerializedObject(run);var items=so.FindProperty("equipment");
            foreach(var book in books)
            {
                if(Enumerable.Range(0,items.arraySize).Any(i=>items.GetArrayElementAtIndex(i).objectReferenceValue==book))continue;
                var index=items.arraySize;items.InsertArrayElementAtIndex(index);items.GetArrayElementAtIndex(index).objectReferenceValue=book;added.Add(book.name);
            }
            if(so.ApplyModifiedPropertiesWithoutUndo())EditorUtility.SetDirty(run);
        }
        if(added.Count>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        if(!wasOpen)EditorSceneManager.CloseScene(scene,true);
        content.spellbookPoolRevision=1;EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/SpellbookPool-Preparation.txt","Registered regular shared spellbooks: "+books.Length+"\nAdded: "+added.Count+"\n"+string.Join("\n",added)+"\nNo shop slot, weight, pricing, perk or relic-effect changes. "+DateTime.Now);
    }
}
