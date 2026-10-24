using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Display palette only: retain the generated source PNG, dimensions and sampling unchanged.
internal static class SelectionPalettePreparation
{
    struct Bin { public Vector3 sum;public int count;public Vector3 Color=>sum/count; }
    internal static Material Get(string imagePath)
    {
        var materialPath=imagePath.Replace(".png",".mat");var palettePath=imagePath.Replace(".png","Palette.asset");
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material!=null&&material.GetTexture("_Palette")!=null)return material;
        var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(File.ReadAllBytes(imagePath));
        var pixels=image.GetPixels32();var background=pixels[image.width*(image.height/2)+2];var bg=new Vector3(background.r,background.g,background.b)/255f;
        var bins=new Dictionary<int,Bin>();
        for(var y=0;y<image.height;y+=2)for(var x=0;x<image.width;x+=2)
        {
            var c=pixels[y*image.width+x];var color=new Vector3(c.r,c.g,c.b)/255f;if((color-bg).sqrMagnitude<.085f*.085f)continue;
            var key=(c.r>>3)*1024+(c.g>>3)*32+(c.b>>3);bins.TryGetValue(key,out var bin);bin.sum+=color;bin.count++;bins[key]=bin;
        }
        Object.DestroyImmediate(image);
        var entries=bins.Values.ToArray();var centers=new List<Vector3>{entries.OrderByDescending(b=>b.count).First().Color};
        while(centers.Count<32)
        {
            var best=-1f;var next=centers[0];foreach(var b in entries){var d=centers.Min(c=>(c-b.Color).sqrMagnitude)*b.count;if(d>best){best=d;next=b.Color;}}centers.Add(next);
        }
        for(var pass=0;pass<10;pass++)
        {
            var sums=new Vector3[32];var counts=new int[32];
            foreach(var bin in entries)
            {
                var c=bin.Color;var best=float.MaxValue;var slot=0;
                for(var i=0;i<32;i++){var d=(c-centers[i]).sqrMagnitude;if(d<best){best=d;slot=i;}}
                sums[slot]+=bin.sum;counts[slot]+=bin.count;
            }
            for(var i=0;i<32;i++)if(counts[i]>0)centers[i]=sums[i]/counts[i];
        }
        var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
        if(palette==null){palette=new Texture2D(32,1,TextureFormat.RGBA32,false,false){name=Path.GetFileNameWithoutExtension(imagePath)+" Palette",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};AssetDatabase.CreateAsset(palette,palettePath);}
        palette.SetPixels32(centers.Select(c=>(Color32)new Color(c.x,c.y,c.z,1)).ToArray());palette.Apply();EditorUtility.SetDirty(palette);
        if(material==null){material=new Material(Shader.Find("UI/Behemoth/SelectionPalette"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetTexture("_Palette",palette);EditorUtility.SetDirty(material);return material;
    }
}
