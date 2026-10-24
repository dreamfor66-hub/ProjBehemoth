using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class FatiguePreparation
{
    const string Root="Assets/Data/Behemoth/",Art="Assets/Art/Arcade224/";
    static double next;
    static FatiguePreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+5;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var content=AssetDatabase.LoadAssetAtPath<GameContent>(Root+"GameContent.asset");
        if(content==null||content.fatigueRevision>=1||!File.Exists(Art+"FatigueFaces.png")||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    static T Asset<T>(string path)where T:ScriptableObject
    {var data=AssetDatabase.LoadAssetAtPath<T>(path);if(data!=null)return data;Directory.CreateDirectory(Path.GetDirectoryName(path));data=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(data,path);return data;}
    internal static void Prepare()
    {
        if(EditorApplication.isPlaying)return;
        var content=AssetDatabase.LoadAssetAtPath<GameContent>(Root+"GameContent.asset");
        var settings=AssetDatabase.LoadAssetAtPath<FatigueSettings>(Root+"Settings/Fatigue.asset");
        if(settings==null)
        {
            settings=Asset<FatigueSettings>(Root+"Settings/Fatigue.asset");
            settings.attackSpeed=new AnimationCurve(new Keyframe(0,.35f),new Keyframe(10,.38f),new Keyframe(40,.55f),new Keyframe(60,.70f),new Keyframe(80,.86f),new Keyframe(90,.95f),new Keyframe(100,1));
            EditorUtility.SetDirty(settings);
        }
        content.fatigue=settings;
        var table=BehemothTextEditor.Table;table.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach(var p in new Dictionary<string,string>{
            ["ui.fatigue.title"]="피로도",["ui.fatigue.normal"]="정상",["ui.fatigue.tired"]="지침",["ui.fatigue.exhausted"]="피로",["ui.fatigue.danger"]="탈진",
            ["ui.fatigue.detail"]="피로도 {0}/100 · {1}\n공격속도 -{2}%\n최종피해 -{3}%",
            ["ui.fatigue.grace"]="지치기까지 {0}보의 여유가 남아 있다.",
            ["ui.fatigue.warning"]="더 무리해서 움직였다가는 죽을 수도 있다.",
            ["ui.fatigue.death"]="끝내 발걸음을 옮기지 못하고 쓰러졌다.\n피로도가 0이 되었다.",
            ["ui.camp.title"]="모험가의 임시캠프",["ui.camp.map"]="야영지",["ui.camp.marker"]="야",
            ["ui.camp.intro"]="모험가들이 남긴 안전한 쉼터다.\n야영은 이 층에서 한 번만 할 수 있다.",
            ["ui.camp.full"]="야영한다",["ui.camp.used"]="야영한다 · 사용함",
            ["ui.camp.full.summary"]="피로도 100까지 · 체력 10 회복",
            ["ui.camp.full.detail"]="피로도 100까지 · 체력 10 회복\n다시 걸을 여유를 얻는다.\n살아 있는 일반 몬스터가 이동할 수 있다.",
            ["ui.camp.full.result"]="몸을 누이고 충분히 쉬었다.\n피로도 100 · 체력 10 회복",
            ["ui.camp.full.result.moved"]="몸을 누이고 충분히 쉬었다.\n피로도 100 · 체력 10 회복\n쉬는 사이 통로에서 발소리가 들렸다.",
            ["ui.camp.short"]="잠깐 쉰다",["ui.camp.short.detail"]="피로도 60까지 회복",
            ["ui.camp.short.result"]="잠시 숨을 골랐다.\n피로도가 60까지 회복되었다.",
            ["ui.camp.leave"]="지금은 떠난다",["ui.fatigue.potion.name"]="기운 회복 포션",["ui.fatigue.potion.detail"]="피로도 30 회복\n지치기까지 10보의 여유를 얻는다."
        })table.Set(p.Key,p.Value,"Fatigue");
        table.Invalidate();EditorUtility.SetDirty(table);
        var faces=ImportFaces();var potion=Potion(content);
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/MainGameScene.unity");var open=scene.IsValid()&&scene.isLoaded;
        if(open&&scene.isDirty)throw new InvalidOperationException("Save the authored scene before preparing fatigue UI.");
        if(!open)scene=EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity",OpenSceneMode.Additive);
        var run=All<RunController>(scene).First();var combat=All<CombatController>(scene).First();var flow=All<GameFlowController>(scene).First();
        var canvas=All<Canvas>(scene).First(c=>c.transform.Find("QuickInventory")!=null);
        var hud=Child(canvas.transform,"FatigueHUD",typeof(RectTransform),typeof(FatigueHudView)).GetComponent<FatigueHudView>();Place(hud.transform,0,0,384,224);
        hud.run=run;hud.expansion=All<RunExpansionView>(scene).First();hud.faces=faces;hud.face=Box(hud.transform,"Expression",-174,52,32,32);hud.face.sprite=faces.Face(CharacterKind.Warrior,0);hud.face.raycastTarget=true;
        var select=hud.face.GetComponent<Button>()??hud.face.gameObject.AddComponent<Button>();select.transition=Selectable.Transition.None;select.targetGraphic=hud.face;
        hud.hover=hud.face.GetComponent<HoverTooltipTrigger>()??hud.face.gameObject.AddComponent<HoverTooltipTrigger>();hud.hover.anchor=hud.face.rectTransform;hud.hover.view=All<QuickInventoryView>(scene).First().tooltip;
        // Same logical area as the world, below interactive HUD/drawers. No overlay on central combat signals.
        var haze=Child(canvas.transform,"FatigueHaze",typeof(RectTransform),typeof(CanvasRenderer),typeof(FatigueHaze)).GetComponent<FatigueHaze>();Place(haze.transform,0,-12,384,200);haze.raycastTarget=false;haze.transform.SetSiblingIndex(canvas.transform.Find("Background").GetSiblingIndex()+1);hud.haze=haze;
        hud.transform.SetSiblingIndex(canvas.transform.Find("QuickInventory").GetSiblingIndex()+1);
        ReadabilityPreparation.ConfigureHud(hud);
        var expansion=hud.expansion;
        Place(expansion.modal.transform,0,0,384,224);
        expansion.compactChoices=Enumerable.Range(0,3).Select(n=>
        {
            var name="CompactChoice_"+n;var parent=expansion.choices[0].button.transform.parent;
            var found=parent.Find(name);var go=found!=null?found.gameObject:Object.Instantiate(expansion.choices[0].button.gameObject,parent);go.name=name;
            var row=new ExpansionChoiceButton{button=go.GetComponent<Button>(),title=go.transform.Find("ChoiceTitle").GetComponent<TextMeshProUGUI>(),description=go.transform.Find("ChoiceDescription").GetComponent<TextMeshProUGUI>(),icon=go.transform.Find("Icon").GetComponent<Image>()};
            Place(row.button.transform,0,-22-n*30,348,28);Place(row.title.transform,0,6,330,14);Place(row.description.transform,0,-8,330,12);row.icon.gameObject.SetActive(false);go.SetActive(false);return row;
        }).ToArray();EditorUtility.SetDirty(expansion);
        var images=All<PresentationImageBindings>(scene).First();var faceBinding=images.bindings.FirstOrDefault(b=>b.image==hud.face);if(faceBinding==null)images.bindings=images.bindings.Append(new PresentationImageBindings.Binding{image=hud.face,arcade=hud.face.sprite,color=Color.white}).ToArray();else faceBinding.arcade=hud.face.sprite;EditorUtility.SetDirty(images);
        var campPath=Art+"AdventurerCamp.png";
        if(File.Exists(campPath))
        {AssetDatabase.ImportAsset(campPath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(campPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();flow.campIllustration=AssetDatabase.LoadAssetAtPath<Sprite>(campPath);}
        else flow.campIllustration=content.events.First(e=>e.id=="Shelter").illustration;
        EditorUtility.SetDirty(flow);
        var so=new SerializedObject(run);var pool=so.FindProperty("equipment");if(!Enumerable.Range(0,pool.arraySize).Any(i=>pool.GetArrayElementAtIndex(i).objectReferenceValue==potion)){var i=pool.arraySize;pool.InsertArrayElementAtIndex(i);pool.GetArrayElementAtIndex(i).objectReferenceValue=potion;so.ApplyModifiedPropertiesWithoutUndo();}
        var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>("Assets/Editor/Arcade-ArtReferences.asset");
        var geometry=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/RunIcons/Perk_BrightEdge.png");
        for(var h=0;h<faces.heroes.Length;h++)for(var e=0;e<4;e++)
        {var property=$"heroes.Array.data[{h}].expressions.Array.data[{e}]";var entry=archive.entries.FirstOrDefault(x=>x.asset==faces&&x.property==property);if(entry==null)archive.entries.Add(new ArtReferenceArchive.Entry{asset=faces,property=property,arcade=faces.heroes[h].expressions[e],geometry=geometry});else entry.arcade=faces.heroes[h].expressions[e];}
        if(!archive.entries.Any(x=>x.asset==potion&&x.property=="icon"))archive.entries.Add(new ArtReferenceArchive.Entry{asset=potion,property="icon",arcade=potion.icon,geometry=geometry});EditorUtility.SetDirty(archive);
        BehemothTextEditor.SyncAsset(potion,true);File.WriteAllText(BehemothTextEditor.CsvPath,table.ExportCsv(),new System.Text.UTF8Encoding(true));
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art+"ArcadeFont.asset");font.TryAddCharacters(string.Concat(table.entries.Where(e=>e.key.StartsWith("ui.fatigue")||e.key.StartsWith("ui.camp")).Select(e=>e.kr)),out string _);foreach(var t in font.atlasTextures)t.filterMode=FilterMode.Point;EditorUtility.SetDirty(font);
        EditorUtility.SetDirty(hud);EditorUtility.SetDirty(hud.hover);EditorUtility.SetDirty(haze);
        content.fatigueRevision=1;EditorUtility.SetDirty(content);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!open)EditorSceneManager.CloseScene(scene,true);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Fatigue-Preparation.txt","Fatigue settings,24expression sprites, serialized32px HUD, camp, common potion; designer combat baselines preserved. "+DateTime.Now);
    }
    static EquipmentData Potion(GameContent content)
    {
        var potion=AssetDatabase.LoadAssetAtPath<EquipmentData>(Root+"Equipment/FatiguePotion.asset");
        if(potion==null)
        {
            potion=Asset<EquipmentData>(Root+"Equipment/FatiguePotion.asset");potion.id="FatiguePotion";potion.displayName=GameText.Get("ui.fatigue.potion.name");potion.description=GameText.Get("ui.fatigue.potion.detail");potion.itemKind=ShopItemKind.Potion;potion.scope=RelicScope.Shared;potion.price=24;potion.maxPurchaseCount=2;
            potion.icon=content.equipment.First(e=>e.id=="RagePotion").icon;
            potion.abilities.Add(new EquipmentAbility{trigger=new EquipmentTrigger{triggerType=TriggerType.OnConsume,triggerCount=1,chancePercent=100},target=TargetType.Owner,effects=new(){new EquipmentEffect{effectType=EffectType.RecoverFatigue,effectMagnitude=30,travelGraceSteps=10,effectCount=1}}});EditorUtility.SetDirty(potion);
        }
        foreach(var ability in potion.abilities)for(var index=0;index<ability.effects.Count;index++){var effect=ability.effects[index];if(effect.effectType==EffectType.RecoverFatigue&&effect.travelGraceSteps==0&&effect.effectTime>0){effect.travelGraceSteps=Mathf.RoundToInt(effect.effectTime);effect.effectTime=0;ability.effects[index]=effect;EditorUtility.SetDirty(potion);}}
        if(!content.equipment.Contains(potion))content.equipment.Add(potion);return potion;
    }
    static FatigueFaces ImportFaces()
    {
        var closeup=File.Exists(Art+"FatigueFacesCloseup.png");var path=Art+(closeup?"FatigueFacesCloseup.png":"FatigueFaces.png");AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Multiple;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.mipmapEnabled=false;i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=2048;i.spritePixelsPerUnit=32;
        var source=new Texture2D(2,2);ImageConversion.LoadImage(source,File.ReadAllBytes(path));var sourceHeight=source.height;var w=source.width/4;var h=source.height/6;Object.DestroyImmediate(source);
#pragma warning disable CS0618
        var centers=new[]{128,382,628,876,1122,1385};
        i.spritesheet=Enumerable.Range(0,24).Select(n=>new SpriteMetaData{name=$"Face_{n/4}_{n%4}",rect=closeup?new Rect(n%4*256+16,sourceHeight-centers[n/4]-112,224,224):new Rect(n%4*w,sourceHeight-(n/4+1)*h,w,h),alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f}).ToArray();
#pragma warning restore CS0618
        i.SaveAndReimport();var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();var faces=Asset<FatigueFaces>(Root+"Presentation/FatigueFaces.asset");
        faces.heroes=Enumerable.Range(0,6).Select(row=>new FatigueFaces.Hero{character=(CharacterKind)row,expressions=Enumerable.Range(0,4).Select(col=>sprites.Single(s=>s.name==$"Face_{row}_{col}")).ToArray()}).ToArray();EditorUtility.SetDirty(faces);return faces;
    }
    static IEnumerable<T> All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true));
    static GameObject Child(Transform parent,string name,params Type[] components){var t=parent.Find(name);if(t!=null)return t.gameObject;var go=new GameObject(name,components);go.transform.SetParent(parent,false);return go;}
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;r.localScale=Vector3.one;}
    static Image Box(Transform p,string n,float x,float y,float w,float h){var image=Child(p,n,typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(image.transform,x,y,w,h);image.color=Color.white;return image;}
}
