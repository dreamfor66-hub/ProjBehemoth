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

[InitializeOnLoad]
internal static class FeedbackRevision3Preparation
{
    const string Profile="Assets/Data/Presentation/Arcade224.asset";
    static readonly Color Ink=new Color32(11,12,22,255),Paper=new Color32(222,211,175,255),Gold=new Color32(216,187,98,255);
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");
    static double next;
    static FeedbackRevision3Preparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+5;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var p=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        if(p==null||p.feedbackRevision>=4||!File.Exists("Assets/Art/Arcade224/SelectionWizard.png"))return;
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    internal static void ConfigureSelection(OutGameView view)
    {
        var page=view.transform.Find("SelectionCenter");if(page==null)page=view.portrait.transform.parent;
        var p=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);view.presentation=p;
        var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>("Assets/Editor/Arcade-ArtReferences.asset");
        var backgrounds=new[]{new Color32(12,25,50,255),new Color32(11,40,29,255),new Color32(49,10,18,255),new Color32(36,17,34,255),new Color32(59,23,8,255),new Color32(37,22,52,255)};
        foreach(var data in view.content.characters)
        {
            var path="Assets/Art/Arcade224/Selection"+data.character+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            data.selectionIllustration=AssetDatabase.LoadAssetAtPath<Sprite>(path);data.selectionMaterial=SelectionPalettePreparation.Get(path);data.selectionBackdrop=backgrounds[(int)data.character];EditorUtility.SetDirty(data);
            var entry=archive.entries.FirstOrDefault(e=>e.asset==data&&e.property=="selectionIllustration");
            if(entry==null){entry=new ArtReferenceArchive.Entry{asset=data,property="selectionIllustration"};archive.entries.Add(entry);}entry.arcade=data.selectionIllustration;
            var materialEntry=archive.entries.FirstOrDefault(e=>e.asset==data&&e.property=="selectionMaterial");if(materialEntry==null){materialEntry=new ArtReferenceArchive.Entry{asset=data,property="selectionMaterial"};archive.entries.Add(materialEntry);}materialEntry.arcade=data.selectionMaterial;
        }
        EditorUtility.SetDirty(archive);
        view.selectionSplash=Box(page,"CharacterSplash",0,0,384,224,Color.white);view.selectionSplash.transform.SetAsFirstSibling();view.selectionSplash.raycastTarget=false;view.selectionSplash.preserveAspect=false;
        Place(view.portrait.transform,87,8,96,144);view.portrait.gameObject.SetActive(false);
        Place(view.characterName.transform,-95,82,158,28);view.characterName.fontSize=88;view.characterName.color=Gold;
        Place(view.selectionStory.transform,-95,24,158,80);view.selectionStory.fontSize=44;view.selectionStory.lineSpacing=0;
        Place(view.selectionStats.transform,-95,-25,158,16);view.selectionStats.fontSize=40;
        var relic=view.startingRelicHover.transform;Place(relic,-95,-47,158,20);
        Place(view.selectionRelicIcon.transform,-69,0,16,16);Place(view.selectionRelic.transform,12,0,130,18);view.selectionRelic.fontSize=40;
        for(var i=0;i<view.characterButtons.Length;i++)
        {
            var b=view.characterButtons[i];Place(b.transform,-95+i*38,-78,30,30);
            b.transform.Find("Portrait").gameObject.SetActive(false);
            var symbol=Child(b.transform,"Symbol",typeof(RectTransform),typeof(CanvasRenderer),typeof(ClassSymbolGraphic)).GetComponent<ClassSymbolGraphic>();Place(symbol.transform,0,0,24,24);symbol.character=view.content.characters[i].character;symbol.presentation=p;symbol.raycastTarget=false;symbol.color=Paper;symbol.SetAllDirty();
            Place(view.characterLabels[i].transform,0,0,28,28);view.characterLabels[i].fontSize=32;
        }
        var bindings=view.GetComponent<PresentationImageBindings>();
        if(bindings!=null&&!bindings.bindings.Any(b=>b.image==view.selectionSplash))bindings.bindings=bindings.bindings.Append(new PresentationImageBindings.Binding{image=view.selectionSplash,arcade=view.content.characters[0].selectionIllustration,color=Color.white}).ToArray();
        if(bindings!=null){var splash=bindings.bindings.First(b=>b.image==view.selectionSplash);splash.overrideMaterial=true;splash.arcadeMaterial=view.content.characters[0].selectionMaterial;}
        EditorUtility.SetDirty(bindings);EditorUtility.SetDirty(view);
    }
    internal static void Prepare()
    {
        if(EditorApplication.isPlaying)return;
        OutGameShellPreparation.Prepare();
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var expansion=All<RunExpansionView>(scene).First();ConfigureSpellbook(expansion);
        PrepareCriticalPerks(All<CombatController>(scene).First().Content,All<RunController>(scene));
        var combat=All<CombatSceneView>(scene).First();combat.criticalFlash=Box(combat.LogicalArea,"CriticalFlash",0,0,384,224,Color.clear);combat.criticalFlash.raycastTarget=false;combat.criticalFlash.enabled=false;combat.criticalFlash.transform.SetAsLastSibling();EditorUtility.SetDirty(combat);
        const string prefabPath="Assets/Prefabs/UI/DamageNumberView.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var view=prefab.GetComponent<DamageNumberView>();var label=prefab.GetComponentsInChildren<TextMeshProUGUI>(true).First(t=>!t.name.StartsWith("Outline"));var offsets=new[]{new Vector2(-4,-4),new Vector2(0,-4),new Vector2(4,-4),new Vector2(-4,0),new Vector2(4,0),new Vector2(-4,4),new Vector2(0,4),new Vector2(4,4)};
            var labels=offsets.Select((offset,i)=>{var copy=Child(label.transform.parent,"Outline"+i,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();EditorUtility.CopySerialized(label,copy);copy.name="Outline"+i;copy.rectTransform.anchorMin=label.rectTransform.anchorMin;copy.rectTransform.anchorMax=label.rectTransform.anchorMax;copy.rectTransform.pivot=label.rectTransform.pivot;copy.rectTransform.sizeDelta=label.rectTransform.sizeDelta;copy.rectTransform.anchoredPosition=label.rectTransform.anchoredPosition+offset;copy.raycastTarget=false;copy.color=Color.black;copy.gameObject.SetActive(false);return copy;}).ToArray();
            label.transform.SetAsLastSibling();view.ConfigureOutline(labels);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var multi=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Multi.asset");foreach(var unit in multi.Units)for(var i=0;i<unit.effects.Count;i++){var effect=unit.effects[i];if(effect.effectType==EffectType.ActionExtraHits&&effect.effectTime<=0){effect.effectTime=.12f;unit.effects[i]=effect;}}EditorUtility.SetDirty(multi);
        var p=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);p.feedbackRevision=4;EditorUtility.SetDirty(p);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!open)EditorSceneManager.CloseScene(scene,true);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/FeedbackRevision3-Preparation.txt","Serialized selection splashes/symbols, spellbook, flash, damage outline. "+DateTime.Now);
    }
    static void PrepareCriticalPerks(GameContent content,RunController[] controllers)
    {
        var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>("Assets/Editor/Arcade-ArtReferences.asset");
        var chance=Ensure("KeenInstinct","perk.critical.chance",ModifierKind.CriticalChance,.05f,AssetDatabase.LoadAssetAtPath<PerkData>("Assets/Data/Behemoth/Perks/SharpWeapon.asset").icon);
        var damage=Ensure("DecisiveBlow","perk.critical.damage",ModifierKind.CriticalDamageMultiplier,.15f,content.perks.FirstOrDefault(p=>p.id=="Exploit")?.icon);
        foreach(var controller in controllers)
        {
            var so=new SerializedObject(controller);var list=so.FindProperty("perks");
            foreach(var perk in new[]{chance,damage})if(!Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).objectReferenceValue==perk))
            {var index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=perk;}
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(controller);
        }
        foreach(var character in content.characters)EditorUtility.SetDirty(character);
        EditorUtility.SetDirty(content);EditorUtility.SetDirty(archive);
        PerkData Ensure(string id,string key,ModifierKind kind,float value,Sprite icon)
        {
            var path="Assets/Data/Behemoth/Perks/"+id+".asset";var perk=AssetDatabase.LoadAssetAtPath<PerkData>(path);
            if(perk==null)
            {
                perk=ScriptableObject.CreateInstance<PerkData>();perk.id=id;perk.scope=RelicScope.Shared;perk.maxStacks=5;perk.icon=icon;
                perk.displayName=GameText.Get(key+".name");perk.description=GameText.Get(key+".description");
                perk.textKeys.Add(new DataTextBinding{propertyPath="displayName",key=key+".name",lastText=perk.displayName});perk.textKeys.Add(new DataTextBinding{propertyPath="description",key=key+".description",lastText=perk.description});
                perk.modifiers.Add(new StatModifier{kind=kind,value=value});AssetDatabase.CreateAsset(perk,path);BehemothTextEditor.SyncAsset(perk,true);
            }
            if(!content.perks.Contains(perk))content.perks.Add(perk);
            if(!archive.entries.Any(e=>e.asset==perk&&e.property=="icon"))archive.entries.Add(new ArtReferenceArchive.Entry{asset=perk,property="icon",geometry=null,arcade=perk.icon});
            return perk;
        }
    }
    static void ConfigureSpellbook(RunExpansionView expansion)
    {
        var page=Box(expansion.modal.transform.parent,"Spellbook",0,0,380,220,Ink);page.raycastTarget=true;
        var view=page.GetComponent<SpellbookView>()??page.gameObject.AddComponent<SpellbookView>();view.owner=expansion;expansion.spellbookView=view;
        Text(page.transform,"Title","ui.spellbook.title",-133,94,92,22,22);
        view.close=Button(page.transform,"Close","ui.common.close",151,96,54,18);
        view.slots=new Button[4];view.slotLabels=new TextMeshProUGUI[4];view.slotIcons=new Image[4];view.slotFills=new Image[4];
        var positions=new[]{new Vector2(-142,4),new Vector2(-82,4),new Vector2(-112,34),new Vector2(-112,-26)};
        for(var i=0;i<4;i++)
        {
            var button=Button(page.transform,"Slot"+i,"",positions[i].x,positions[i].y,44,24);view.slots[i]=button;
            var label=button.GetComponentInChildren<TextMeshProUGUI>();Place(label.transform,8,1,28,14);view.slotLabels[i]=label;
            view.slotIcons[i]=Box(button.transform,"Icon",-12,1,12,12,Color.white);view.slotIcons[i].preserveAspect=true;
            var track=Box(button.transform,"Cooldown",0,-9,40,2,Color.clear);view.slotFills[i]=Box(track.transform,"Fill",0,0,40,2,Gold);var fill=view.slotFills[i].rectTransform;fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
        }
        view.preparedName=Text(page.transform,"PreparedName","",-112,-58,132,32);view.preparedName.color=Gold;
        view.heading=Text(page.transform,"Name","",62,65,204,32,22);view.heading.alignment=TextAlignmentOptions.Left;view.heading.textWrappingMode=TextWrappingModes.NoWrap;view.heading.color=Gold;
        view.description=Text(page.transform,"Description","",62,0,204,88);view.description.alignment=TextAlignmentOptions.TopLeft;
        Text(page.transform,"AlternativesTitle","ui.spellbook.alternatives",62,-52,204,16).alignment=TextAlignmentOptions.Left;
        var scrollObject=Child(page.transform,"KnownSpells",typeof(RectTransform),typeof(Image),typeof(ScrollRect));Place(scrollObject.transform,39,-78,158,36);scrollObject.GetComponent<Image>().color=new Color32(25,27,45,255);
        var viewport=Box(scrollObject.transform,"Viewport",0,0,158,36,Color.clear);if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();viewport.raycastTarget=true;
        var content=Child(viewport.transform,"Content",typeof(RectTransform));Place(content.transform,0,0,158,36);var cr=(RectTransform)content.transform;cr.anchorMin=new Vector2(.5f,1);cr.anchorMax=cr.anchorMin;cr.pivot=new Vector2(.5f,1);cr.anchoredPosition=Vector2.zero;
        view.knownScroll=scrollObject.GetComponent<ScrollRect>();view.knownScroll.viewport=viewport.rectTransform;view.knownScroll.content=cr;view.knownScroll.horizontal=false;view.knownScroll.vertical=true;view.knownScroll.scrollSensitivity=48;
        var scrollTrack=Box(scrollObject.transform,"Scrollbar",78,0,2,36,new Color32(75,74,96,255));scrollTrack.raycastTarget=true;var handle=Box(scrollTrack.transform,"Handle",0,0,2,12,Gold);handle.raycastTarget=true;
        handle.rectTransform.sizeDelta=Vector2.zero;handle.rectTransform.anchoredPosition=Vector2.zero;
        var scrollbar=scrollTrack.GetComponent<Scrollbar>()??scrollTrack.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;view.knownScroll.verticalScrollbar=scrollbar;view.knownScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        view.knownButtons=new Button[16];view.knownLabels=new TextMeshProUGUI[16];
        for(var i=0;i<16;i++){var b=Button(content.transform,"Known"+i,"",0,-10-i*20,154,18);var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);view.knownButtons[i]=b;view.knownLabels[i]=b.GetComponentInChildren<TextMeshProUGUI>();view.knownLabels[i].alignment=TextAlignmentOptions.Left;}
        view.equip=Button(page.transform,"Prepare","ui.spellbook.prepare",151,-78,54,36);view.equipLabel=view.equip.GetComponentInChildren<TextMeshProUGUI>();view.equipLabel.textWrappingMode=TextWrappingModes.Normal;
        EditorUtility.SetDirty(view);EditorUtility.SetDirty(expansion);page.gameObject.SetActive(false);
    }
    static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    static GameObject Child(Transform p,string n,params Type[] types){var t=p.Find(n);if(t!=null)return t.gameObject;var g=new GameObject(n,types);g.transform.SetParent(p,false);return g;}
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static Image Box(Transform p,string n,float x,float y,float w,float h,Color c){var image=Child(p,n,typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(image.transform,x,y,w,h);image.color=c;image.raycastTarget=false;return image;}
    static TextMeshProUGUI Text(Transform p,string n,string key,float x,float y,float w,float h,int size=11){var t=Child(p,n,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();Place(t.transform,x,y,w,h);t.font=Font;t.fontSize=size*4;t.enableAutoSizing=false;t.text=string.IsNullOrEmpty(key)?"":GameText.Get(key);t.color=Paper;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;if(!string.IsNullOrEmpty(key)){var b=t.GetComponent<GameTextBinding>()??t.gameObject.AddComponent<GameTextBinding>();b.key=key;}return t;}
    static Button Button(Transform p,string n,string key,float x,float y,float w,float h){var image=Box(p,n,x,y,w,h,new Color32(25,27,45,255));image.raycastTarget=true;var b=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};var colors=b.colors;colors.highlightedColor=colors.selectedColor=new Color(1.3f,1.3f,1.3f);b.colors=colors;Text(b.transform,"Label",key,0,0,w-4,h-2);return b;}
}
