using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Scoped, opt-in request in the existing Editor; no scene mutation or Play mode.
[InitializeOnLoad]
internal static class PlayerCanvasExpansionVerification
{
    const string Folder="Docs/Production/PlayerCanvas-2026-10-28/";
    const string Request="Temp/PlayerCanvasExpansion.verify";
    static PlayerCanvasExpansionVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        try{File.Delete(Request);}catch(IOException){return;} // Writer may still be closing the request file.
        try {Verify();} catch(Exception error){File.WriteAllText(Folder+"UnityVerification.txt","FAIL\n"+error);Debug.LogException(error);}
    }
    static readonly MethodInfo Populate=typeof(Image).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null);
    static (Rect bounds,Rect uv) Geometry(Image image,CombatSpriteAnimator animator,CombatSpriteAnimation library,Sprite sprite)
    {
        animator.library=library;image.sprite=sprite;image.overrideSprite=null;
        using var vertices=new VertexHelper();Populate.Invoke(image,new object[]{vertices});animator.ModifyMesh(vertices);
        if(vertices.currentVertCount!=4)throw new InvalidOperationException("Expected untrimmed UI quad: "+sprite.name);
        var min=Vector2.positiveInfinity;var max=Vector2.negativeInfinity;var uvMin=min;var uvMax=max;var vertex=new UIVertex();
        for(var i=0;i<4;i++){vertices.PopulateUIVertex(ref vertex,i);min=Vector2.Min(min,vertex.position);max=Vector2.Max(max,vertex.position);uvMin=Vector2.Min(uvMin,vertex.uv0);uvMax=Vector2.Max(uvMax,vertex.uv0);}
        return (Rect.MinMaxRect(min.x,min.y,max.x,max.y),Rect.MinMaxRect(uvMin.x,uvMin.y,uvMax.x,uvMax.y));
    }
    static Vector2 PixelPosition((Rect bounds,Rect uv) mesh,Vector2 uv)=>mesh.bounds.min+Vector2.Scale(mesh.bounds.size,new Vector2((uv.x-mesh.uv.xMin)/mesh.uv.width,(uv.y-mesh.uv.yMin)/mesh.uv.height));
    static void Verify()
    {
        var log=new StringBuilder("Actual Unity Image.OnPopulateMesh + CombatSpriteAnimator.ModifyMesh. No scene changes or Play mode.\n");
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Angular/WorldArtwork.asset");
        var paths=new System.Collections.Generic.List<string>();
        foreach(var name in Enum.GetNames(typeof(CharacterKind)))paths.Add("Assets/Data/Presentation/Angular/Animations/Hero-"+name+".asset");
        paths.Add("Assets/Data/Presentation/Trials/Angular01_WarriorAnimation.asset");
        var count=0;
        foreach(var path in paths)
        {
            var library=AssetDatabase.LoadAssetAtPath<CombatSpriteAnimation>(path);if(library==null)throw new InvalidOperationException(path);
            foreach(var state in new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Guard})
            {
                var sprite=library.For(state);if(sprite==null)continue;
                var source=AssetDatabase.GetAssetPath(sprite);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                var oldLibrary=Object.Instantiate(library);oldLibrary.canvasPaddingPixels=Vector4.zero;
                Sprite original=null;GameObject go=null;
                try
                {
                    texture.LoadImage(File.ReadAllBytes(Folder+"Before/"+source));
                    var oldMeta=File.ReadAllText(Folder+"Before/"+source+".meta");
                    var meshType=(SpriteMeshType)int.Parse(Regex.Match(oldMeta,@"spriteMeshType: (\d+)").Groups[1].Value);
                    original=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,meshType);
                    var padding=library.canvasPaddingPixels;
                    if(sprite.rect.size!=new Vector2(texture.width+padding.x+padding.z,texture.height+padding.y+padding.w))throw new InvalidOperationException("Imported sprite rescaled: "+source);
                    go=new GameObject("Temporary player canvas geometry check",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                    go.hideFlags=HideFlags.HideAndDontSave;
                    var image=go.GetComponent<Image>();image.preserveAspect=true;
                    var animator=go.AddComponent<CombatSpriteAnimator>();animator.presentation=profile;
                    var rect=image.rectTransform;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(23,-19);
                    foreach(var frame in new[]{new Vector2(640,680),new Vector2(960,1020)})
                    {
                        rect.sizeDelta=frame;
                        var before=Geometry(image,animator,oldLibrary,original);
                        var after=Geometry(image,animator,library,sprite);
                        foreach(var point in new[]{Vector2.zero,Vector2.one,new Vector2(1,0),new Vector2(0,1),Vector2.one*.5f})
                        {
                            var expected=PixelPosition(before,point);
                            var pixel=new Vector2(padding.x,padding.y)+Vector2.Scale(new Vector2(texture.width,texture.height),point);
                            var actual=PixelPosition(after,new Vector2(pixel.x/sprite.rect.width,pixel.y/sprite.rect.height));
                            if(Vector2.Distance(expected,actual)>.002f)throw new InvalidOperationException("Artwork moved/scaled: "+source+" "+expected+" != "+actual);
                        }
                        if(rect.anchoredPosition!=new Vector2(23,-19)||rect.localScale!=Vector3.one||rect.localRotation!=Quaternion.identity)throw new InvalidOperationException("Pose transform mutation");
                    }
                    count++;log.AppendLine("PASS "+path+" / "+state+": original pixels retain screen position/size in rest and combat frames; expanded margins drawable.");
                }
                finally{if(go!=null)Object.DestroyImmediate(go);if(original!=null)Object.DestroyImmediate(original);Object.DestroyImmediate(texture);Object.DestroyImmediate(oldLibrary);}
            }
        }
        File.WriteAllText(Folder+"UnityVerification.txt",log+"COMPLETE "+count+" pose references, 2 frame sizes each.\n");
    }
}
