#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace DungeonSlash
{
    // Old path aliases keep one-time migrations and historical reproduction scripts usable.
    public static class DesignerAssetPaths
    {
        public const string IndexPath="Docs/Production/PlayabilityRevision-2026-10-30/DataNames.json";
        [Serializable] public sealed class Entry {public string oldPath,path,guid,id,displayName,type;public List<string> aliases=new();}
        [Serializable] public sealed class Index {public List<Entry> entries=new();}
        static Dictionary<string,string> aliases;
        public static string Resolve(string path)
        {
            if(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))return path;
            EnsureAliases();
            return aliases.TryGetValue(path,out var guid)&&!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))?AssetDatabase.GUIDToAssetPath(guid):path;
        }
        public static T Load<T>(string path)where T:UnityEngine.Object
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;
            return AssetDatabase.LoadAssetAtPath<T>(Resolve(path));
        }
        static void EnsureAliases()
        {
            if(aliases==null)
            {
                aliases=new(StringComparer.OrdinalIgnoreCase);
                if(File.Exists(IndexPath))foreach(var row in JsonUtility.FromJson<Index>(File.ReadAllText(IndexPath)).entries)
                {aliases[row.oldPath]=row.guid;aliases[row.path]=row.guid;foreach(var alias in row.aliases??new())aliases[alias]=row.guid;}
            }
        }
        public static void Invalidate()=>aliases=null;
        public static void SaveIndex(Index index)
        {
            // Replacing the file also works while an editor/indexer memory-maps the old JSON.
            var temporary=IndexPath+".writing";
            File.WriteAllText(temporary,JsonUtility.ToJson(index,true));
            if(File.Exists(IndexPath))File.Replace(temporary,IndexPath,null);else File.Move(temporary,IndexPath);
            Invalidate();
        }
    }
}
#endif
