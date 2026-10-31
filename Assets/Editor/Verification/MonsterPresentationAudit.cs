using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class MonsterPresentationAudit
{
    [Serializable] internal sealed class Row
    {public string path,guid,name,texture,sprite;public Rect artwork,rect;public float ppu,scale;public Vector2 pivot;public List<MonsterHitBox> hitBoxes;}
    [Serializable] internal sealed class Report {public List<Row> monsters=new();}
    static MonsterPresentationAudit()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        const string request="Temp/MonsterPresentationAudit.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
        var mode=File.ReadAllText(request).Trim();File.Delete(request);
        try
        {
            var report=new Report();
            foreach(var guid in AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/Combat/Monsters"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var data=DungeonSlash.DesignerAssetPaths.Load<MonsterData>(path);var s=data.ReferenceArtwork;
                report.monsters.Add(new(){path=path,guid=guid,name=data.displayName,texture=AssetDatabase.GetAssetPath(s.texture),sprite=AssetDatabase.GetAssetPath(s),artwork=data.ArtworkBounds,rect=s.rect,ppu=s.pixelsPerUnit,scale=data.combatScale,pivot=s.pivot,hitBoxes=data.hitBoxes});
            }
            File.WriteAllText(PlayabilityRevisionPreparation.Folder+"Sizes-"+mode+".json",JsonUtility.ToJson(report,true));
        }
        catch(Exception e){File.WriteAllText(PlayabilityRevisionPreparation.Folder+"SizeAuditError.txt",e.ToString());Debug.LogException(e);}
    }
}
