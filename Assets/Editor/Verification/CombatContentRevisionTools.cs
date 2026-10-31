using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class CombatContentRevisionTools
{
    internal const string Folder = "Docs/Production/CombatAndContentRevision-2026-10-31/";
    static bool waiting,started,stopAfter;
    static bool visual;
    [Serializable] class Export { public MonsterRow[] monsters; public SpellRow[] spells; }
    [Serializable] class MonsterRow
    {
        public string path, guid, name, portrait, texture, animation;
        public float scale; public string json; public FrameRow[] frames; public string[] attacks;
    }
    [Serializable] class FrameRow { public string state, sprite, texture; public Rect rect; public Vector2 pivot; public float ppu; }
    [Serializable] class SpellRow { public string path, name, id; public int tier; public SpellShape shape; }
    static CombatContentRevisionTools() => EditorApplication.update += Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(waiting&&EditorApplication.isPlaying&&UnityEngine.Object.FindFirstObjectByType<CombatController>()!=null)
        {waiting=false;started=true;var probe=new GameObject("Combat content scoped verification").AddComponent<CombatContentRevisionLiveProbe>();if(visual)probe.RunVisuals();else probe.Run();}
        if(started&&File.Exists(Folder+(visual?"VisualReviewComplete.txt":"LiveComplete.txt")))
        {started=false;if(stopAfter)EditorApplication.isPlaying=false;}
        const string request = "Temp/CombatContentRevision.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        var command = File.ReadAllText(request).Trim(); File.Delete(request);
        Directory.CreateDirectory(Folder);
        try
        {
            if (command == "export") ExportData();
            if (command == "names") {CombatContentRevisionPreparation.NormalizeArtNames();AssetDatabase.SaveAssets();}
            if (command == "paintings") {CombatContentRevisionPreparation.InstallPaintings();AssetDatabase.SaveAssets();}
            if (command == "organize") {CombatContentRevisionPreparation.OrganizeCurrentPaintings();RestorePathHistory();File.WriteAllText(Folder+"PathsComplete.txt","Staged GUID history merged; active art grouped and rejected wides archived.\n");}
            if (command == "fatigue-routes") FatigueRouteBudgetReview.Run();
            if (command == "polish") CombatContentRevisionPreparation.MatchAttackNames();
            if (command == "captions") {CombatContentRevisionPreparation.InstallScenes();File.WriteAllText(Folder+"CaptionLayerInstalled.txt","Saved shared enemy caption layer above actor paintings.\n");}
            if (command == "run"||command=="visuals")
            {
                visual=command=="visuals";var complete=Folder+(visual?"VisualReviewComplete.txt":"LiveComplete.txt");
                if(File.Exists(complete))File.Delete(complete);
                stopAfter=!EditorApplication.isPlaying;waiting=true;EditorApplication.isPlaying=true;EditorApplication.isPaused=false;
            }
            if (command == "stop") EditorApplication.isPlaying = false;
        }
        catch (Exception e) { File.WriteAllText(Folder + "Error.txt", e.ToString()); Debug.LogException(e); }
    }
    static void ExportData()
    {
        foreach (var kind in new[]{"RandomEventData", "MonsterAttackData", "EncounterData"})
        {
            var rows = AssetDatabase.FindAssets("t:"+kind, new[]{"Assets/Data"}).Select(g => {
                var path=AssetDatabase.GUIDToAssetPath(g);var value=AssetDatabase.LoadMainAssetAtPath(path);
                return new SerializedRow{path=path,guid=g,json=JsonUtility.ToJson(value,true)};
            }).ToArray();
            File.WriteAllText(Folder+kind+".json",JsonUtility.ToJson(new SerializedRows{rows=rows},true));
        }
        var export = new Export {
            monsters = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data" }).Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var data = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
                var animation = data.combatAnimation;
                return new MonsterRow { path=path, guid=guid, name=data.displayName, scale=data.combatScale, json=JsonUtility.ToJson(data,true),
                    portrait=AssetDatabase.GetAssetPath(data.portrait), texture=data.portrait!=null?AssetDatabase.GetAssetPath(data.portrait.texture):"",
                    animation=AssetDatabase.GetAssetPath(animation), attacks=data.attacks.Where(a=>a!=null).Select(a=>AssetDatabase.GetAssetPath(a)).ToArray(),
                    frames=animation==null?Array.Empty<FrameRow>():new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Stunned,CombatVisualState.Dead}.Select(state=>{
                        var sprite=animation.For(state);return new FrameRow{state=state.ToString(),sprite=AssetDatabase.GetAssetPath(sprite),texture=sprite!=null?AssetDatabase.GetAssetPath(sprite.texture):"",rect=sprite!=null?sprite.rect:new Rect(),pivot=sprite!=null?sprite.pivot:Vector2.zero,ppu=sprite!=null?sprite.pixelsPerUnit:0};}).ToArray() };
            }).ToArray(),
            spells=AssetDatabase.FindAssets("t:SpellData",new[]{"Assets/Data"}).Select(g=>{
                var path=AssetDatabase.GUIDToAssetPath(g);var s=AssetDatabase.LoadAssetAtPath<SpellData>(path);return new SpellRow{path=path,name=s.displayName,id=s.id,tier=s.tier,shape=s.shape};}).ToArray()
        };
        File.WriteAllText(Folder+"Export.json",JsonUtility.ToJson(export,true));
    }
    static void RestorePathHistory()
    {
        var index=JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath));
        var history=JsonUtility.FromJson<SerializedRows>(File.ReadAllText(Folder+"PathHistory.json"));
        foreach(var move in history.rows)
        {
            var current=AssetDatabase.GUIDToAssetPath(move.guid);if(string.IsNullOrEmpty(current))throw new InvalidDataException("Missing preserved GUID: "+move.oldPath);
            var row=index.entries.FirstOrDefault(e=>e.guid==move.guid);
            if(row==null){row=new(){guid=move.guid,oldPath=move.oldPath,path=current,type="Asset",displayName=Path.GetFileNameWithoutExtension(current)};index.entries.Add(row);}
            row.aliases??=new();if(!row.aliases.Contains(move.oldPath))row.aliases.Add(move.oldPath);row.path=current;
        }
        DesignerAssetPaths.SaveIndex(index);
    }
    [Serializable] class SerializedRow {public string path,guid,json,oldPath;}
    [Serializable] class SerializedRows {public SerializedRow[] rows;}
}
