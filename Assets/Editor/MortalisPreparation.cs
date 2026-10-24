using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class MortalisPreparation
{
    const string Profile = "Assets/Data/Presentation/Arcade224.asset";
    static readonly Color Ink = new Color32(11,12,22,255), Paper = new Color32(222,211,175,255), Gold = new Color32(216,187,98,255);
    static double next;
    static MortalisPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next) return; next = EditorApplication.timeSinceStartup + 5;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var p = AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        if (p == null || p.mortalisRevision >= 1 || !File.Exists("Assets/Art/Arcade224/DungeonMortalisLogo.png") || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) return;
        try { Prepare(); } catch (Exception e) { next = double.MaxValue; Debug.LogException(e); }
    }
    internal static void Prepare()
    {
        if (EditorApplication.isPlaying) return;
        var table = BehemothTextEditor.Table; table.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach (var pair in new[] { ("ui.title","Dungeon Mortalis"), ("ui.title.kr","Dungeon Mortalis"), ("ui.title.fallback","Dungeon\nMortalis"),
            ("ui.potion.drink","마시기"), ("ui.potion.throw","던지기"), ("ui.potion.discard","버리기"), ("ui.potion.cancel","취소"), ("ui.potion.target","던질 적을 선택하세요"),
            ("data.equipmentdata.openingrockpotion.description","던지기: 적 하나에게 26 피해"),
            ("data.equipmentdata.poisonpotion.displayname","독 포션"), ("data.equipmentdata.poisonpotion.description","던지기: 적에게 독 3 부여 (8초)\n마시기: 자신에게 독 3 부여 (8초)\n독은 2초마다 중첩당 3 피해") }) table.Set(pair.Item1, pair.Item2, "Dungeon Mortalis revision");
        table.Invalidate(); EditorUtility.SetDirty(table); File.WriteAllText(BehemothTextEditor.CsvPath, table.ExportCsv(), new System.Text.UTF8Encoding(true));
        OutGameShellPreparation.Prepare();
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/MainGameScene.unity"); var open = scene.IsValid() && scene.isLoaded;
        if (!open) scene = EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity", OpenSceneMode.Additive);
        var run = All<RunController>(scene).First(); var battle = All<CombatController>(scene).First();
        var stone = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/DungeonSlashPrototype/Equipment/OpeningRockPotion.asset");
        foreach (var ability in stone.abilities) ability.trigger.triggerType = TriggerType.OnThrow;
        // Existing magnitude stays designer-authored; only delivery changes from pre-consume to aimed throw.
        foreach (var ability in stone.abilities) for (var i=0;i<ability.effects.Count;i++) { var e=ability.effects[i]; e.targets=AbilityTarget.ContextTarget; ability.effects[i]=e; }
        BehemothTextEditor.SyncAsset(stone, true); EditorUtility.SetDirty(stone);
        var path = "Assets/Data/Behemoth/Equipment/PoisonPotion.asset"; Directory.CreateDirectory(Path.GetDirectoryName(path));
        var poison = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
        if (poison == null)
        {
            poison = ScriptableObject.CreateInstance<EquipmentData>(); poison.id="PoisonPotion"; poison.displayName=table.Get("data.equipmentdata.poisonpotion.displayname"); poison.description=table.Get("data.equipmentdata.poisonpotion.description");
            poison.itemKind=ShopItemKind.Potion; poison.price=20; poison.maxPurchaseCount=2; poison.icon=PoisonIcon();
            foreach (var trigger in new[]{TriggerType.OnConsume,TriggerType.OnThrow}) poison.abilities.Add(new EquipmentAbility {
                trigger=new EquipmentTrigger {triggerType=trigger,triggerCount=1,triggerMaxCount=1,chancePercent=100},
                target=trigger==TriggerType.OnConsume?TargetType.Owner:TargetType.Enemy,
                effects=new(){new EquipmentEffect{effectType=EffectType.ApplyStatus,effectMagnitude=3,effectCount=1,status=StatusKind.Poison,statusDamage=3,effectTime=8,statusCap=60,targets=AbilityTarget.ContextTarget}} });
            AssetDatabase.CreateAsset(poison,path); BehemothTextEditor.SyncAsset(poison,true);
        }
        foreach(var binding in poison.textKeys.Where(b=>b.propertyPath is "displayName" or "description"))
        {binding.key="data.equipmentdata.poisonpotion."+binding.propertyPath.ToLowerInvariant();binding.lastText=table.Get(binding.key);table.entries.First(e=>e.key==binding.key).source=path+":"+binding.propertyPath;}
        table.entries.RemoveAll(e=>e.key.StartsWith("data.equipmentdata.poisonpotion_")&&e.source.StartsWith(path+":"));table.Invalidate();EditorUtility.SetDirty(poison);EditorUtility.SetDirty(table);
        File.WriteAllText(BehemothTextEditor.CsvPath,table.ExportCsv(),new System.Text.UTF8Encoding(true));
        if(!battle.Content.equipment.Contains(poison))battle.Content.equipment.Add(poison); EditorUtility.SetDirty(battle.Content);
        var so=new SerializedObject(run);var items=so.FindProperty("equipment");if(!Enumerable.Range(0,items.arraySize).Any(i=>items.GetArrayElementAtIndex(i).objectReferenceValue==poison)){items.InsertArrayElementAtIndex(items.arraySize);items.GetArrayElementAtIndex(items.arraySize-1).objectReferenceValue=poison;so.ApplyModifiedPropertiesWithoutUndo();}EditorUtility.SetDirty(run);
        var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>("Assets/Editor/Arcade-ArtReferences.asset");
        if(!archive.entries.Any(e=>e.asset==poison&&e.property=="icon"))archive.entries.Add(new ArtReferenceArchive.Entry{asset=poison,property="icon",arcade=poison.icon});EditorUtility.SetDirty(archive);
        ConfigurePotions(scene);
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");font.TryAddCharacters(string.Concat(table.entries.Where(e=>e.key.StartsWith("ui.potion")||e.key.Contains("poisonpotion")||e.key=="ui.title").Select(e=>e.kr)),out string _);foreach(var t in font.atlasTextures)t.filterMode=FilterMode.Point;EditorUtility.SetDirty(font);
        PlayerSettings.productName="Dungeon Mortalis";
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);profile.mortalisRevision=1;EditorUtility.SetDirty(profile);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!open)EditorSceneManager.CloseScene(scene,true);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Mortalis-Preparation.txt","Dungeon Mortalis title, shared potion actions, aimed throw, poison potion serialized. "+DateTime.Now);
    }
    internal static void ConfigureTitle(OutGameView view)
    {
        var menu=view.GetComponent<OutGameMenuView>();var page=menu.titlePage.transform;
        var black=Box(page,"BlackBackdrop",0,0,384,224,Color.black);black.transform.SetAsFirstSibling();
        foreach(var name in new[]{"PixelLogo","KoreanTitle","Logo"}){var old=page.Find(name);if(old!=null)old.gameObject.SetActive(false);}
        const string path="Assets/Art/Arcade224/DungeonMortalisLogo.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
#pragma warning disable CS0618
        importer.spritesheet=new[]{new SpriteMetaData{name="Dungeon Mortalis",rect=new Rect(64,272,1408,480),alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f}};
#pragma warning restore CS0618
        importer.SaveAndReimport();
        var image=Box(page,"MortalisLogo",0,48,304,104,Color.white);image.sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();image.preserveAspect=true;
        image.material=LogoMaterial();
        var fallback=Text(page,"MortalisTitleFallback",0,48,320,100,22);fallback.text=GameText.Get("ui.title.fallback");fallback.textWrappingMode=TextWrappingModes.Normal;
        var key=fallback.GetComponent<GameTextBinding>()??fallback.gameObject.AddComponent<GameTextBinding>();key.key="ui.title.fallback";
        var style=page.GetComponent<ArtTitleView>()??page.gameObject.AddComponent<ArtTitleView>();style.presentation=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);style.illustration=image.gameObject;style.fallback=fallback;fallback.gameObject.SetActive(!style.presentation.arcade);EditorUtility.SetDirty(style);
        Place(menu.newGame.transform,0,-24,144,18);Place(menu.options.transform,0,-48,144,18);Place(menu.quit.transform,0,-72,144,18);
        var bindings=view.GetComponent<PresentationImageBindings>();if(!bindings.bindings.Any(b=>b.image==image))bindings.bindings=bindings.bindings.Append(new PresentationImageBindings.Binding{image=image,arcade=image.sprite,color=Color.white,overrideMaterial=true,arcadeMaterial=image.material}).ToArray();
        EditorUtility.SetDirty(bindings);EditorUtility.SetDirty(menu);
    }
    static Material LogoMaterial()
    {
        const string path="Assets/Art/Arcade224/DungeonMortalisLogo.mat",palettePath="Assets/Art/Arcade224/DungeonMortalisLogoPalette.asset";
        var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
        if(palette==null){palette=new Texture2D(32,1,TextureFormat.RGBA32,false,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};AssetDatabase.CreateAsset(palette,palettePath);}
        var colors=new Color32[]{new(0,0,0,255),new(46,16,5,255),new(88,29,6,255),new(137,55,7,255),new(194,103,10,255),new(228,158,25,255),new(247,205,83,255),new(255,239,166,255)};
        palette.SetPixels32(Enumerable.Range(0,32).Select(i=>colors[i%8]).ToArray());palette.Apply();EditorUtility.SetDirty(palette);
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("UI/Behemoth/SelectionPalette"));AssetDatabase.CreateAsset(material,path);}material.SetTexture("_Palette",palette);EditorUtility.SetDirty(material);return material;
    }
    static Sprite PoisonIcon()
    {
        const string path="Assets/Art/Arcade224/PoisonPotionIcon.asset";var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(existing!=null)return existing;
        var tex=new Texture2D(16,16,TextureFormat.RGBA32,false){name="PoisonPotion",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var pixels=new Color32[256];
        for(var y=2;y<=10;y++)for(var x=4;x<=11;x++)if((x>4&&x<11)||(y>3&&y<9))pixels[y*16+x]=y<8?new Color32(71,151,79,255):new Color32(47,78,70,255);
        for(var y=10;y<14;y++)for(var x=6;x<10;x++)pixels[y*16+x]=y==13?new Color32(169,112,64,255):new Color32(132,178,159,255);
        for(var y=4;y<9;y++)pixels[y*16+5]=new Color32(169,210,148,255);
        pixels[6*16+7]=pixels[6*16+9]=pixels[5*16+8]=new Color32(11,12,22,255);tex.SetPixels32(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);var sprite=Sprite.Create(tex,new Rect(0,0,16,16),Vector2.one*.5f,16);sprite.name="PoisonPotion";AssetDatabase.AddObjectToAsset(sprite,tex);return sprite;
    }
    static void ConfigurePotions(Scene scene)
    {
        var quick=All<QuickInventoryView>(scene).First();var root=quick.transform.parent;
        var overlay=Box(root,"PotionActions",0,0,384,224,new Color(0,0,0,.04f));overlay.raycastTarget=true;
        var view=overlay.GetComponent<PotionActionView>()??overlay.gameObject.AddComponent<PotionActionView>();
        view.run=quick.run;view.combat=All<CombatController>(scene).First();view.drawers=All<ArcadeHudController>(scene).First();view.expansion=All<RunExpansionView>(scene).First();view.tooltip=quick.tooltip;
        view.menu=Box(view.transform,"Menu",0,44,112,96,Ink).rectTransform;view.menu.GetComponent<Image>().raycastTarget=true;
        var line=view.menu.GetComponent<Outline>()??view.menu.gameObject.AddComponent<Outline>();line.effectColor=Gold;line.effectDistance=new Vector2(4,-4);
        view.heading=Text(view.menu,"Heading",0,34,104,18,11);view.heading.color=Gold;
        view.drink=Button(view.menu,"Drink","ui.potion.drink",0,10,104,20);view.throwButton=Button(view.menu,"Throw","ui.potion.throw",0,-12,104,20);view.discard=Button(view.menu,"Discard","ui.potion.discard",0,-34,104,20);
        view.cancel=Button(view.transform,"Cancel","ui.potion.cancel",158,98,52,18);
        view.prompt=Text(view.transform,"Prompt",0,88,260,18,11);view.prompt.color=Gold;
        view.bottle=Box(view.transform,"Bottle",0,0,16,16,Color.white);
        view.targetRing=Box(view.transform,"Target",0,0,36,36,Gold);view.targetRing.sprite=TargetIcon();
        quick.potionActions=view;EditorUtility.SetDirty(quick);EditorUtility.SetDirty(view);overlay.gameObject.SetActive(false);
    }
    static Sprite TargetIcon()
    {
        const string path="Assets/Art/Arcade224/PotionTargetIcon.asset";var found=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(found!=null)return found;
        var tex=new Texture2D(36,36,TextureFormat.RGBA32,false){name="Potion target brackets",filterMode=FilterMode.Point};var pixels=new Color32[36*36];
        for(var y=0;y<36;y++)for(var x=0;x<36;x++)if((x<2||x>33)&&(y<9||y>26)||(y<2||y>33)&&(x<9||x>26))pixels[y*36+x]=new Color32(255,255,255,255);
        tex.SetPixels32(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);var sprite=Sprite.Create(tex,new Rect(0,0,36,36),Vector2.one*.5f,36);sprite.name="Potion target brackets";AssetDatabase.AddObjectToAsset(sprite,tex);return sprite;
    }
    static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    static GameObject Child(Transform parent,string name,params Type[] types){var t=parent.Find(name);if(t!=null)return t.gameObject;var g=new GameObject(name,types);g.transform.SetParent(parent,false);return g;}
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c){var i=Child(p,n,typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(i.transform,x,y,w,h);i.color=c;i.raycastTarget=false;return i;}
    static TextMeshProUGUI Text(Transform p,string n,float x,float y,float w,float h,int size){var t=Child(p,n,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();Place(t.transform,x,y,w,h);t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");t.fontSize=size*4;t.color=Paper;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.NoWrap;return t;}
    static Button Button(Transform p,string n,string key,float x,float y,float w,float h){var i=Box(p,n,x,y,w,h,Ink);i.raycastTarget=true;var b=i.GetComponent<Button>()??i.gameObject.AddComponent<Button>();b.targetGraphic=i;var c=b.colors;c.highlightedColor=Gold;c.selectedColor=Color.white;c.disabledColor=new Color(.3f,.3f,.35f);c.fadeDuration=0;b.colors=c;var text=Text(i.transform,"Label",0,0,w-4,h-2,11);text.text=GameText.Get(key);var binding=text.GetComponent<GameTextBinding>()??text.gameObject.AddComponent<GameTextBinding>();binding.key=key;b.targetGraphic=text;return b;}
}


