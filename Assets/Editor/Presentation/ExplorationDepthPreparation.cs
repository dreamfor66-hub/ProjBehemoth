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
internal static partial class ExplorationDepthPreparation
{
    internal const string Folder="Docs/Production/ExplorationDepth-2026-11-01/";
    static double next;
    static TypographyProfile typography;
    static ExplorationDepthPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+4;
        if(!File.Exists(Folder+"UIReady.txt"))return;
        var paths=new[]{"Assets/Scenes/MainGameScene.unity","Assets/Scenes/Trials/Angular01_Main.unity"};
        if(paths.Any(p=>{var s=SceneManager.GetSceneByPath(p);return s.isLoaded&&s.isDirty;}))return;
        try
        {
            if(!File.Exists(Folder+"UIInstalled.txt")||!File.ReadAllText(Folder+"UIInstalled.txt").StartsWith("v4"))
            {
                InstallText(); typography=AssetDatabase.LoadAssetAtPath<TypographyProfile>(TypographyPreparation.ProfilePath);
                foreach(var path in paths)
                {
                    var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
                    if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try{InstallUI(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                    finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
                }
                AssetDatabase.SaveAssets();File.WriteAllText(Folder+"UIInstalled.txt","v4\nSaved play-code panels, fixed ASCII code font, scrollable room details, copy/paste/validation, floor record labels and read-only world-view controls in both playable scenes.\n");
            }
            if(File.Exists(Folder+"ArtReady.json")&&File.Exists(Folder+"ContentPlan.json")&&!File.Exists(Folder+"Installed.txt"))InstallContent();
        }
        catch(Exception e){File.WriteAllText(Folder+"PreparationError.txt",e.ToString());Debug.LogException(e);next+=60;}
    }
    static void Backup(string path)
    {var target=Folder+"Before/"+path;if(!File.Exists(target)&&File.Exists(path)){Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target);}}
    static void InstallUI(Scene scene)
    {
        var all=scene.GetRootGameObjects();var overlay=all.SelectMany(r=>r.GetComponentsInChildren<DebugCheatOverlay>(true)).Single();
        var flow=all.SelectMany(r=>r.GetComponentsInChildren<GameFlowController>(true)).Single();
        var canvas=((GameObject)new SerializedObject(overlay).FindProperty("panel").objectReferenceValue).GetComponentInParent<Canvas>();
        var old=canvas.transform.Find("Play record");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=Node("Play record",canvas.transform,Vector2.zero,Vector2.zero);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var view=root.gameObject.AddComponent<PlayRecordView>();view.flow=flow;
        var panel=Box("Record panel",root,Vector2.zero,new(1460,880),new Color32(12,14,16,253));view.panel=panel.gameObject;
        Label("Heading",panel,new(0,390),new(1000,60),"ui.playrecord.title",36);
        view.closeButton=Button("Close",panel,new(630,390),new(120,52),"ui.playrecord.close");
        var inputBox=Box("Code input",panel,new(-95,278),new(1060,100),new Color32(28,31,34,255));
        var input=inputBox.gameObject.AddComponent<TMP_InputField>();view.codeInput=input;input.characterLimit=1500000;input.lineType=TMP_InputField.LineType.MultiLineNewline;
        var viewport=Node("Text viewport",inputBox,Vector2.zero,new(1028,82));viewport.gameObject.AddComponent<RectMask2D>();
        var text=Label("Text",viewport,Vector2.zero,new(1028,82),null,19);text.alignment=TextAlignmentOptions.TopLeft;text.richText=false;
        var codeFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset");if(codeFont!=null)text.font=codeFont;
        // An opaque support code must stay legible independently of the game's calligraphic base face.
        // A saved, disabled binding prevents automatic typography preparation from restyling it.
        text.gameObject.AddComponent<TypographyRoleBinding>().enabled=false;
        var placeholder=Label("Placeholder",viewport,Vector2.zero,new(1028,82),"ui.playrecord.placeholder",22);placeholder.alignment=TextAlignmentOptions.TopLeft;placeholder.color=new Color32(160,158,145,255);
        input.textViewport=viewport;input.textComponent=text;input.placeholder=placeholder;input.targetGraphic=inputBox.GetComponent<Image>();input.caretColor=new Color(.95f,.86f,.65f);input.selectionColor=new Color(.4f,.31f,.14f,.7f);
        view.copyButton=Button("Copy run code",panel,new(550,309),new(230,50),"ui.playrecord.copy");
        view.pasteButton=Button("Paste clipboard",panel,new(550,250),new(230,50),"ui.playrecord.paste");
        view.floorButtons=new Button[3];for(var i=0;i<3;i++)view.floorButtons[i]=Button("Floor "+(i+1),panel,new(-554+i*133,180),new(120,48),"ui.playrecord.act"+(i+1));
        view.currentButton=Button("Current run",panel,new(135,180),new(220,48),"ui.playrecord.current");
        view.importButton=Button("Load pasted record",panel,new(420,180),new(310,48),"ui.playrecord.import");
        view.summary=Label("Summary",panel,new(0,128),new(1320,40),null,22);
        var scrollBox=Box("Room list",panel,new(-412,-106),new(530,410),new Color32(17,19,21,255));
        var scroll=scrollBox.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        var clip=Node("Viewport",scrollBox,Vector2.zero,new(512,402));clip.gameObject.AddComponent<RectMask2D>();scroll.viewport=clip;
        var content=Node("Rooms",clip,Vector2.zero,new(500,0));content.anchorMin=new(0,1);content.anchorMax=Vector2.one;content.pivot=new(.5f,1);content.anchoredPosition=Vector2.zero;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=6;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;view.roomList=content;
        view.roomTemplate=Button("Room row",content,Vector2.zero,new(500,50),null);view.roomTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight=50;
        view.roomTemplate.GetComponentInChildren<TextMeshProUGUI>().fontSize=22;view.roomTemplate.gameObject.SetActive(false);
        var detailClip=Node("Details viewport",panel,new(272,-72),new(700,354));detailClip.gameObject.AddComponent<RectMask2D>();
        detailClip.gameObject.AddComponent<Image>().color=new Color(0,0,0,.001f);
        var detailScroll=detailClip.gameObject.AddComponent<ScrollRect>();detailScroll.horizontal=false;detailScroll.movementType=ScrollRect.MovementType.Clamped;detailScroll.viewport=detailClip;
        view.detail=Label("Room details",detailClip,Vector2.zero,new(700,354),null,23);view.detail.alignment=TextAlignmentOptions.TopLeft;
        var dr=view.detail.rectTransform;dr.anchorMin=new(0,1);dr.anchorMax=Vector2.one;dr.pivot=new(.5f,1);dr.anchoredPosition=Vector2.zero;dr.sizeDelta=new(0,354);
        dr.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;detailScroll.content=dr;
        view.previewButton=Button("View room",panel,new(275,-287),new(410,54),"ui.playrecord.view");
        view.feedback=Label("Feedback",panel,new(0,-370),new(1310,92),"ui.playrecord.help",22);
        var bar=Box("Record viewing toolbar",root,new(0,0),new(1500,102),new Color32(12,14,16,250));bar.anchorMin=bar.anchorMax=new(.5f,0);bar.anchoredPosition=new(0,55);view.previewToolbar=bar.gameObject;
        view.previewLabel=Label("Viewing",bar,new(-395,14),new(650,64),null,25);
        view.previousRoom=Button("Previous room",bar,new(-14,0),new(140,60),"ui.playrecord.previous");
        view.nextRoom=Button("Next room",bar,new(136,0),new(140,60),"ui.playrecord.next");
        view.turnButton=Button("Turn view",bar,new(296,0),new(160,60),"ui.playrecord.turn");
        view.tableButton=Button("Room table",bar,new(471,0),new(170,60),"ui.playrecord.table");
        view.leavePreview=Button("Return to live run",bar,new(650,0),new(175,60),"ui.playrecord.return");
        var chapter=overlay.chapterTools.transform;var prior=chapter.Find("Open play record");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
        var entry=Button("Open play record",chapter,new(466,-338),new(350,50),"ui.playrecord.open");UnityEventTools.AddPersistentListener(entry.onClick,view.Open);
        panel.gameObject.SetActive(false);bar.gameObject.SetActive(false);EditorUtility.SetDirty(view);
    }
    static RectTransform Node(string name,Transform parent,Vector2 position,Vector2 size)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
    static RectTransform Box(string name,Transform parent,Vector2 position,Vector2 size,Color color)
    {var r=Node(name,parent,position,size);r.gameObject.AddComponent<Image>().color=color;return r;}
    static TextMeshProUGUI Label(string name,Transform parent,Vector2 position,Vector2 size,string key,float font)
    {
        var t=Node(name,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=typography.Font(Typeface.Base);t.fontSize=font;t.color=new Color32(230,223,200,255);t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.Normal;
        if(!string.IsNullOrEmpty(key)){t.text=GameText.Get(key);t.gameObject.AddComponent<GameTextBinding>().key=key;}return t;
    }
    static Button Button(string name,Transform parent,Vector2 position,Vector2 size,string key)
    {var r=Box(name,parent,position,size,new Color32(41,41,39,255));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();var n=b.navigation;n.mode=Navigation.Mode.None;b.navigation=n;Label("Label",r,Vector2.zero,size-new Vector2(12,4),key,24);return b;}
    static void InstallText()
    {
        var rows=new[]{
            ("ui.explore.wall.opened","숨겨진 방을 찾았다!"),("ui.playrecord.open","플레이 코드"),("ui.playrecord.title","플레이 기록 조회"),
            ("ui.playrecord.close","닫기"),("ui.playrecord.copy","현재 코드 복사"),("ui.playrecord.paste","붙여넣기"),("ui.playrecord.import","붙여넣은 기록 불러오기"),
            ("ui.playrecord.current","현재 플레이"),("ui.playrecord.placeholder","BHM1-로 시작하는 플레이 코드를 붙여넣으세요."),
            ("ui.playrecord.help","기록은 실제 방 배치·사건·선택·방향을 담습니다. 조회는 현재 플레이를 바꾸지 않습니다.\n미진입 사건은 아직 확정되지 않았으며, 원화가 갱신된 경우 현재 리소스로 표시합니다."),
            ("ui.playrecord.invalid","코드를 읽을 수 없습니다. 전체 코드와 끝부분이 빠짐없이 복사됐는지 확인하세요."),
            ("ui.playrecord.empty","기록된 플레이가 없습니다."),("ui.playrecord.copied","플레이 코드를 복사했습니다. 이 코드를 피드백과 함께 보내주세요."),
            ("ui.playrecord.pasted","코드를 붙여넣었습니다. ‘붙여넣은 기록 불러오기’를 누르세요."),("ui.playrecord.imported","기록을 불러왔습니다. 방을 선택하면 배치와 선택 내역을 조회할 수 있습니다."),
            ("ui.playrecord.busy","방을 둘러보는 중에만 화면을 조회할 수 있습니다. 현재 선택이나 전투를 마친 뒤 열어주세요."),
            ("ui.playrecord.view","선택한 방의 화면 보기"),("ui.playrecord.previous","이전 방"),("ui.playrecord.next","다음 방"),("ui.playrecord.turn","다른 입구 시점"),
            ("ui.playrecord.table","방 목록"),("ui.playrecord.return","플레이로 복귀"),("ui.playrecord.none","없음"),("ui.playrecord.pending","미진입 · 아직 미확정"),("ui.playrecord.emptyroom","빈 방"),
            ("ui.playrecord.visited","방문"),("ui.playrecord.unvisited","미방문"),("ui.playrecord.facing0","북쪽"),("ui.playrecord.facing1","동쪽"),("ui.playrecord.facing2","남쪽"),("ui.playrecord.facing3","서쪽"),
            ("ui.playrecord.summary","기록된 막 {0}개 · 기록 시작 {1}"),("ui.playrecord.row","#{0}  ({1}, {2})  {3} · {4}"),
            ("ui.playrecord.act1","1막 기록"),("ui.playrecord.act2","2막 기록"),("ui.playrecord.act3","3막 기록"),
            ("ui.playrecord.state","오브젝트 상태: {0}"),("ui.playrecord.removed","떠남 / 사라짐"),("ui.playrecord.used","사용됨"),("ui.playrecord.resolved","해결됨"),("ui.playrecord.available","미사용 / 미해결"),
            ("ui.playrecord.detail","{0}막 · 방 #{1} ({2}, {3}) · {4}\n사건: {5}\n오브젝트: {6}\n배경: {7}\n연결된 방: {8}\n실제 진입 방향: {9}\n\n선택 기록\n{10}"),
            ("ui.playrecord.preview","기록 조회 · {0}막 #{1} · {2}\n조사·이동·보상은 실행되지 않습니다."),
            ("ui.companion.recoveryhound","회수견이 내 걸음을 확인하고 다시 어둠 쪽을 경계한다.")
        };
        var csv=ScriptableObject.CreateInstance<GameTextDatabase>();csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach(var row in rows){csv.Set(row.Item1,row.Item2,"Exploration depth");BehemothTextEditor.Table.Set(row.Item1,row.Item2,"Exploration depth");}
        File.WriteAllText(BehemothTextEditor.CsvPath,csv.ExportCsv(),new System.Text.UTF8Encoding(true));Object.DestroyImmediate(csv);EditorUtility.SetDirty(BehemothTextEditor.Table);
    }
    static partial void InstallContent();
}
