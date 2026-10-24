using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class AshenVowArtPolish
{
    static AshenVowArtPolish()=>EditorApplication.delayCall+=Prepare;
    private static void Prepare()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Prepare;return;}
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        if(content==null||content.artRevision>=11)return;
        if(content.artRevision<2){EditorApplication.delayCall+=Prepare;return;}
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)){EditorApplication.delayCall+=Prepare;return;}
        const string root="Assets/Art/AshenVow/";
        if(!System.IO.File.Exists(root+"Atlases/MazeEnemyAtlas.png"))return;
        var sprites=SliceActors(root+"Atlases/MazeEnemyAtlas.png");
        var names=new[]{"B1_Boss_MazeGuardian","B1_Elite_Minotaur","B1_Minion_Bat","B1_Minion_Leech","B1_Minion_Skeleton","B2_Boss_FrostQueen","B2_Elite_FrostOgre","B2_Minion_FrostBat","B2_Minion_IceWraith","B2_Minion_Yeti","B3_Boss_SoulEater","B3_Elite_SoulKnight","B3_Minion_AshHound","B3_Minion_ShadeLancer","B3_Minion_VoidMaw","Special_MerchantMutant","Special_Minion_Mimic"};
        for(var i=0;i<names.Length;i++){var enemy=AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_"+names[i]+".asset");enemy.portrait=sprites[i];EditorUtility.SetDirty(enemy);}
        var dogPath=root+"TrailDog.png";AssetDatabase.ImportAsset(dogPath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(dogPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        var dog=AssetDatabase.LoadAssetAtPath<CompanionData>("Assets/Data/Behemoth/Companions/TrailDog.asset");dog.combatSprite=AssetDatabase.LoadAssetAtPath<Sprite>(dogPath);EditorUtility.SetDirty(dog);
        var path="Assets/Scenes/MainGameScene.unity";var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
        var flow=components.OfType<GameFlowController>().First();var so=new SerializedObject(flow);
        var events=AssetDatabase.LoadAllAssetsAtPath(root+"Atlases/EventAtlas.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        foreach(var pair in new[]{("goldEventSprite",11),("fountainEventSprite",15),("goddessEventSprite",4),("merchantEventSprite",7)})so.FindProperty(pair.Item1).objectReferenceValue=events[pair.Item2];so.ApplyModifiedPropertiesWithoutUndo();
        var backdrop=components.OfType<StageBackdropView>().First();backdrop.stages=new[]{"CastleCrypt","FrozenCrypt","Furnace"}.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>(root+"Environments/"+n+".png")).ToArray();
        var expansion=components.OfType<RunExpansionView>().First();expansion.modal.GetComponent<Image>().color=Color.white;expansion.illustratedTextY=20;
        expansion.illustratedTextHeight=100;expansion.modalNarrative.fontSize=22;expansion.plainTextHeight=310;expansion.plainTextY=165;
        for(var i=0;i<expansion.choices.Length;i++){var rect=(RectTransform)expansion.choices[i].button.transform;rect.anchoredPosition=new Vector2(0,-94-i*82);rect.sizeDelta=new Vector2(510,76);}
        foreach(var rect in new[]{(RectTransform)expansion.previousPage.transform,(RectTransform)expansion.nextPage.transform,expansion.pageLabel.rectTransform}){rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-399);rect.sizeDelta=new Vector2(rect.sizeDelta.x,22);}
        foreach(var image in components.OfType<Image>().Where(i=>i.name=="StoryPanel"||i.name=="NarrativeScroll"||i.name=="Viewport"))image.color=Color.clear;
        var map=components.OfType<DungeonMapView>().First();var mapImage=map.GetComponent<Image>();if(mapImage!=null)mapImage.color=new Color(.055f,.052f,.048f,.96f);
        foreach(var item in components.OfType<PerkChoiceItem>())
        {
            var label=item.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);label.enableAutoSizing=false;label.fontSize=22;label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
            label.rectTransform.anchoredPosition=new Vector2(34,0);label.rectTransform.sizeDelta=new Vector2(338,141);
            var icon=item.transform.Find("Icon") as RectTransform;if(icon!=null){icon.anchoredPosition=new Vector2(-174,0);icon.sizeDelta=new Vector2(64,64);}
        }
        foreach(var item in components.OfType<ShopItemView>())
            foreach(var label in item.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {label.enableAutoSizing=false;label.fontSize=label.name=="Description"?18:22;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
        // Give the saved prefabs a representative sprite as well as their runtime data binding.
        foreach(var prefab in new[]{"Assets/Prefabs/Combat/MonsterView.prefab","Assets/Prefabs/Combat/PlayerView.prefab"})
        {var p=PrefabUtility.LoadPrefabContents(prefab);var view=(Object)p.GetComponent<MonsterView>()??p.GetComponent<PlayerView>();if(view!=null){var serialized=new SerializedObject(view);var body=(Image)serialized.FindProperty("body").objectReferenceValue;body.sprite=view is MonsterView?sprites[4]:content.characters.First(c=>c.character==CharacterKind.Warrior).combatSprite;body.color=Color.white;body.preserveAspect=true;}PrefabUtility.SaveAsPrefabAsset(p,prefab);PrefabUtility.UnloadPrefabContents(p);}
        var dogRoot=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Companions/TrailDog.prefab");var dogView=dogRoot.GetComponent<CompanionView>();dogView.Configure(dog);PrefabUtility.SaveAsPrefabAsset(dogRoot,"Assets/Prefabs/Companions/TrailDog.prefab");PrefabUtility.UnloadPrefabContents(dogRoot);
        BehemothTextEditor.Table.Set("ui.run.start.controls","빠르게 긋기: 일반공격\n전장을 누른 뒤 긋기: 차지 공격 · 약점 파괴\n캐릭터를 누르는 동안: 가드\n\n방향을 골라 성 안을 탐험하세요.","AshenVow");
        BehemothTextEditor.Table.Set("ui.run.start.magic","가드 중 방향으로 긋고 떼기: 주문\n주문은 MP를 소비하며, MP는 전투마다 회복됩니다.","AshenVow");
        foreach(var pair in new[]{("ui.spell.learn.title","새 주문 익히기"),("ui.spell.learn.body","{0}을 익힙니다.\n{1} 방향에 준비할 주문을 고르세요. 배운 주문은 주문서에 남습니다."),("ui.spell.learn.prepare"," 준비"),("ui.spell.learn.store","새 주문을 익혀 주문서에 보관하고, 현재 주문을 계속 사용합니다."),("ui.spell.learn.decline","이번에는 배우지 않는다")})BehemothTextEditor.Table.Set(pair.Item1,pair.Item2,"AshenVow");
        BehemothTextEditor.Table.Set("ui.perkchoiceitem.69655f06","<align=left><size=24>{0}</size>\n<size=20>{1}</size></align>","AshenVow");
        content.artRevision=11;EditorUtility.SetDirty(content);BehemothTextEditor.SyncAll();BehemothTextEditor.ExportDefault();AssetDatabase.SaveAssets();
    }
    private static Sprite[] SliceActors(string path)
    {
        AshenVowArtPreparation.Slice("MazeEnemyAtlas",5,4);
        // Atlas metadata follows isolated alpha silhouettes rather than cutting neighboring weapons.
        var texture=new Texture2D(2,2);texture.LoadImage(System.IO.File.ReadAllBytes(path));
        var pixels=texture.GetPixels32();var width=texture.width;var height=texture.height;var visited=new bool[pixels.Length];
        var shapes=new System.Collections.Generic.List<(Rect rect,int count)>();var queue=new System.Collections.Generic.Queue<int>();
        for(var start=0;start<pixels.Length;start++)
        {
            if(visited[start]||pixels[start].a<96)continue;
            visited[start]=true;queue.Enqueue(start);var minX=width;var minY=height;var maxX=0;var maxY=0;var count=0;
            while(queue.Count>0)
            {
                var p=queue.Dequeue();var x=p%width;var y=p/width;count++;minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
                {var nx=x+dx;var ny=y+dy;if(nx<0||nx>=width||ny<0||ny>=height)continue;var n=ny*width+nx;if(visited[n]||pixels[n].a<96)continue;visited[n]=true;queue.Enqueue(n);}
            }
            if(count>200)shapes.Add((Rect.MinMaxRect(Mathf.Max(0,minX-2),Mathf.Max(0,minY-2),Mathf.Min(width,maxX+3),Mathf.Min(height,maxY+3)),count));
        }
        var metadata=new SpriteMetaData[20];
        for(var i=0;i<20;i++)
        {
            var center=new Vector2((i%5+.5f)*width/5f,(3-i/5+.5f)*height/4f);
            var shape=shapes.OrderBy(s=>(s.rect.center-center).sqrMagnitude).First();shapes.Remove(shape);
            metadata[i]=new SpriteMetaData{name="MazeEnemyAtlas_"+i.ToString("D2"),rect=shape.rect,alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f};
        }
        Object.DestroyImmediate(texture);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
#pragma warning disable CS0618
        importer.spritesheet=metadata;
#pragma warning restore CS0618
        EditorUtility.SetDirty(importer);importer.SaveAndReimport();
        // Preserve existing sprite IDs while persisting edited rectangles in Unity's native metadata.
        var metaPath=path+".meta";var yaml=System.IO.File.ReadAllText(metaPath);
        foreach(var entry in metadata)
        {
            var rect=entry.rect;
            yaml=System.Text.RegularExpressions.Regex.Replace(yaml,"(name: "+entry.name+@"\r?\n\s+rect:\r?\n\s+serializedVersion: 2\r?\n)\s+x: [^\r\n]+\r?\n\s+y: [^\r\n]+\r?\n\s+width: [^\r\n]+\r?\n\s+height: [^\r\n]+",m=>m.Groups[1].Value+$"        x: {rect.x:0}\n        y: {rect.y:0}\n        width: {rect.width:0}\n        height: {rect.height:0}");
        }
        System.IO.File.WriteAllText(metaPath,yaml);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
}
