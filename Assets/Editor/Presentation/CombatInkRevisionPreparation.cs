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
internal static class CombatInkRevisionPreparation
{
    internal const string Folder="Docs/Production/CombatInkRevision-2026-10-30/";
    static bool busy,waiting,running,stopAfter,resultsOnly;
    static string completion="Complete.txt";
    static CombatInkRevisionPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Folder+"Ready.txt")&&!File.Exists(Folder+"Installed.txt"))
        {
            busy=true;
            try{Prepare();File.WriteAllText(Folder+"Installed.txt","Saved grouped action HUD, warm target palette and title-only death actions.\n");}
            catch(Exception e){File.WriteAllText(Folder+"PreparationError.txt",e.ToString());Debug.LogException(e);}
            finally{busy=false;}
        }
        const string request="Temp/CombatInkRevision.request";
        if(File.Exists(request)&&File.Exists(Folder+"Installed.txt"))
        {
            var mode=File.ReadAllText(request).Trim();File.Delete(request);
            if(mode=="stop"){EditorApplication.isPlaying=false;waiting=running=false;return;}
            if(mode is "verify" or "results")
            {resultsOnly=mode=="results";completion=resultsOnly?"ResultsComplete.txt":"Complete.txt";stopAfter=!EditorApplication.isPlaying;waiting=true;running=false;if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;EditorApplication.isPaused=false;}
        }
        if(waiting&&EditorApplication.isPlaying&&Object.FindFirstObjectByType<CombatController>()!=null)
        {waiting=false;running=true;var probe=new GameObject("Scoped combat ink verification").AddComponent<CombatInkRevisionLiveProbe>();if(resultsOnly)probe.RunResults();else probe.Run();}
        if(running&&File.Exists(Folder+completion)){running=false;if(stopAfter)EditorApplication.isPlaying=false;}
    }
    static T Ref<T>(Object target,string field)where T:Object=>new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    static void Place(RectTransform r,Vector2 position,Vector2 size)
    {r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;r.localScale=Vector3.one;}
    static void Font(TextMeshProUGUI t,float size)
    {t.fontSize=size;t.enableAutoSizing=false;t.raycastTarget=false;var role=t.GetComponent<TypographyRoleBinding>();if(role!=null){role.designSize=size;role.Apply();}}
    static void Prepare()
    {
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            if(scene.isDirty&&!File.Exists(Folder+"Before/UnsavedMainGameScene.unity"))
                if(!EditorSceneManager.SaveScene(scene,Folder+"Before/UnsavedMainGameScene.unity",true))throw new IOException("Unsaved scene preservation failed");
            T Find<T>()where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).Single();
            var hud=Find<CombatHudView>();var hp=(RectTransform)Ref<Image>(hud,"playerHp").transform.parent;
            var guard=(RectTransform)Ref<Image>(hud,"shield").transform.parent;var recovery=hud.attackRecovery.rectTransform;
            var width=hp.rect.width;var left=hp.anchoredPosition.x-width*.5f;var first=width*.56f;var second=width-first-18;var row=hp.anchoredPosition.y+92;
            guard.SetParent(hp.parent,false);Place(guard,new Vector2(left+first*.5f,row),new Vector2(first,46));
            var label=Ref<TextMeshProUGUI>(hud,"shieldText");Place(label.rectTransform,new Vector2(0,42),new Vector2(first+8,32));Font(label,26);
            hud.guardGauge.shieldSilhouette=true;hud.guardGauge.raycastTarget=false;EditorUtility.SetDirty(hud.guardGauge);
            recovery.SetParent(hp.parent,false);var point=new Vector2(left+first+18+second*.5f,row);Place(recovery,point,new Vector2(second,42));
            var anchor=recovery.GetComponent<AttackCooldownAnchor>();anchor.placement=AttackCooldownPlacement.Authored;anchor.authoredPosition=point;EditorUtility.SetDirty(anchor);
            var old=hp.parent.Find("AttackRecoveryLabel") as RectTransform;
            if(old==null){old=(RectTransform)new GameObject("AttackRecoveryLabel",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI),typeof(GameTextBinding)).transform;old.SetParent(hp.parent,false);}
            Place(old,point+Vector2.up*42,new Vector2(second,32));var attackLabel=old.GetComponent<TextMeshProUGUI>();attackLabel.font=label.font;attackLabel.fontSharedMaterial=label.fontSharedMaterial;attackLabel.color=new Color32(221,208,169,255);attackLabel.alignment=TextAlignmentOptions.Center;Font(attackLabel,26);attackLabel.text="공격";old.GetComponent<GameTextBinding>().key="ui.combat.attack.recovery";
            // The label follows the actual gauge's visibility, including inventory/navigation transitions.
            var visibility=old.GetComponent<CombatGaugeLabel>()??old.gameObject.AddComponent<CombatGaugeLabel>();visibility.gauge=hud.attackRecovery;EditorUtility.SetDirty(visibility);
            var result=Find<ResultView>();var details=Ref<TextMeshProUGUI>(result,"details");details.gameObject.SetActive(false);
            var button=Ref<Button>(result,"restart");var buttonLabel=button.GetComponentInChildren<TextMeshProUGUI>(true);buttonLabel.text="타이틀로";var binding=buttonLabel.GetComponent<GameTextBinding>();if(binding!=null)binding.key="ui.result.return";
            Place((RectTransform)button.transform,new Vector2(0,0),new Vector2(300,62));
            var table=BehemothTextEditor.Table;table.Set("ui.result.return","타이틀로");table.Set("ui.combat.attack.recovery","공격");EditorUtility.SetDirty(table);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("HUD save failed");
            var style=AssetDatabase.FindAssets("t:WeakPointVisualStyle",new[]{"Assets/Data"}).Select(g=>AssetDatabase.LoadAssetAtPath<WeakPointVisualStyle>(AssetDatabase.GUIDToAssetPath(g))).Single();
            style.core=new Color32(240,225,183,255);style.stroke=new Color32(194,55,33,255);EditorUtility.SetDirty(style);AssetDatabase.SaveAssets();BehemothTextEditor.ExportDefault();
            File.WriteAllText(Folder+"Layout.txt",$"HP/MP unchanged. HP width={width}, guard={guard.anchoredPosition}/{guard.sizeDelta}, recovery={point}/{recovery.sizeDelta}.\n");
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }
}
