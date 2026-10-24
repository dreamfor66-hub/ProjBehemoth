using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class AshenVowArtPreparation
{
    private const string Art = "Assets/Art/AshenVow/";
    private static readonly Color Gold = new(.78f, .62f, .36f);
    private static readonly Color Paper = new(.88f, .84f, .73f);
    private static TMP_FontAsset pixel;
    private static Sprite frame;
    static AshenVowArtPreparation() => EditorApplication.delayCall += Prepare;
    internal static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Prepare; return; }
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(AshenVowGameplayPreparation.Root + "/GameContent.asset");
        if (content == null || content.artRevision >= 2) return;
        if (content.gameplayRevision < 1) { EditorApplication.delayCall += Prepare; return; }
        if (!File.Exists(Art + "Atlases/EventAtlas.png") || !File.Exists(Art + "Atlases/HeroAtlas.png")) return;
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) { EditorApplication.delayCall += Prepare; return; }
        try
        {
            pixel = MakeFont(); if (pixel == null) { EditorApplication.delayCall += Prepare; return; }
            frame = MakeFrame();
            var relic = Slice("RelicAtlas", 8, 8); var relicB = Slice("RelicAtlasB", 8, 8);
            var perk = Slice("PerkAtlas", 8, 8); var enemies = Slice("EnemyAtlas", 6, 6); var heroes = Slice("HeroAtlas", 3, 2); var events = Slice("EventAtlas", 4, 6);
            foreach (var name in new[] { "CastleCrypt", "Furnace", "FrozenCrypt" }) ImportSingle(Art + "Environments/" + name + ".png");
            var classes = new[] { CharacterKind.Warrior, CharacterKind.Rogue, CharacterKind.Barbarian, CharacterKind.Samurai, CharacterKind.Monk, CharacterKind.Wizard };
            var colors = new[] { new Color(.88f,.70f,.40f), new Color(.49f,.68f,.46f), new Color(.82f,.34f,.28f), new Color(.62f,.69f,.86f), new Color(.80f,.66f,.35f), new Color(.68f,.54f,.84f) };
            for (var i = 0; i < classes.Length; i++)
            {
                var character = content.characters.First(c => c.character == classes[i]);
                character.portrait = ImportSingle(Art + "Portraits/" + classes[i] + ".png"); character.combatSprite = heroes[i]; character.characterColor = colors[i]; EditorUtility.SetDirty(character);
            }
            var iconIndices = new[] { 30,31,32,33,34,35,36,37,38,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30 };
            for (var i = 0; i < content.perks.Count; i++) { content.perks[i].icon = perk[iconIndices[i]]; content.perks[i].abilitySource.icon = perk[iconIndices[i]]; EditorUtility.SetDirty(content.perks[i]); EditorUtility.SetDirty(content.perks[i].abilitySource); }
            var spellIcons = new Dictionary<string,int> { ["Ember"]=49,["EnergyBolt"]=50,["Spark"]=51,["FireBloom"]=52,["FrostRing"]=53,["ChainLightning"]=54,["PoisonMist"]=39,["EnchantFire"]=40,["EnchantIce"]=41,["EnchantLightning"]=42,["LightningMark"]=43,["Cleanse"]=44,["Meteor"]=45,["VortexLightning"]=46,["IcicleShower"]=47,["CrimsonInferno"]=48 };
            foreach (var spell in content.spells) { if (spellIcons.TryGetValue(spell.id, out var index)) spell.icon = perk[index]; EditorUtility.SetDirty(spell); }
            var itemIcons = new Dictionary<string,int> { ["Starter_Warrior"]=0,["Starter_Rogue"]=1,["Starter_Barbarian"]=2,["Starter_Samurai"]=3,["Starter_Monk"]=4,["Starter_Wizard"]=5,["GoldenTooth"]=8,["GuardianBell"]=9,["Hourglass"]=10,["MirrorShard"]=11,["AmberSeed"]=12,["BloodContract"]=13,["RaggedBanner"]=15,["TwinDaggers"]=16,["ViperFang"]=17,["IceFocus"]=20,["StormBottle"]=21,["IronFortress"]=26,["Spear"]=27,["Hammer"]=28,["AzureTalon"]=29,["CorePrism"]=31,["GuidingWisp"]=80,["Parasite"]=82,["PilgrimOath"]=83,["WellCurse"]=85,["ThornBulwark"]=76,["ThunderWire"]=77,["CrosswindCharm"]=73,["EdgeCharm"]=74,["HunterLantern"]=75,["DistanceMomentum"]=79,["ChargeCoil"]=70,["ChargeWard"]=71,["AegisBattery"]=68,["RagePotion"]=114,["RevivalPotion"]=115,["StoneSkinPotion"]=113 };
            var allIcons = relic.Concat(relicB).ToArray(); var used = new HashSet<int>(itemIcons.Values); var spare = new Queue<int>(Enumerable.Range(0, allIcons.Length).Where(i => !used.Contains(i)));
            foreach (var item in content.equipment)
            {
                if (item.grantedSpell != null) item.icon = item.grantedSpell.icon;
                else if (item.id == "StrayDog") item.icon = perk[57];
                else item.icon = allIcons[itemIcons.TryGetValue(item.id, out var i) ? i : spare.Count > 0 ? spare.Dequeue() : 63];
                EditorUtility.SetDirty(item);
            }
            var enemyNames = new[] { "AlarmHound","ArmoredExecutioner","AshCrow","AshGeneral","BellKeeper","BloodExecutioner","BroodKeeper","ChainBrother","Chainling","CinderImp","CinderPriest","ClockworkJudge","CrystalSpider","DeepTrialGuardian","EclipseBeast","ElderLeftHeart","ElderRightHeart","FurnaceGuard","FurnaceMite","IceMirror","IceNeedle","LampPriest","LeftHeart","MirrorKnight","OrbitBat","PaleWisp","RightHeart","SlagTitan","SnowBell","SoulLamp","StormMatriarch","StormOrb","TrainingLight","TrialGuardian","TunnelRat","TwinHeart" };
            for (var i = 0; i < enemyNames.Length; i++)
            { var data = AssetDatabase.LoadAssetAtPath<MonsterData>(AshenVowGameplayPreparation.Root + "/Monsters/" + enemyNames[i] + ".asset"); if (data != null) { data.portrait = enemies[i]; EditorUtility.SetDirty(data); } }
            foreach (var name in new[] { "TrainingMedium", "TrainingHeavy" }) { var data = AssetDatabase.LoadAssetAtPath<MonsterData>(AshenVowGameplayPreparation.Root + "/Monsters/" + name + ".asset"); data.portrait = enemies[32]; EditorUtility.SetDirty(data); }
            var eventNames = new[] { "StrayDog","Symbiosis","Mirror","Contract","Pilgrim","LostBeliever","Hourglass","StormBottle","AmberSeed","Bell","Banner","Tooth","Training","Scholar","Forge","Well","Investment","Delivery","MasterTrial","Shelter" };
            foreach (var story in content.events)
            { var index = Array.IndexOf(eventNames, story.id); if (index >= 0) { story.illustration = events[index]; EditorUtility.SetDirty(story); } }
            SeedText();
            foreach (var path in new[] { "Assets/Scenes/OutGameScene.unity", "Assets/Scenes/MainGameScene.unity" }) Upgrade(path, content);
            UpgradePrefabs();
            PlayerSettings.productName = "DEEP SIX";
            content.artRevision = 2; EditorUtility.SetDirty(content); BehemothTextEditor.SyncAll(); BehemothTextEditor.ExportDefault(); AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/AshenVow-ArtPreparation.txt", "Pixel portraits, sprites, item atlas, scene illustrations, Galmuri bitmap font, saved scene UI applied.");
        }
        catch (Exception error) { Debug.LogException(error); }
    }
    internal static Sprite[] Slice(string name, int columns, int rows)
    {
        var path = Art + "Atlases/" + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); Configure(importer); importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.GetSourceTextureWidthAndHeight(out var width, out var height);
        var rects = new List<SpriteMetaData>();
        for (var i = 0; i < columns * rows; i++)
        {
            var x = Mathf.RoundToInt(i % columns * width / (float)columns); var right = Mathf.RoundToInt((i % columns + 1) * width / (float)columns);
            var y = height - Mathf.RoundToInt((i / columns + 1) * height / (float)rows); var top = height - Mathf.RoundToInt(i / columns * height / (float)rows);
            rects.Add(new() { name = name + "_" + i.ToString("D2"), rect = new Rect(x,y,right-x,top-y), alignment = (int)SpriteAlignment.Center, pivot = Vector2.one * .5f });
        }
