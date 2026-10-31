using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Repainted art is indexed at the ORIGINAL canvas size. No coarse intermediate raster.
[InitializeOnLoad]
internal static class FlatArtImport
{
    [Serializable] internal sealed class Entry
    {
        public string source,target;
        public int column,row,columns=1,rows=1;
        public bool fitOriginal;
        public bool coherentTones;
        public int width,height,left,bottom,paintWidth,paintHeight;
        public string hash;
    }
    [Serializable] internal sealed class Manifest { public List<Entry> entries=new(); }
    const string Path="Assets/Art/DarkSeven/Import.json";
    static double next;
    static string lastInputs;
    static FlatArtImport(){EditorApplication.delayCall+=Prepare;EditorApplication.update+=Poll;}
    static void Poll(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;Prepare();}
    static void Prepare()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Path))return;
        var palette=AssetDatabase.LoadAssetAtPath<DarkPaletteStyle>("Assets/Data/Presentation/ObsidianSeven.asset");
        if(palette==null)return;
        var manifestTime=File.GetLastWriteTimeUtc(Path);
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path));var changed=false;
        var inputs=new System.Text.StringBuilder(File.GetLastWriteTimeUtc(Path).Ticks.ToString()+JsonUtility.ToJson(palette));
        foreach(var entry in manifest.entries)inputs.Append(entry.source).Append(File.GetLastWriteTimeUtc(entry.source).Ticks);
        if(lastInputs==inputs.ToString())return;
        lastInputs=inputs.ToString();
        var sourceKeys=new Dictionary<string,string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var entry in manifest.entries)
            {
                if(!File.Exists(entry.source)||!File.Exists(entry.target))continue;
                if(!sourceKeys.TryGetValue(entry.source,out var sourceKey))
                {
                    using var sha=SHA256.Create();
                    sourceKey=Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(entry.source)))+Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(palette))));
                    sourceKeys.Add(entry.source,sourceKey);
                }
                var key=(entry.coherentTones?"native-regions-1/":"native-flat-2/")+sourceKey+entry.column+","+entry.row;
                if(entry.hash==key)continue;
                var original=Load(entry.target);var source=Load(entry.source);
                try
                {
                    if(entry.width==0)
                    {
                        entry.width=original.width;entry.height=original.height;
                        var box=Bounds(original.GetPixels32(),original.width,original.height);
                        entry.left=box.x;entry.bottom=box.y;entry.paintWidth=box.width;entry.paintHeight=box.height;
                    }
                    var sw=source.width/entry.columns;var sh=source.height/entry.rows;
                    var pixels=source.GetPixels32();var cuts=entry.coherentTones?ToneCuts(pixels):null;var cell=new Color32[sw*sh];
                    var sx=entry.column*sw;var sy=source.height-(entry.row+1)*sh;
                    for(var y=0;y<sh;y++)Array.Copy(pixels,(sy+y)*source.width+sx,cell,y*sw,sw);
                    var crop=entry.fitOriginal?Bounds(cell,sw,sh):new RectInt(0,0,sw,sh);
                    var dest=entry.fitOriginal?new RectInt(entry.left,entry.bottom,entry.paintWidth,entry.paintHeight):new RectInt(0,0,entry.width,entry.height);
                    // Image service can return a canvas one pixel short. Preserve the saved
                    // production canvas; reject any actual resolution/aspect change.
                    if(!entry.fitOriginal&&(Mathf.Abs(sw-entry.width)>1||Mathf.Abs(sh-entry.height)>1))throw new InvalidOperationException("Native repaint dimensions changed: "+entry.source);
                    var output=new Color32[entry.width*entry.height];var inks=Array.ConvertAll(palette.Colors,c=>(Color32)c);
                    for(var y=0;y<dest.height;y++)for(var x=0;x<dest.width;x++)
                    {
                        var p=cell[(crop.y+Mathf.Min(crop.height-1,y*crop.height/dest.height))*sw+crop.x+Mathf.Min(crop.width-1,x*crop.width/dest.width)];
                        if(p.a<128)continue;
                        var accentAllowed=p.r>p.g*1.6f&&p.r>p.b*1.7f&&p.r-p.g>50;
                        var nearest=0;var best=int.MaxValue;
                        if(cuts!=null)
                        {
                            if(accentAllowed)nearest=6;
                            else {var value=(p.r+p.g+p.b)/3;while(nearest<cuts.Length&&value>=cuts[nearest])nearest++;}
                        }
                        else for(var n=0;n<inks.Length;n++)
                        {
                            if(n==6&&!accentAllowed)continue;
                            var dr=p.r-inks[n].r;var dg=p.g-inks[n].g;var db=p.b-inks[n].b;
                            var distance=dr*dr+dg*dg+db*db;if(distance<best){best=distance;nearest=n;}
                        }
                        output[(dest.y+y)*entry.width+dest.x+x]=inks[nearest];
                    }
                    var texture=new Texture2D(entry.width,entry.height,TextureFormat.RGBA32,false);texture.SetPixels32(output);texture.Apply();
                    var temp=entry.target+".inktmp";File.WriteAllBytes(temp,texture.EncodeToPNG());File.Replace(temp,entry.target,null);Object.DestroyImmediate(texture);
                    entry.hash=key;changed=true;
                }
                finally{Object.DestroyImmediate(original);Object.DestroyImmediate(source);}
            }
            if(changed&&File.GetLastWriteTimeUtc(Path)==manifestTime)File.WriteAllText(Path,JsonUtility.ToJson(manifest,true));
        }
        catch(Exception exception){Debug.LogException(exception);}
        finally{AssetDatabase.StopAssetEditing();}
        if(changed)AssetDatabase.Refresh();
    }
    static Texture2D Load(string path){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(path));return texture;}
    // Put tone boundaries in the source histogram's empty valleys, rather than
    // through a broad flat color whose tiny codec/model variations cause mottling.
    static int[] ToneCuts(Color32[] pixels)
    {
        var histogram=new int[256];
        foreach(var p in pixels)if(p.a>=128&&!(p.r>p.g*1.6f&&p.r>p.b*1.7f&&p.r-p.g>50))histogram[(p.r+p.g+p.b)/3]++;
        var cuts=new[]{12,39,76,132,196};
        for(var n=0;n<cuts.Length;n++)
        {
            var center=cuts[n];var chosen=center;var best=double.MaxValue;var radius=n==0?4:17;
            for(var cut=center-radius;cut<=center+radius;cut++)
            {
                var density=0;for(var k=-2;k<=2;k++)density+=histogram[Mathf.Clamp(cut+k,0,255)];
                var score=density*(1+Mathf.Abs(cut-center)*.015)+Mathf.Abs(cut-center)*.01;
                if(score<best){best=score;chosen=cut;}
            }
            cuts[n]=chosen;
        }
        return cuts;
    }
    static RectInt Bounds(Color32[] p,int width,int height)
    {
        var left=width;var right=0;var bottom=height;var top=0;
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)if(p[y*width+x].a>=128)
        {left=Mathf.Min(left,x);right=Mathf.Max(right,x+1);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y+1);}
        return new RectInt(left,bottom,Mathf.Max(1,right-left),Mathf.Max(1,top-bottom));
    }
}
