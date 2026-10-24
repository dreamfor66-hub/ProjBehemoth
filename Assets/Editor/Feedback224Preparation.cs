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

// Saved, inspectable assets; never touches combat collision or designer pacing values.
[InitializeOnLoad]
internal static class Feedback224Preparation
{
    const string Profile="Assets/Data/Presentation/Arcade224.asset";
    static readonly Color Ink=new Color32(11,12,22,255),Paper=new Color32(222,211,175,255);
    static TMP_FontAsset font; static ArcadePresentation art; static double next;
    static Feedback224Preparation()=>EditorApplication.update+=Poll;
    static void Poll(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        art=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        if(art==null||art.feedbackRevision>=1||!File.Exists("Assets/Art/Arcade224/HeroFacePortraits.png"))return;
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    internal static void Prepare()
    {
        if(EditorApplication.isPlaying)return;
        art=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");
        var table=AssetDatabase.LoadAssetAtPath<GameTextDatabase>("Assets/Data/Localization/Resources/BehemothKr.asset");table.ImportCsv(File.ReadAllText("Assets/Data/Localization/kr.csv"));table.Invalidate();EditorUtility.SetDirty(table);
        foreach(var guid in AssetDatabase.FindAssets("t:LocalizedData",new[]{"Assets/Data"})){
            var data=AssetDatabase.LoadAssetAtPath<LocalizedData>(AssetDatabase.GUIDToAssetPath(guid));var so=new SerializedObject(data);
            foreach(var binding in data.textKeys){var p=so.FindProperty(binding.propertyPath);if(p!=null&&table.Contains(binding.key)){p.stringValue=table.Get(binding.key);binding.lastText=p.stringValue;}}
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);
        }
        var faces=Faces();Prefabs();
        foreach(var path in new[]{"Assets/Scenes/OutGameScene.unity","Assets/Scenes/MainGameScene.unity"}){
            var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var canvas=All<Canvas>(scene).First(c=>c.isRootCanvas);var view=All<OutGameView>(scene).FirstOrDefault();
            if(view!=null)Selection(view,faces);else Main(scene,canvas);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
        }
        art.feedbackRevision=1;EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Feedback224-Preparation.txt","Feedback assets saved: faces, legacy modal, combat cues, inventory, routes, MP/guard layout.\n"+DateTime.Now);
    }
    static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    internal static T Ref<T>(Object o,string field)where T:Object=>new SerializedObject(o).FindProperty(field)?.objectReferenceValue as T;
    static void Set(Object o,string field,Object v){var so=new SerializedObject(o);so.FindProperty(field).objectReferenceValue=v;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Place(Transform t,float x,float y,float w,float h){if(t==null)return;var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=Vector2.one*.5f;r.offsetMin=r.offsetMax=Vector2.zero;}
    static GameObject Child(Transform p,string name,params Type[] types){var t=p.Find(name);if(t!=null)return t.gameObject;var g=new GameObject(name,types);g.transform.SetParent(p,false);return g;}
    static TextMeshProUGUI Label(Transform p,string name,float x,float y,float w,float h,string text="",int size=11){var t=Child(p,name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();Place(t.transform,x,y,w,h);t.font=font;t.fontSize=size*4;t.enableAutoSizing=false;t.color=Paper;t.alignment=TextAlignmentOptions.Center;t.text=text;t.raycastTarget=false;return t;}
    static void Font(TextMeshProUGUI t,int size=11){if(t==null)return;t.font=font;t.fontSize=size*4;t.enableAutoSizing=false;}
    static Sprite[] Faces()=>OutGameShellPreparation.ImportPortraits(AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset"));
    static void Selection(OutGameView view,Sprite[] faces){if(view.GetComponent<OutGameMenuView>()!=null){OutGameShellPreparation.Configure(view);return;}var center=view.characterButtons[0].transform.parent;
        Place(center.Find("Title"),0,93,330,26);var title=center.Find("Title").GetComponent<TextMeshProUGUI>();Font(title,16);title.alignment=TextAlignmentOptions.Center;Place(view.characterName.transform,0,70,300,16);view.characterName.alignment=TextAlignmentOptions.Center;view.characterName.gameObject.SetActive(true);
        for(var i=0;i<6;i++){var b=view.characterButtons[i];Place(b.transform,-150+i*60,28,54,66);var pic=b.transform.Find("Portrait").GetComponent<Image>();pic.sprite=faces[i];pic.preserveAspect=true;Place(pic.transform,0,6,48,48);Place(view.characterLabels[i].transform,0,-25,54,14);Place(view.selectionMarks[i].transform,0,-32,52,1);
            foreach(var binding in center.GetComponentsInParent<Canvas>()[0].GetComponentsInChildren<PresentationImageBindings>(true).SelectMany(p=>p.bindings))if(binding.image==pic)binding.arcade=faces[i];
        }
        view.selectionStats=Label(center,"BaseStats",0,-20,340,16);
        view.selectionRelic=Label(center,"StarterName",10,-43,310,18);
        view.selectionRelicIcon=Child(center,"StarterIcon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();Place(view.selectionRelicIcon.transform,-160,-43,20,20);view.selectionRelicIcon.preserveAspect=true;
        Place(view.compactDescription.transform,0,-66,350,28);Place(view.startButton.transform,61,-96,154,24);Place(center.Find("Details"),-105,-96,80,24);view.Select(0);EditorUtility.SetDirty(view);
    }
    static void Main(Scene scene,Canvas canvas){var root=canvas.transform;var expansion=All<RunExpansionView>(scene).First();var tip=All<ItemTooltipView>(scene).First();expansion.tooltip=tip;
        var eventView=All<RoomEventView>(scene).First();var panel=Ref<GameObject>(eventView,"panel").transform;Place(panel,0,0,360,200);
        foreach(var t in panel.GetComponentsInChildren<TextMeshProUGUI>(true)){Font(t);t.alignment=TextAlignmentOptions.Center;}
        panel.GetComponent<Image>().sprite=art.frame;panel.GetComponent<Image>().color=Color.white;panel.GetComponent<Image>().type=Image.Type.Sliced;Place(panel.Find("ContextTitle"),0,83,332,20);var accent=panel.Find("TopAccent");if(accent!=null)accent.gameObject.SetActive(false);
        Place(panel.Find("Title"),0,83,332,18);Place(Ref<Image>(eventView,"illustration").transform,0,40,52,52);Place(Ref<TextMeshProUGUI>(eventView,"message").transform,0,-14,330,68);
        var yes=Ref<Button>(eventView,"yesButton");var no=Ref<Button>(eventView,"noButton");Place(yes.transform,-78,-78,142,26);Place(no.transform,78,-78,142,26);Place(Ref<TextMeshProUGUI>(eventView,"yesLabel").transform,0,0,138,22);Place(Ref<TextMeshProUGUI>(eventView,"noLabel").transform,0,0,138,22);
        var mana=All<ManaHudView>(scene).First();Place(mana.gauge.transform,-132,-29,92,5);Place(mana.label.transform,0,7,104,10);Font(mana.label,8);
        var hud=All<CombatHudView>(scene).First();var shield=Ref<Image>(hud,"shield");Place(shield.transform.parent,-52,-78,7,48);Stretch(shield.rectTransform);var st=Ref<TextMeshProUGUI>(hud,"shieldText");if(st!=null)st.gameObject.SetActive(false);
        var hp=Ref<Image>(hud,"playerHp");var hpt=Ref<TextMeshProUGUI>(hud,"playerHpText");Place(hp.transform.parent,-132,-85,92,5);if(hpt!=null){hpt.gameObject.SetActive(true);Place(hpt.transform,0,8,104,14);Font(hpt,11);hpt.color=Paper;hpt.alignment=TextAlignmentOptions.Center;}
        Place(Ref<RectTransform>(hud,"chargeGaugeRoot"),0,0,64,40);foreach(var gauge in Ref<RectTransform>(hud,"chargeGaugeRoot").GetComponentsInChildren<ArcGaugeGraphic>(true)){var so=new SerializedObject(gauge);so.FindProperty("thickness").floatValue=20;so.ApplyModifiedPropertiesWithoutUndo();Place(gauge.transform,0,0,gauge.name.Contains("Second")?74:64,gauge.name.Contains("Second")?48:40);}
        var arcadeHud=All<ArcadeHudController>(scene).First();Place(arcadeHud.hint.transform,-22,-102,310,14);Font(arcadeHud.hint,8);
        Place(expansion.encounterHint.transform,0,-16,310,14);Font(expansion.encounterHint,8);
        Place(expansion.characterStatus.transform,-119,-34,132,42);Font(expansion.characterStatus,11);Place(expansion.stageTitle.transform,0,53,300,16);
        foreach(var choice in expansion.choices){if(choice.button.GetComponent<ChoicePreview>()==null)choice.button.gameObject.AddComponent<ChoicePreview>();Font(choice.description,10);}
        var inv=root.Find("RightPanel");var grids=new[]{"PerkGrid","RelicGrid","PotionGrid"};
        for(var i=0;i<3;i++){var grid=inv.Find(grids[i]);Place(grid,0,39-i*53,326,48);var layout=grid.GetComponent<GridLayoutGroup>();layout.cellSize=Vector2.one*112;layout.spacing=Vector2.one*16;layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=10;Place(inv.Find("Section"+i),-146,71-i*53,64,14);var section=inv.Find("Section"+i).GetComponent<TextMeshProUGUI>();section.text=GameText.Get("ui.arcade.inventory."+i);}
        var tipPanel=Ref<GameObject>(tip,"panel");Place(tipPanel.transform,0,0,180,90);tipPanel.GetComponent<Image>().color=Color.white;tipPanel.GetComponent<Image>().sprite=art.frame;tipPanel.GetComponent<Image>().type=Image.Type.Sliced;tipPanel.GetComponent<Image>().raycastTarget=false;var tipGroup=tipPanel.GetComponent<CanvasGroup>();if(tipGroup!=null)tipGroup.blocksRaycasts=false;Place(Ref<TextMeshProUGUI>(tip,"title").transform,0,30,164,18);Place(Ref<TextMeshProUGUI>(tip,"description").transform,0,0,164,60);Font(Ref<TextMeshProUGUI>(tip,"description"),11);tipPanel.transform.SetAsLastSibling();
        var inventory=All<RunHudView>(scene).First();var prefab=Ref<RunHudIconView>(inventory,"iconPrefab");var quick=Child(root,"QuickInventory",typeof(RectTransform),typeof(QuickInventoryView)).GetComponent<QuickInventoryView>();Place(quick.transform,0,0,384,224);quick.run=All<RunController>(scene).First();quick.tooltip=tip;
        RunHudIconView[] Pool(string prefix,int count,float startX,float y,int size){var list=new RunHudIconView[count];for(var i=0;i<count;i++){var name=prefix+i;var t=quick.transform.Find(name);var icon=t!=null?t.GetComponent<RunHudIconView>():Object.Instantiate(prefab,quick.transform);icon.name=name;Place(icon.transform,startX+i*(size+3),y,size,size);Place(Ref<Image>(icon,"iconImage").transform,0,0,size-2,size-2);icon.gameObject.SetActive(false);list[i]=icon;}return list;}
        quick.relics=Pool("Relic",18,-174,77,16);quick.potions=Pool("Potion",4,-12,100,16);quick.transform.SetSiblingIndex(root.Find("ArcadeStatus").GetSiblingIndex()+1);
        foreach(var map in All<DungeonMapView>(scene)){var content=Ref<RectTransform>(map,"content");if(content.GetComponent<RectMask2D>()==null)content.gameObject.AddComponent<RectMask2D>();var so=new SerializedObject(map);so.FindProperty("spacing").floatValue=80;so.ApplyModifiedPropertiesWithoutUndo();}
        var bindings=All<PresentationImageBindings>(scene).First();var list=bindings.bindings.ToList();
        var panels=new[]{panel,tipPanel.transform,expansion.modal.transform,Ref<GameObject>(All<ShopView>(scene).First(),"panel").transform,Ref<GameObject>(All<PerkChoiceView>(scene).First(),"panel").transform};
        foreach(var box in panels){var image=box.GetComponent<Image>();if(image==null)continue;image.sprite=art.frame;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=.5f;image.color=Color.white;var entry=list.FirstOrDefault(b=>b.image==image);if(entry==null){entry=new PresentationImageBindings.Binding{image=image};list.Add(entry);}entry.arcade=art.frame;entry.color=Color.white;}
        var tipImage=tipPanel.GetComponent<Image>();tipImage.sprite=null;tipImage.color=new Color32(25,27,45,255);var tipBinding=list.First(b=>b.image==tipImage);tipBinding.arcade=null;tipBinding.color=tipImage.color;
        var outline=tipPanel.GetComponent<Outline>()??tipPanel.AddComponent<Outline>();outline.effectColor=new Color32(113,107,117,255);outline.effectDistance=new Vector2(4,-4);outline.useGraphicAlpha=true;
        bindings.bindings=list.ToArray();EditorUtility.SetDirty(bindings);
        foreach(var item in new Object[]{quick,expansion,eventView,hud})EditorUtility.SetDirty(item);
    }
    static void Prefabs(){
        var dog=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Arcade224/States/CompanionDog.png");dog.maxTextureSize=128;dog.SaveAndReimport();
        var rat=(TextureImporter)AssetImporter.GetAtPath("Assets/Art/Arcade224/States/EnemyStates_08.png");
#pragma warning disable CS0618
        var rects=rat.spritesheet;for(var i=0;i<rects.Length;i++)if(rects[i].name.Contains("TunnelRat")){var r=rects[i].rect;r.y=0;r.height=rects[i].name.EndsWith("Dead")?190:rects[i].name.EndsWith("Ready")?212:205;rects[i].rect=r;}rat.spritesheet=rects;
#pragma warning restore CS0618
        rat.SaveAndReimport();
        void Edit(string path,Action<GameObject> edit){var p=PrefabUtility.LoadPrefabContents(path);try{edit(p);PrefabUtility.SaveAsPrefabAsset(p,path);}finally{PrefabUtility.UnloadPrefabContents(p);}}
        Edit("Assets/Prefabs/Combat/MonsterView.prefab",p=>{var v=p.GetComponent<MonsterView>();var old=Ref<Image>(v,"body");if(old.transform==p.transform){var body=Child(p.transform,"Art",typeof(RectTransform),typeof(Image)).GetComponent<Image>();body.sprite=old.sprite;body.preserveAspect=true;body.raycastTarget=false;Stretch(body.rectTransform);body.transform.SetAsFirstSibling();old.enabled=false;Set(v,"body",body);}var label=Ref<TextMeshProUGUI>(v,"stateLabel");Place(label.transform,0,29,240,18);Font(label);label.textWrappingMode=TextWrappingModes.NoWrap;Place(Ref<Image>(v,"hpFill").transform.parent,0,16,104,4);
            Place(Ref<Image>(v,"attackNameBackdrop").transform,100,0,140,20);Ref<Image>(v,"attackNameBackdrop").color=Ink;Place(Ref<TextMeshProUGUI>(v,"attackNameLabel").transform,100,0,136,18);Font(Ref<TextMeshProUGUI>(v,"attackNameLabel"));Ref<TextMeshProUGUI>(v,"attackNameLabel").alignment=TextAlignmentOptions.Center;
            Place(Ref<Image>(v,"chargeTimeBackdrop").transform,100,-24,68,26);Ref<Image>(v,"chargeTimeBackdrop").color=Ink;Place(Ref<TextMeshProUGUI>(v,"chargeTimeLabel").transform,100,-24,64,24);Font(Ref<TextMeshProUGUI>(v,"chargeTimeLabel"),16);Ref<TextMeshProUGUI>(v,"chargeTimeLabel").alignment=TextAlignmentOptions.Center;Ref<TextMeshProUGUI>(v,"chargeTimeLabel").color=Paper;
        });
        Edit("Assets/Prefabs/Combat/WeakPointView.prefab",p=>{var v=p.GetComponent<WeakPointView>();var reticle=Child(p.transform,"Reticle",typeof(RectTransform),typeof(WeakPointReticle)).GetComponent<WeakPointReticle>();Stretch(reticle.rectTransform);reticle.presentation=art;reticle.raycastTarget=false;v.reticle=reticle;});
        Edit("Assets/Prefabs/UI/RunHudIconView.prefab",p=>{var view=p.GetComponent<RunHudIconView>();Place(Ref<Image>(view,"iconImage").transform,0,0,24,24);var count=Ref<TextMeshProUGUI>(view,"stackLabel");Place(count.transform,7,-7,12,12);Font(count,8);});
        Edit("Assets/Prefabs/UI/MapRoomIcon.prefab",p=>{var v=p.GetComponent<MapRoomIcon>();foreach(var field in new[]{"northRoute","eastRoute","southRoute","westRoute"}){var image=Ref<Image>(v,field);if(image==null)continue;var vertical=field is "northRoute" or "southRoute";Place(image.transform,field=="eastRoute"?10:field=="westRoute"?-10:0,field=="northRoute"?10:field=="southRoute"?-10:0,vertical?2:10,vertical?10:2);image.transform.SetAsFirstSibling();}});
    }
}


