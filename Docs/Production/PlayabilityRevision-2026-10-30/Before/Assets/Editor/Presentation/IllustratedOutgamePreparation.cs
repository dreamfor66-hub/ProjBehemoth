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
using Object=UnityEngine.Object;

// One-time serialized migration; opening the project prepares approved complete artwork.
[InitializeOnLoad]
internal static class IllustratedOutgamePreparation
{
    internal const string Report="Docs/Production/OutgameIllustrated-2026-10-29/";
    internal const string Art="Assets/Art/UI/Outgame/";
    const string Stamp="Assets/Data/Presentation/OutgameIllustratedInstalled.txt";
    const string Revision="narrative-selection-3";
    static double next;
    static IllustratedOutgamePreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+4;
        if(!File.Exists(Report+"Ready.txt")||(File.Exists(Stamp)&&File.ReadAllText(Stamp)==Revision))return;
        var files=Enum.GetNames(typeof(CharacterKind)).Select(n=>Art+n+"-Illustration.png").Concat(Enum.GetNames(typeof(CharacterKind)).Select(n=>Art+n+"-Button.png")).Concat(new[]{Art+"TitleBackdrop.png",Art+"TitleLogo.png"}).ToArray();
        if(files.Any(p=>!File.Exists(p)))return;
        try{Prepare(files);File.WriteAllText(Stamp,Revision);AssetDatabase.ImportAsset(Stamp);}
        catch(Exception e){File.WriteAllText(Report+"PreparationError.txt",e.ToString());next=EditorApplication.timeSinceStartup+60;Debug.LogException(e);}
    }
    static T Asset<T>(string p)where T:Object=>AssetDatabase.LoadAssetAtPath<T>(p);
    static void Import(string p)
    {
        AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var t=(TextureImporter)AssetImporter.GetAtPath(p);
        t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
        t.filterMode=FilterMode.Bilinear;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;
        t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;
        var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(s);t.SaveAndReimport();
    }
    static RectTransform Move(Component c,Transform parent,Vector2 position,Vector2 size)
    {
        var r=(RectTransform)c.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;c.gameObject.SetActive(true);return r;
    }
    static RectTransform Node(string n,Transform p)
    {var old=p.Find(n);if(old!=null)return (RectTransform)old;var g=new GameObject(n,typeof(RectTransform));g.layer=p.gameObject.layer;g.transform.SetParent(p,false);return (RectTransform)g.transform;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    static Image Image(string n,Transform p,Sprite sprite,Color color)
    {var r=Node(n,p);r.gameObject.SetActive(true);var i=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.material=null;i.raycastTarget=false;return i;}
    static void Font(TextMeshProUGUI t,float size,Color color)
    {
        t.enableAutoSizing=false;t.fontSize=size;t.color=color;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;
        var role=t.GetComponent<TypographyRoleBinding>();if(role!=null){role.designSize=size;role.snapSize=false;role.Apply();}
    }
    static TextMeshProUGUI Label(string n,Transform p,TextMeshProUGUI model,string key,Vector2 pos,Vector2 size,float font)
    {
        var r=Node(n,p);var t=r.GetComponent<TextMeshProUGUI>()??r.gameObject.AddComponent<TextMeshProUGUI>();t.font=model.font;t.alignment=TextAlignmentOptions.MidlineLeft;
        var b=t.GetComponent<GameTextBinding>()??t.gameObject.AddComponent<GameTextBinding>();b.key=key;t.text=GameText.Get(key);Font(t,font,new Color32(201,184,140,255));Move(t,p,pos,size);return t;
    }
    static void Skin(Button button,InkUiStyle style)
    {
        foreach(var skin in button.GetComponentsInChildren<InkUiSurface>(true))skin.gameObject.SetActive(false);
        var r=Node("Illustrated Ink",button.transform);r.gameObject.SetActive(true);Stretch(r);r.SetAsFirstSibling();var skinNew=r.GetComponent<InkUiSurface>()??r.gameObject.AddComponent<InkUiSurface>();
        skinNew.source=button.image;skinNew.control=button;skinNew.style=style;skinNew.role=InkSurfaceRole.Button;skinNew.material=style.material;skinNew.raycastTarget=false;
        var feedback=button.GetComponent<InkUiFeedback>()??button.gameObject.AddComponent<InkUiFeedback>();feedback.surface=skinNew;
        button.transition=Selectable.Transition.ColorTint;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.3f,1.2f,1.1f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.7f,.6f,.5f);button.colors=colors;
    }
    static void Prepare(string[] files)
    {
        var path="Assets/Scenes/OutGameScene.unity";var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved OutGameScene preserved; migration is pending.");
        foreach(var f in files)Import(f);
        const string profilePath="Assets/Data/Presentation/IllustratedOutgame.asset";
        var profile=Asset<ArcadePresentation>(profilePath);
        if(profile==null){profile=Object.Instantiate(Asset<ArcadePresentation>("Assets/Data/Presentation/Angular/WorldArtwork.asset"));AssetDatabase.CreateAsset(profile,profilePath);}
        profile.arcade=true;profile.width=1920;profile.height=1080;profile.preserveIllustrationColors=true;EditorUtility.SetDirty(profile);
        if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var roots=scene.GetRootGameObjects();var menu=roots.SelectMany(r=>r.GetComponentsInChildren<OutGameMenuView>(true)).Single();var view=menu.selection;
            var page=menu.characterPage.transform;var title=menu.titlePage.transform;var canvas=view.GetComponentInParent<Canvas>();
            canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1536,864);
            foreach(var camera in roots.SelectMany(r=>r.GetComponentsInChildren<ArcadeRasterCamera>(true))){camera.presentation=profile;camera.quantizeColors=false;camera.preserveNativeUiColors=true;}
            view.presentation=profile;view.illustratedSelection=true;view.compact=true;
            var ink=Asset<InkUiStyle>("Assets/Data/Presentation/InkUi.asset");var bone=new Color32(224,212,182,255);
            var oldTitle=title.Cast<Transform>().ToArray();var oldPage=page.Cast<Transform>().ToArray();
            var backdrop=Image("Illustrated Title Backdrop",title,Asset<Sprite>(Art+"TitleBackdrop.png"),Color.white);Stretch(backdrop.rectTransform);backdrop.transform.SetAsFirstSibling();
            var logo=Image("Illustrated Title Logo",title,Asset<Sprite>(Art+"TitleLogo.png"),Color.white);logo.preserveAspect=true;Move(logo,title,new Vector2(-325,170),new Vector2(720,310));
            foreach(var t in roots.SelectMany(r=>r.GetComponentsInChildren<ArtTitleView>(true)))
            {if(t.illustration!=null)t.illustration.SetActive(false);t.illustration=logo.gameObject;t.presentation=profile;t.Refresh();}
            var ti=0;foreach(var button in new[]{menu.newGame,menu.options,menu.quit})
            {
                Move(button,title,new Vector2(-388,-66-ti*80),new Vector2(480,64));button.image.color=new Color32(20,18,15,255);Skin(button,ink);
                foreach(var text in button.GetComponentsInChildren<TextMeshProUGUI>(true)){text.gameObject.SetActive(true);Move(text,button.transform,Vector2.zero,new Vector2(450,58));Font(text,32,bone);text.alignment=TextAlignmentOptions.Center;}
                ti++;
            }
            var oath=Label("Title Oath",title,view.characterName,"ui.selection.subtitle",new Vector2(-350,-335),new Vector2(660,45),23);
            foreach(var c in oldTitle)if(c.parent==title&&c!=menu.newGame.transform&&c!=menu.options.transform&&c!=menu.quit.transform&&c!=logo.transform&&c!=oath.transform)c.gameObject.SetActive(false);
            logo.gameObject.SetActive(true);backdrop.gameObject.SetActive(true);
            var splash=view.selectionSplash;Move(splash,page,Vector2.zero,new Vector2(1536,864));Stretch(splash.rectTransform);splash.preserveAspect=false;splash.material=null;splash.color=Color.white;splash.raycastTarget=false;splash.transform.SetAsFirstSibling();
            var shade=Node("Illustration Readability",page);shade.gameObject.SetActive(true);Stretch(shade);if(shade.GetComponent<CanvasRenderer>()==null)shade.gameObject.AddComponent<CanvasRenderer>();var shadeGraphic=shade.GetComponent<OutgameIllustrationShade>()??shade.gameObject.AddComponent<OutgameIllustrationShade>();shadeGraphic.raycastTarget=false;shade.SetSiblingIndex(1);
            var intro=Node("Character Introduction",page);Stretch(intro);view.illustratedIntroduction=intro.gameObject;
            Label("Selection Heading",intro,view.characterName,"ui.outgame.characters",new Vector2(-482,355),new Vector2(520,42),28);
            Move(view.characterName,page,new Vector2(-482,279),new Vector2(540,88));Font(view.characterName,64,bone);view.characterName.alignment=TextAlignmentOptions.MidlineLeft;
            Move(view.selectionStory,intro,new Vector2(-482,157),new Vector2(530,132));Font(view.selectionStory,23,bone);view.selectionStory.alignment=TextAlignmentOptions.TopLeft;
            Move(view.compactDescription,intro,new Vector2(-482,27),new Vector2(530,95));Font(view.compactDescription,29,bone);view.compactDescription.alignment=TextAlignmentOptions.TopLeft;
            Move(view.selectionStats,intro,new Vector2(-482,-51),new Vector2(530,38));Font(view.selectionStats,25,bone);
            Move(view.selectionRelicIcon,intro,new Vector2(-710,-130),new Vector2(50,50));
            Move(view.selectionRelic,intro,new Vector2(-441,-130),new Vector2(440,70));Font(view.selectionRelic,24,bone);
            if(view.startingRelicHover!=null){Move(view.startingRelicHover,intro,new Vector2(-482,-130),new Vector2(530,76));view.startingRelicHover.anchor=(RectTransform)view.startingRelicHover.transform;var ray=view.startingRelicHover.GetComponent<Image>()??view.startingRelicHover.gameObject.AddComponent<Image>();ray.color=Color.clear;ray.raycastTarget=true;}
            Move(view.detailsButton,page,new Vector2(-616,-214),new Vector2(260,48));Skin(view.detailsButton,ink);Move(view.detailsButtonLabel,view.detailsButton.transform,Vector2.zero,new Vector2(248,44));Font(view.detailsButtonLabel,25,bone);
            Move(view.detailsPanel.transform,page,new Vector2(-482,86),new Vector2(550,330));
            var panel=view.detailsPanel.GetComponent<Image>()??view.detailsPanel.AddComponent<Image>();panel.enabled=true;panel.color=new Color32(9,10,9,248);panel.raycastTarget=false;
            foreach(Transform c in view.detailsPanel.transform)c.gameObject.SetActive(false);
            Move(menu.detailHeading,view.detailsPanel.transform,new Vector2(0,125),new Vector2(500,56));Font(menu.detailHeading,33,bone);
            Move(view.abilityStats,view.detailsPanel.transform,new Vector2(0,0),new Vector2(500,186));Font(view.abilityStats,27,bone);
            if(view.extraStatsHover!=null)
            {
                // The legacy hover trigger shares the panel GameObject. Give its footer a
                // separate anchor; moving that component would move/resize the whole panel.
                var previous=view.extraStatsHover;var footer=Node("Extra Stats Footer",view.detailsPanel.transform);
                var hover=footer.GetComponent<HoverTooltipTrigger>()??footer.gameObject.AddComponent<HoverTooltipTrigger>();hover.view=previous.view;hover.anchor=footer;
                if(previous!=hover)previous.enabled=false;view.extraStatsHover=hover;
                Move(hover,view.detailsPanel.transform,new Vector2(0,-126),new Vector2(500,48));
                var hit=footer.GetComponent<Image>()??footer.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
                Label("Extra Stats Label",footer,view.characterName,"ui.selection.extra.hint",Vector2.zero,new Vector2(480,44),23);
            }
            view.characterArtwork=new Image[view.characterButtons.Length];view.characterFrames=new Image[view.characterButtons.Length];
            for(var i=0;i<view.characterButtons.Length;i++)
            {
                var data=view.content.characters[i];var name=data.character.ToString();data.selectionIllustration=Asset<Sprite>(Art+name+"-Illustration.png");data.selectionMaterial=null;EditorUtility.SetDirty(data);
                var button=view.characterButtons[i];Move(button,page,new Vector2(-380+i*145,-327),new Vector2(130,150));
                foreach(Transform c in button.transform)c.gameObject.SetActive(false);
                var frame=Image("Illustrated Portrait Frame",button.transform,null,new Color32(75,65,45,255));Stretch(frame.rectTransform);frame.raycastTarget=true;button.targetGraphic=frame;view.characterFrames[i]=frame;
                var well=Image("Portrait Well",frame.transform,null,new Color32(8,9,8,255));Move(well,frame.transform,Vector2.zero,new Vector2(124,144));
                var portrait=Image("Original Character Artwork",well.transform,Asset<Sprite>(Art+name+"-Button.png"),Color.white);Move(portrait,well.transform,new Vector2(0,12),new Vector2(120,116));portrait.preserveAspect=true;view.characterArtwork[i]=portrait;
                Move(view.characterLabels[i],well.transform,new Vector2(0,-57),new Vector2(122,28));Font(view.characterLabels[i],23,bone);view.characterLabels[i].alignment=TextAlignmentOptions.Center;
                Move(view.selectionMarks[i],frame.transform,new Vector2(0,-72),new Vector2(38,5));view.selectionMarks[i].color=new Color32(169,40,29,255);
                button.transition=Selectable.Transition.ColorTint;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.45f,1.35f,1.15f);colors.selectedColor=new Color(1.2f,1.15f,1.05f);colors.pressedColor=new Color(.65f,.6f,.5f);button.colors=colors;
                var nav=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=view.characterButtons[(i+view.characterButtons.Length-1)%view.characterButtons.Length],selectOnRight=view.characterButtons[(i+1)%view.characterButtons.Length],selectOnUp=view.detailsButton,selectOnDown=view.startButton};button.navigation=nav;
            }
            Move(view.startButton,page,new Vector2(595,-350),new Vector2(250,70));view.startButton.image.color=new Color32(92,26,20,255);Skin(view.startButton,ink);
            foreach(var t in view.startButton.GetComponentsInChildren<TextMeshProUGUI>(true)){t.gameObject.SetActive(true);Move(t,view.startButton.transform,Vector2.zero,new Vector2(230,60));Font(t,32,bone);}
            Move(menu.back,page,new Vector2(-649,-354),new Vector2(180,56));Skin(menu.back,ink);foreach(var t in menu.back.GetComponentsInChildren<TextMeshProUGUI>(true)){t.gameObject.SetActive(true);Move(t,menu.back.transform,Vector2.zero,new Vector2(170,52));Font(t,27,bone);}
            foreach(var c in oldPage)
                if(c.parent==page&&c!=splash.transform&&c!=shade&&c!=intro&&c!=view.characterName.transform&&c!=view.detailsPanel.transform&&c!=view.detailsButton.transform&&c!=view.startButton.transform&&c!=menu.back.transform&&!view.characterButtons.Any(b=>b.transform==c))c.gameObject.SetActive(false);
            if(page.GetComponent<Image>()!=null)page.GetComponent<Image>().enabled=false;
            view.selectedClassSymbol?.gameObject.SetActive(false);view.portrait.gameObject.SetActive(false);
            view.Select(0);view.SetDetailsVisible(false);menu.titlePage.SetActive(true);menu.characterPage.SetActive(false);menu.optionsPage.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Report+"Installed.txt","Saved illustrated title/selection scene. 6 full-screen paintings, 6 original-art buttons, readable ink overlay and native-color 1920x1080 presentation. Existing action bindings retained.\n");
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }
}