#pragma warning disable CS0618
        importer.spritesheet = rects.ToArray();
#pragma warning restore CS0618
        importer.SaveAndReimport(); return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
    private static void Configure(TextureImporter importer)
    { importer.textureType = TextureImporterType.Sprite; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096; importer.spritePixelsPerUnit = 100; }
    private static Sprite ImportSingle(string path)
    { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); var importer = (TextureImporter)AssetImporter.GetAtPath(path); Configure(importer); importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path); }
    private static TMP_FontAsset MakeFont()
    {
        const string path = "Assets/Art/Fonts/Resources/AshenVowPixel.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if (font != null) return font;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Galmuri/Galmuri11.ttf"); if (source == null) return null;
        font = TMP_FontAsset.CreateFontAsset(source, 16, 1, GlyphRenderMode.RASTER_HINTED, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        font.name = "AshenVowPixel"; AssetDatabase.CreateAsset(font, path);
        font.TryAddCharacters(new string((string.Concat(BehemothTextEditor.Table.entries.Select(e => e.kr)) + "DEEP SIX 딥 식스 MP 0123456789 +−%→←↑↓·").Distinct().ToArray()), out string missing);
        if (Shader.Find("TextMeshPro/Bitmap") != null) font.material.shader = Shader.Find("TextMeshPro/Bitmap");
        AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures) if (texture != null) { texture.filterMode = FilterMode.Point; if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font); }
        var settings = Resources.Load<TMP_Settings>("TMP Settings"); if (settings != null) { var so = new SerializedObject(settings); so.FindProperty("m_defaultFontAsset").objectReferenceValue = font; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings); }
        EditorUtility.SetDirty(font); return font;
    }
    private static Sprite MakeFrame()
    {
        var path = Art + "Frame.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(32,32,TextureFormat.RGBA32,false);
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
            {
                var edge = Mathf.Min(x,y,31-x,31-y);
                var c = edge == 0 ? new Color(.035f,.028f,.027f) : edge == 1 ? new Color(.37f,.28f,.17f) : edge == 2 ? Gold : edge == 3 ? new Color(.18f,.13f,.10f) : new Color(.047f,.043f,.045f,.97f);
                if (edge == 2 && (x+y)%7 == 0) c *= .7f;
                texture.SetPixel(x,y,c);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        }
        var sprite = ImportSingle(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.spriteBorder = new Vector4(6,6,6,6); importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void SeedText()
    {
        var table = BehemothTextEditor.Table;
        foreach (var entry in new Dictionary<string,string> { ["ui.title"]="DEEP SIX", ["ui.title.kr"]="딥 식스", ["ui.selection.subtitle"]="돌아올 수 없는 성, 누구의 서약을 잇겠는가", ["ui.spellbook.title"]="주문서", ["ui.spellbook.description"]="배운 주문은 남아 있습니다. 탐험 중 사용할 주문을 고르세요.", ["ui.common.close"]="닫기", ["ui.spell.slotcompact"]="{0} MP {1:0}\n{2}" }) table.Set(entry.Key,entry.Value,"AshenVow");
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static T Ref<T>(Object owner, string name) where T : Object => new SerializedObject(owner).FindProperty(name)?.objectReferenceValue as T;
    private static RectTransform Place(Transform target, Vector2 position, Vector2 size)
    { var rect=(RectTransform)target; rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f; rect.localScale=Vector3.one; rect.anchoredPosition=position; rect.sizeDelta=size; return rect; }
    private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    { var child=parent.Find(name); if(child==null){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);child=go.transform;} return Place(child,position,size); }
    private static Image Image(string name,Transform parent,Vector2 position,Vector2 size,Sprite sprite,Color color)
    { var root=Rect(name,parent,position,size);var image=root.GetComponent<Image>()??root.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;return image; }
    private static TextMeshProUGUI Text(string name,Transform parent,Vector2 position,Vector2 size,string text,int point)
    {var root=Rect(name,parent,position,size);var label=root.GetComponent<TextMeshProUGUI>()??root.gameObject.AddComponent<TextMeshProUGUI>();label.text=text;label.font=pixel;label.fontSize=point;label.color=Paper;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;return label;}
    private static void Upgrade(string path,GameContent content)
    {
        var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        foreach(var label in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TextMeshProUGUI>(true))){label.font=pixel;label.color=Paper;label.fontSize=Mathf.Max(16,Mathf.Round(label.fontSize/2)*2);}
        foreach(var button in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Button>(true)))ThemeButton(button);
        var canvas=Find<Canvas>(scene);var background=canvas.transform.Find("Background")?.GetComponent<Image>();
        if(background!=null){background.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Environments/CastleCrypt.png");background.color=new Color(.65f,.65f,.7f);background.preserveAspect=false;}
        var view=Find<OutGameView>(scene);if(view!=null)Selection(canvas,view);
        else Main(canvas,scene,content);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
    }
    private static void ThemeButton(Button button)
    {var image=button.GetComponent<Image>();if(image!=null){image.sprite=frame;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;}var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.88f,.64f);colors.pressedColor=new Color(.67f,.55f,.39f);colors.disabledColor=new Color(.43f,.41f,.40f);button.colors=colors;}
    private static void Selection(Canvas canvas,OutGameView view)
    {
        var center=canvas.transform.Find("SelectionCenter");Place(center,Vector2.zero,new Vector2(1360,820));var surface=center.GetComponent<Image>();if(surface!=null)surface.color=Color.clear;
        var accent=center.Find("TopAccent");if(accent!=null)accent.gameObject.SetActive(false);
        var title=center.Find("Title").GetComponent<TextMeshProUGUI>();Place(title.transform,new Vector2(-350,359),new Vector2(500,70));title.fontSize=54;title.color=Gold;var binding=title.GetComponent<GameTextBinding>();if(binding!=null){binding.key="ui.title";binding.Refresh();}else title.text="DEEP SIX";
        var subtitle=center.Find("Subtitle").GetComponent<TextMeshProUGUI>();Place(subtitle.transform,new Vector2(330,340),new Vector2(560,70));subtitle.fontSize=24;subtitle.alignment=TextAlignmentOptions.Center;
        var subtitleBinding=subtitle.GetComponent<GameTextBinding>();if(subtitleBinding!=null){subtitleBinding.key="ui.selection.subtitle";subtitleBinding.Refresh();}
        var kr=Text("KoreanTitle",center,new Vector2(-350,306),new Vector2(500,40),"딥 식스",24);kr.alignment=TextAlignmentOptions.Center;kr.color=Gold;
        var detail=center.Find("CharacterDetails");Place(detail,new Vector2(330,64),new Vector2(580,466));var panel=detail.GetComponent<Image>();panel.sprite=frame;panel.type=UnityEngine.UI.Image.Type.Sliced;panel.color=Color.white;
        view.portrait.transform.SetParent(center,false);Place(view.portrait.transform,new Vector2(-350,4),new Vector2(420,580));view.portrait.preserveAspect=true;
        Place(view.characterName.transform,new Vector2(0,190),new Vector2(526,44));view.characterName.fontSize=36;
        Place(view.characterDescription.transform,new Vector2(0,130),new Vector2(526,74));view.characterDescription.fontSize=22;view.characterDescription.fontSizeMin=20;view.characterDescription.fontSizeMax=22;
        Place(view.stats.transform,new Vector2(0,27),new Vector2(526,120));view.stats.fontSize=22;
        if(view.startingRelicScroll!=null){var scroll=view.startingRelicScroll;Place(scroll.transform,new Vector2(0,-130),new Vector2(526,145));scroll.viewport.sizeDelta=new Vector2(510,145);view.startingRelic.fontSize=22;}
        for(var i=0;i<view.characterButtons.Length;i++)
        {
            var button=view.characterButtons[i];button.transform.SetParent(center,false);Place(button.transform,new Vector2(-545+i*218,-337),new Vector2(204,78));ThemeButton(button);
            var portrait=button.transform.Find("Portrait")?.GetComponent<Image>();if(portrait!=null){Place(portrait.transform,new Vector2(-67,0),new Vector2(48,65));portrait.sprite=view.content.characters[i].portrait;portrait.color=Color.white;portrait.preserveAspect=true;}
            Place(view.characterLabels[i].transform,new Vector2(28,0),new Vector2(130,45));view.characterLabels[i].fontSize=24;
            Place(view.selectionMarks[i].transform,new Vector2(0,-35),new Vector2(188,3));
        }
        Place(view.startButton.transform,new Vector2(330,-224),new Vector2(580,66));view.startButton.GetComponentInChildren<TextMeshProUGUI>().fontSize=28;
        var controls=center.Find("Controls");Place(controls,new Vector2(0,-405),new Vector2(1300,66));controls.GetComponent<TextMeshProUGUI>().fontSize=18;
        view.Select(0);EditorUtility.SetDirty(view);
    }
    private static void Main(Canvas canvas,Scene scene,GameContent content)
    {
        var run=Find<RunController>(scene);var serialized=new SerializedObject(run);var perks=serialized.FindProperty("perks");perks.arraySize=content.perks.Count;for(var i=0;i<content.perks.Count;i++)perks.GetArrayElementAtIndex(i).objectReferenceValue=content.perks[i];serialized.ApplyModifiedPropertiesWithoutUndo();
        var expansion=Find<RunExpansionView>(scene);var modal=expansion.modal.GetComponent<Image>();if(modal!=null){modal.sprite=frame;modal.type=UnityEngine.UI.Image.Type.Sliced;modal.color=Color.white;}
        var combatSurface=Find<CombatSceneView>(scene).LogicalArea.GetComponent<Image>();if(combatSurface!=null)combatSurface.color=new Color(0,0,0,.07f);
        var stage=canvas.GetComponent<StageBackdropView>()??canvas.gameObject.AddComponent<StageBackdropView>();stage.run=run;stage.background=canvas.transform.Find("Background").GetComponent<Image>();stage.stages=new[]{"CastleCrypt","Furnace","FrozenCrypt"}.Select(n=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Environments/"+n+".png")).ToArray();
        var roomTransition=Find<RoomTransitionView>(scene);var walk=Ref<RectTransform>(roomTransition,"walkBackdrop");if(walk!=null)foreach(var graphic in walk.GetComponentsInChildren<Graphic>(true))graphic.color=Color.clear;
        var roomDither=Rect("RoomPixelTransition",canvas.transform,Vector2.zero,new Vector2(1600,900));var roomDitherGraphic=roomDither.GetComponent<PixelDitherTransition>()??roomDither.gameObject.AddComponent<PixelDitherTransition>();roomDitherGraphic.raycastTarget=false;
        var transitionData=new SerializedObject(roomTransition);transitionData.FindProperty("pixelTransition").objectReferenceValue=roomDitherGraphic;transitionData.ApplyModifiedPropertiesWithoutUndo();
        Place(expansion.modal.transform,Vector2.zero,new Vector2(570,838));var story=expansion.modal.transform.Find("StoryPanel");Place(story,Vector2.zero,new Vector2(546,810));var storySurface=story.GetComponent<Image>();if(storySurface!=null)storySurface.color=Color.clear;
        Place(expansion.modalTitle.transform,new Vector2(0,365),new Vector2(510,54));expansion.modalTitle.fontSize=30;expansion.modalTitle.color=Gold;
        Place(expansion.modalIllustration.transform,new Vector2(0,218),new Vector2(502,254));expansion.modalIllustration.preserveAspect=true;
        expansion.illustratedTextY=55;expansion.illustratedTextHeight=110;expansion.plainTextY=185;expansion.plainTextHeight=250;
        var dither=Rect("PixelTransition",expansion.modal.transform,Vector2.zero,new Vector2(570,838));expansion.transition=dither.GetComponent<PixelDitherTransition>()??dither.gameObject.AddComponent<PixelDitherTransition>();expansion.transition.raycastTarget=false;
        for(var i=0;i<expansion.choices.Length;i++)
        {var choice=expansion.choices[i];Place(choice.button.transform,new Vector2(0,-67-i*88),new Vector2(510,82));ThemeButton(choice.button);Place(choice.icon.transform,new Vector2(-220,0),new Vector2(48,48));Place(choice.title.transform,new Vector2(24,19),new Vector2(432,29));choice.title.fontSize=22;choice.title.color=Gold;Place(choice.description.transform,new Vector2(24,-17),new Vector2(432,41));choice.description.fontSize=18;}
        Place(expansion.previousPage.transform,new Vector2(-155,-378),new Vector2(118,32));Place(expansion.nextPage.transform,new Vector2(155,-378),new Vector2(118,32));Place(expansion.pageLabel.transform,new Vector2(0,-378),new Vector2(160,32));
        for(var i=0;i<expansion.spellButtons.Length;i++)
        {var button=expansion.spellButtons[i];button.GetComponent<RectTransform>().sizeDelta=new Vector2(142,60);Image("SpellIcon",button.transform,new Vector2(-43,0),new Vector2(38,38),null,Color.white);Place(expansion.spellLabels[i].transform,new Vector2(24,0),new Vector2(88,52));expansion.spellLabels[i].fontSize=16;}
        var hud=Rect("ManaAndSpellbook",canvas.transform,new Vector2(0,-385),new Vector2(470,48));var gauge=Image("ManaGauge",hud,new Vector2(-38,0),new Vector2(270,20),null,new Color(.035f,.035f,.09f));
        var fill=Image("Fill",gauge.transform,Vector2.zero,new Vector2(330,20),null,new Color(.30f,.46f,.78f));var label=Text("Value",gauge.transform,Vector2.zero,new Vector2(320,23),"MP 30/30",16);label.alignment=TextAlignmentOptions.Center;
        var bookImage=Image("Spellbook",hud,new Vector2(220,0),new Vector2(100,36),frame,Color.white);bookImage.type=UnityEngine.UI.Image.Type.Sliced;bookImage.raycastTarget=true;var book=bookImage.GetComponent<Button>()??bookImage.gameObject.AddComponent<Button>();book.targetGraphic=bookImage;ThemeButton(book);var bookText=Text("Label",book.transform,Vector2.zero,new Vector2(96,30),"주문서",18);bookText.alignment=TextAlignmentOptions.Center;
        for(var i=book.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEditor.Events.UnityEventTools.RemovePersistentListener(book.onClick,i);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(book.onClick,expansion.ShowSpellbook);
        var mana=hud.GetComponent<ManaHudView>()??hud.gameObject.AddComponent<ManaHudView>();mana.run=run;mana.fill=fill;mana.label=label;mana.gauge=gauge.gameObject;mana.spellbook=book;mana.combat=Find<CombatController>(scene);
        gauge.gameObject.SetActive(false);book.gameObject.SetActive(false);
        foreach(var image in canvas.GetComponentsInChildren<Image>(true).Where(i=>i.sprite==null&&i.color.r>.95f&&i.color.g>.95f&&i.color.b>.95f&&i.name!="Fill")){image.color=new Color(.09f,.08f,.08f);}
        foreach(var panel in canvas.GetComponentsInChildren<Image>(true).Where(i=>i.name.EndsWith("Panel")||i.name=="CharacterSummary")) {if(panel.sprite==null){panel.sprite=frame;panel.type=UnityEngine.UI.Image.Type.Sliced;panel.color=Color.white;}}
        EditorUtility.SetDirty(expansion);EditorUtility.SetDirty(run);
    }
    private static void UpgradePrefabs()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))text.font=pixel;foreach(var button in root.GetComponentsInChildren<Button>(true))ThemeButton(button);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
    }
}
