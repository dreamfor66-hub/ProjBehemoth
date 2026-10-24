using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class OutGameShellPreparation
{
    const string Profile="Assets/Data/Presentation/Arcade224.asset",ScenePath="Assets/Scenes/OutGameScene.unity";
    static readonly Color Ink=new Color32(11,12,22,255),Paper=new Color32(222,211,175,255),Gold=new Color32(216,187,98,255);
    static TMP_FontAsset font;static double next;
    static OutGameShellPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        if(profile==null||profile.feedbackRevision<1||profile.outgameRevision>=3||!File.Exists("Assets/Art/Arcade224/TitleLabyrinth.png"))return;
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    internal static void Prepare()
    {
        if(EditorApplication.isPlaying)return;
        var table=BehemothTextEditor.Table;table.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));table.Invalidate();EditorUtility.SetDirty(table);
        var scene=SceneManager.GetSceneByPath(ScenePath);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OutGameView>(true)).First();
        ImportPortraits(view.content);Configure(view);
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);profile.outgameRevision=3;EditorUtility.SetDirty(profile);PlayerSettings.productName=table.Get("ui.title.kr");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!open)EditorSceneManager.CloseScene(scene,true);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/OutGameShell-Preparation.txt","Title / Character selection / Options serialized. Full portrait cells; no forced palette conversion in OutGameScene.\n"+DateTime.Now);
    }
    internal static Sprite[] ImportPortraits(GameContent content)
    {
        const string path="Assets/Art/Arcade224/HeroFacePortraits.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=2048;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
        importer.GetSourceTextureWidthAndHeight(out var w,out var h);var names=new[]{"Warrior","Rogue","Barbarian","Samurai","Monk","Wizard"};
#pragma warning disable CS0618
        importer.spritesheet=names.SelectMany((n,i)=>new[]{new SpriteMetaData{name=n,rect=new Rect(i%3*w/3f+17,(1-i/3)*h/2f+25,384,576),alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f},new SpriteMetaData{name="Thumb_"+n,rect=new Rect(i%3*w/3f+49,(1-i/3)*h/2f+232,320,320),alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f}}).ToArray();
#pragma warning restore CS0618
        importer.SaveAndReimport();var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>("Assets/Editor/Arcade-ArtReferences.asset");
        for(var i=0;i<content.characters.Count;i++){var data=content.characters[i];data.portrait=sprites.Single(s=>s.name==names[i]);EditorUtility.SetDirty(data);foreach(var e in archive.entries.Where(e=>e.asset==data&&e.property=="portrait"))e.arcade=data.portrait;}EditorUtility.SetDirty(archive);return names.Select(n=>sprites.Single(s=>s.name==n)).ToArray();
    }
    static void SetLocalized(PlayerCombatData data,string property,string key)
    {
        var value=BehemothTextEditor.Table.Get(key);typeof(PlayerCombatData).GetField(property).SetValue(data,value);
        var binding=data.textKeys.FirstOrDefault(b=>b.propertyPath==property);if(binding==null){binding=new DataTextBinding{propertyPath=property};data.textKeys.Add(binding);}binding.key=key;binding.lastText=value;
    }
    static Sprite ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static GameObject Child(Transform parent,string name,params Type[] types){var t=parent.Find(name);if(t!=null)return t.gameObject;var g=new GameObject(name,types);g.transform.SetParent(parent,false);return g;}
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static TextMeshProUGUI Text(Transform parent,string name,string key,float x,float y,float w,float h,int size=11)
    {
        var text=Child(parent,name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();Place(text.transform,x,y,w,h);text.font=font;text.fontSize=size*4;text.enableAutoSizing=false;text.color=Paper;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.text=string.IsNullOrEmpty(key)?"":GameText.Get(key);
        if(!string.IsNullOrEmpty(key)){var binding=text.GetComponent<GameTextBinding>()??text.gameObject.AddComponent<GameTextBinding>();binding.key=key;}return text;
    }
    static Image Box(Transform parent,string name,float x,float y,float w,float h,Color color)
    {var image=Child(parent,name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(image.transform,x,y,w,h);image.sprite=null;image.type=Image.Type.Simple;image.color=color;image.raycastTarget=false;return image;}
    static Button Button(Transform parent,string name,string key,float x,float y,float w,float h)
    {
        var image=Box(parent,name,x,y,w,h,Ink);image.raycastTarget=true;var button=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color32(164,154,145,255);colors.selectedColor=colors.highlightedColor;colors.pressedColor=Gold;colors.fadeDuration=0;button.colors=colors;
        Text(button.transform,"Label",key,0,0,w-6,h-2);return button;
    }
    internal static void Configure(OutGameView view)
    {
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");font.TryAddCharacters(string.Concat(BehemothTextEditor.Table.entries.Where(e=>e.key.StartsWith("ui.outgame")||e.key.StartsWith("ui.title")||e.key.EndsWith(".selectionstory")||e.key.EndsWith(".selectionability")).Select(e=>e.kr)),out string _);foreach(var atlas in font.atlasTextures)atlas.filterMode=FilterMode.Point;EditorUtility.SetDirty(font);
        TMP_Settings.useModernHangulLineBreakingRules=true;EditorUtility.SetDirty(TMP_Settings.instance);
        var canvas=view.GetComponentInParent<Canvas>();var root=canvas.transform;var selection=root.Find("SelectionCenter");
        var menu=view.GetComponent<OutGameMenuView>()??view.gameObject.AddComponent<OutGameMenuView>();menu.selection=view;menu.characterPage=selection.gameObject;
        // Keep the existing inspectable selection page and detailed-stat overlay.
        foreach(var name in new[]{"Title","CharacterDetails","Previous","Next","PageNumber"}){var t=selection.Find(name);if(t!=null)t.gameObject.SetActive(false);}
        var background=root.Find("Background").GetComponent<Image>();
        const string path="Assets/Art/Arcade224/TitleLabyrinth.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        background.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);background.color=Color.white;background.preserveAspect=false;Place(background.transform,0,0,384,224);background.raycastTarget=false;background.transform.SetAsFirstSibling();
        const string materialPath="Assets/Art/Arcade224/LowColorBackground.mat";var backgroundMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(backgroundMaterial==null){backgroundMaterial=new Material(Shader.Find("UI/Behemoth/LowColorBackground"));AssetDatabase.CreateAsset(backgroundMaterial,materialPath);}background.material=backgroundMaterial;
        menu.selectionShade=Box(root,"SelectionShade",0,0,384,224,new Color(11/255f,12/255f,22/255f,.86f));menu.selectionShade.transform.SetSiblingIndex(1);
        var title=Child(root,"TitlePage",typeof(RectTransform));Place(title.transform,0,0,384,224);menu.titlePage=title;
        foreach(var old in new[]{"GameTitle","Subtitle","Rule"}){var t=title.transform.Find(old);if(t!=null)t.gameObject.SetActive(false);}
        var oldLogo=title.transform.Find("Logo");if(oldLogo!=null)oldLogo.gameObject.SetActive(false);
        var logo=Child(title.transform,"PixelLogo",typeof(RectTransform),typeof(CanvasRenderer),typeof(PixelWordmark)).GetComponent<PixelWordmark>();if(logo.GetComponent<CanvasRenderer>()==null)logo.gameObject.AddComponent<CanvasRenderer>();Place(logo.transform,0,60,256,56);logo.word="DEEP SIX";logo.pixelSize=20;logo.raycastTarget=false;logo.SetAllDirty();
        logo.presentation=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        Text(title.transform,"KoreanTitle","ui.title.kr",0,26,240,16).color=Gold;
        menu.newGame=Button(title.transform,"NewGame","ui.outgame.start",0,-3,144,18);
        menu.options=Button(title.transform,"Options","ui.outgame.options",0,-27,144,18);
        menu.quit=Button(title.transform,"Quit","ui.outgame.quit",0,-51,144,18);
        foreach(var button in new[]{menu.newGame,menu.options,menu.quit}){var focus=button.GetComponent<MenuChoiceFocus>()??button.gameObject.AddComponent<MenuChoiceFocus>();focus.label=button.GetComponentInChildren<TextMeshProUGUI>();var marker=Text(button.transform,"Focus","ui.outgame.next",-66,0,12,18);marker.color=Gold;focus.marker=marker.gameObject;marker.gameObject.SetActive(button==menu.newGame);}
        var pageTitle=selection.Find("PageTitle");if(pageTitle!=null)pageTitle.gameObject.SetActive(false);
        menu.back=Button(selection,"Back","ui.outgame.back",166,100,42,18);
        var portraits=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Arcade224/HeroFacePortraits.png").OfType<Sprite>().ToArray();
        for(var i=0;i<view.characterButtons.Length;i++)
        {
            var button=view.characterButtons[i];Place(button.transform,-140+i*56,-72,42,42);button.image.sprite=null;button.image.type=Image.Type.Simple;button.image.color=i==0?Gold:Ink;var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.8f,.8f,.8f);button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};
            var picture=button.transform.Find("Portrait").GetComponent<Image>();picture.sprite=portraits.Single(s=>s.name=="Thumb_"+view.content.characters[i].character);picture.color=Color.white;picture.preserveAspect=true;picture.raycastTarget=false;Place(picture.transform,0,0,40,40);
            view.characterLabels[i].gameObject.SetActive(false);view.selectionMarks[i].gameObject.SetActive(false);button.gameObject.SetActive(true);
        }
        view.portrait.gameObject.SetActive(true);Place(view.portrait.transform,-130,22,96,144);view.portrait.preserveAspect=true;view.portrait.raycastTarget=false;
        view.characterName.gameObject.SetActive(true);Place(view.characterName.transform,58,80,244,28);view.characterName.fontSize=88;view.characterName.enableAutoSizing=false;view.characterName.alignment=TextAlignmentOptions.Left;
        view.selectionStory=Text(selection,"Story","",58,28,244,72);view.selectionStory.alignment=TextAlignmentOptions.TopLeft;view.selectionStory.color=Paper;
        view.compactDescription.gameObject.SetActive(false);Place(view.compactDescription.transform,70,26,218,28);view.compactDescription.fontSize=44;view.compactDescription.enableAutoSizing=false;view.compactDescription.alignment=TextAlignmentOptions.Left;
        Place(view.selectionStats.transform,58,-13,244,16);view.selectionStats.fontSize=44;view.selectionStats.alignment=TextAlignmentOptions.Left;
        var relicRow=Box(selection,"StartingRelicHover",58,-36,244,18,new Color32(28,27,41,255));relicRow.raycastTarget=true;
        view.selectionRelicIcon.transform.SetParent(relicRow.transform,false);Place(view.selectionRelicIcon.transform,-112,0,16,16);view.selectionRelicIcon.raycastTarget=false;
        view.selectionRelic.transform.SetParent(relicRow.transform,false);Place(view.selectionRelic.transform,12,0,212,18);view.selectionRelic.alignment=TextAlignmentOptions.Left;view.selectionRelic.fontSize=44;view.selectionRelic.raycastTarget=false;
        var popup=Box(root,"SideDescription",0,0,154,100,Ink);if(popup.GetComponent<CanvasGroup>()==null)popup.gameObject.AddComponent<CanvasGroup>();var group=popup.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var outline=popup.GetComponent<Outline>()??popup.gameObject.AddComponent<Outline>();outline.effectColor=Gold;outline.effectDistance=new Vector2(4,-4);
        var popupTitle=Text(popup.transform,"Heading","",0,30,140,16);popupTitle.color=Gold;popupTitle.alignment=TextAlignmentOptions.Left;
        var popupBody=Text(popup.transform,"Description","",0,-10,140,64);popupBody.alignment=TextAlignmentOptions.TopLeft;popupBody.overflowMode=TextOverflowModes.Overflow;
        var tooltip=root.GetComponent<ItemTooltipView>()??root.gameObject.AddComponent<ItemTooltipView>();tooltip.expandToFit=true;tooltip.Configure(popup.gameObject,popupTitle,popupBody,(RectTransform)root);
        view.startingRelicHover=relicRow.GetComponent<HoverTooltipTrigger>()??relicRow.gameObject.AddComponent<HoverTooltipTrigger>();view.startingRelicHover.view=tooltip;view.startingRelicHover.anchor=relicRow.rectTransform;
        Place(view.startButton.transform,123,-103,126,18);var startLabel=view.startButton.GetComponentInChildren<TextMeshProUGUI>();Place(startLabel.transform,0,0,122,16);startLabel.alignment=TextAlignmentOptions.Center;startLabel.fontSize=44;
        var details=selection.Find("Details");Place(details,-112,-103,88,18);var detailLabel=details.GetComponentInChildren<TextMeshProUGUI>();Place(detailLabel.transform,0,0,84,16);detailLabel.fontSize=44;detailLabel.text=GameText.Get("ui.outgame.details");var detailBinding=detailLabel.GetComponent<GameTextBinding>();if(detailBinding!=null)detailBinding.key="ui.outgame.details";
        menu.detailHeading=Text(view.detailsPanel.transform,"Heading","",0,80,300,22,22);view.detailsPanel.transform.SetAsLastSibling();view.detailsPanel.SetActive(false);
        var options=Child(root,"OptionsPage",typeof(RectTransform));Place(options.transform,0,0,384,224);menu.optionsPage=options;
        Box(options.transform,"Panel",0,0,330,202,Ink);Text(options.transform,"Title","ui.outgame.options",0,83,300,26,22);
        Text(options.transform,"VolumeLabel","ui.outgame.volume",-92,43,110,18);menu.volumeValue=Text(options.transform,"VolumeValue","",118,43,48,18);
        var sliderRoot=Child(options.transform,"Volume",typeof(RectTransform),typeof(Slider));Place(sliderRoot.transform,27,43,116,20);menu.volume=sliderRoot.GetComponent<Slider>();menu.volume.minValue=0;menu.volume.maxValue=1;
        var track=Box(sliderRoot.transform,"Track",0,0,110,3,new Color32(75,74,96,255));track.raycastTarget=true;
        var fillArea=Child(sliderRoot.transform,"FillArea",typeof(RectTransform));Place(fillArea.transform,0,0,110,0);var fill=Box(fillArea.transform,"Fill",0,0,0,3,Gold);menu.volume.fillRect=fill.rectTransform;
        var handleArea=Child(sliderRoot.transform,"HandleArea",typeof(RectTransform));Place(handleArea.transform,0,0,110,0);var handle=Box(handleArea.transform,"Handle",0,0,5,13,Paper);handle.raycastTarget=true;menu.volume.handleRect=handle.rectTransform;menu.volume.targetGraphic=handle;menu.volume.direction=Slider.Direction.LeftToRight;
        Text(options.transform,"FullscreenLabel","ui.outgame.fullscreen",-92,7,110,18);menu.fullscreen=Button(options.transform,"Fullscreen","",70,7,136,22);menu.fullscreenValue=menu.fullscreen.GetComponentInChildren<TextMeshProUGUI>();
        Text(options.transform,"ScaleLabel","ui.outgame.scale",-92,-29,110,18);menu.windowScale=Button(options.transform,"WindowScale","",70,-29,136,22);menu.scaleValue=menu.windowScale.GetComponentInChildren<TextMeshProUGUI>();
        Text(options.transform,"ShakeLabel","ui.outgame.shake",-92,-65,110,18);menu.screenShake=Button(options.transform,"ScreenShake","",70,-65,136,22);menu.shakeValue=menu.screenShake.GetComponentInChildren<TextMeshProUGUI>();
        menu.optionsBack=Button(options.transform,"Back","ui.outgame.back",0,-91,100,18);
        var binding=root.GetComponent<PresentationImageBindings>();if(binding!=null){foreach(var b in binding.bindings){if(b.image==background){b.arcade=background.sprite;b.color=Color.white;b.overrideMaterial=true;b.arcadeMaterial=backgroundMaterial;}if(b.image==view.portrait){b.arcade=view.content.characters[0].portrait;b.color=Color.white;}for(var i=0;i<view.characterButtons.Length;i++){if(b.image==view.characterButtons[i].image){b.arcade=null;b.color=Ink;}if(b.image==view.characterButtons[i].transform.Find("Portrait").GetComponent<Image>()){b.arcade=b.image.sprite;b.color=Color.white;}}}EditorUtility.SetDirty(binding);}
        foreach(var camera in root.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ArcadeRasterCamera>(true))){camera.quantizeColors=false;EditorUtility.SetDirty(camera);}
        if(File.Exists("Assets/Art/Arcade224/SelectionWizard.png"))FeedbackRevision3Preparation.ConfigureSelection(view);
        view.Select(0);menu.detailHeading.text=view.content.characters[0].displayName;
        selection.gameObject.SetActive(false);options.SetActive(false);title.SetActive(true);menu.selectionShade.gameObject.SetActive(false);EditorUtility.SetDirty(menu);EditorUtility.SetDirty(view);
        if(File.Exists("Assets/Art/Arcade224/DungeonMortalisLogo.png"))MortalisPreparation.ConfigureTitle(view);
        if(BehemothTextEditor.Table.Contains("ui.selection.ability.stats"))ReadabilityPreparation.ConfigureSelection(view);
    }
}
