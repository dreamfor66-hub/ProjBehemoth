using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class DesignerDataNamingVerification
{
    static DesignerDataNamingVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        const string request="Temp/DesignerDataNaming.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
        File.Delete(request);var log=PlayabilityRevisionPreparation.Folder+"NamingVerification.txt";
        const string sourcePath="Assets/Data/Combat/Buffs/NamingVerificationProbe.asset",referencePath="Assets/Editor/Verification/NamingVerificationReference.asset";
        if(File.Exists(sourcePath)||File.Exists(referencePath))throw new InvalidOperationException("Refusing to replace existing naming test assets");
        var table=BehemothTextEditor.Table;var keys=new HashSet<string>();string guid=null;
        var report=new List<string>();
        void Check(bool ok,string name){report.Add((ok?"PASS ":"FAIL ")+name);if(!ok)throw new InvalidOperationException(name);}
        try
        {
            var count=JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath)).entries.Count;
            var source=ScriptableObject.CreateInstance<BuffData>();source.displayName="편집 확인 A";AssetDatabase.CreateAsset(source,sourcePath);guid=AssetDatabase.AssetPathToGUID(sourcePath);BehemothTextEditor.SyncAsset(source);foreach(var binding in source.textKeys)keys.Add(binding.key);
            var reference=ScriptableObject.CreateInstance<EquipmentData>();reference.abilities.Add(new(){effects=new(){new EquipmentEffect{effectType=EffectType.AddBuff,buff=source}}});AssetDatabase.CreateAsset(reference,referencePath);AssetDatabase.SaveAssets();
            DesignerDataNaming.Normalize();var first=AssetDatabase.GUIDToAssetPath(guid);
            Check(first.EndsWith("Data_Buff_편집_확인_A.asset"),"new gameplay data receives a consistent readable filename");
            Check(DesignerAssetPaths.Load<BuffData>(sourcePath)==source,"historical source path resolves through stable GUID alias");
            source.displayName="편집 확인 B";BehemothTextEditor.SyncAsset(source);EditorUtility.SetDirty(source);AssetDatabase.SaveAssets();DesignerDataNaming.Normalize();var second=AssetDatabase.GUIDToAssetPath(guid);
            Check(second.EndsWith("Data_Buff_편집_확인_B.asset")&&AssetDatabase.AssetPathToGUID(second)==guid,"editing displayName renames the file while retaining its GUID");
            Check(reference.abilities[0].effects[0].buff==source&&DesignerAssetPaths.Load<BuffData>(first)==source,"serialized effect reference and intermediate filename alias both survive rename");
            Check(source.textKeys.Select(k=>k.key).ToHashSet().SetEquals(keys),"localization keys remain stable through repeated name edits");
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));guid=null;AssetDatabase.DeleteAsset(referencePath);table.entries.RemoveAll(e=>keys.Contains(e.key));table.Invalidate();EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();DesignerDataNaming.Normalize();
            Check(JsonUtility.FromJson<DesignerAssetPaths.Index>(File.ReadAllText(DesignerAssetPaths.IndexPath)).entries.Count==count,"temporary fixtures are cleaned and production index count is preserved");report.Add("COMPLETE 6 PASS / 0 FAIL");
        }
        catch(Exception e){report.Add(e.ToString());Debug.LogException(e);}
        finally
        {
            if(guid!=null)AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));if(File.Exists(referencePath))AssetDatabase.DeleteAsset(referencePath);
            table.entries.RemoveAll(e=>keys.Contains(e.key));table.Invalidate();EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
            File.WriteAllText(log,string.Join("\n",report)+"\n");
        }
    }
}
